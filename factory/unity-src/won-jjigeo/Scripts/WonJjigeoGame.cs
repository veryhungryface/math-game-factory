// 원 찍어 — 심야 청동 삼각 명판 각인 프레스.
// 핀을 끌어 명판(또는 그 바깥 베드)의 한 점에 박으면, 그 점을 중심으로 원이 스윕한다.
// 외심 미션이면 세 꼭짓점을, 내심 미션이면 세 변을 물어야 각인 성공. 판정은 WonGeo.cs 의 정수 거리² 비교.
// 코드는 정답 자리로 스냅·자동 작도·힌트 선을 미리 그리지 않는다. 원은 학생이 박은 점의 결과만 보여 준다
// (정답으로 판정된 뒤에만 핀이 중심 홈에 "안착"하며 정확한 원을 새긴다).
using System.Collections;
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Mgf.WonJjigeo
{
    public class WonJjigeoGame : MonoBehaviour, IMgfGame
    {
        [System.Serializable]
        class State : MgfState
        {
            public int combo, maxCombo, plates, pokes, scraps;
            public string mission = "", task = "";
            public bool onboarding;
        }

        class Pin
        {
            public GameObject root; public Transform body; public Renderer[] rends;
            public int slot; public bool bent;
        }

        class Plate
        {
            public GameObject go; public Mesh mesh; public MeshRenderer mr;
            public Vector3 pivot;          // 월드 기준 무게중심(메시 로컬 원점)
        }

        // ── 상수·팔레트 (기획서 palette: #D2C4A8 모래 베드 · #1F6B5A 녹청 · #9B2C2C 산화 적 · #2A2420 철 · #E8DCC8 각인 크림)
        static readonly Color Sand = Hex("D2C4A8"), Verd = Hex("1F6B5A"), VerdLit = Hex("3FB597"), Rust = Hex("9B2C2C"),
            Iron = Hex("2A2420"), Cream = Hex("E8DCC8"), Brass = Hex("B08D57");
        const float PlayTime = 90f, PlateH = 0.2f;
        static Color Hex(string h) => MgfLook.Hex(h);
        // CreatePrimitive 는 구·원기둥에 SphereCollider·CapsuleCollider 를 붙인다. 코드에서 참조하지 않으면 엔진 코드 스트리핑이
        // 그 클래스를 빼 버려 "Can't add component" 콘솔 에러가 난다 → 타입을 참조해 남긴다.
        static readonly System.Type[] keepTypes = { typeof(SphereCollider), typeof(CapsuleCollider), typeof(MeshCollider) };

        readonly State st = new State();
        Gen gen;
        System.Random rng = new System.Random(20260926);
        readonly List<MgfProblem> bank = new List<MgfProblem>();

        Camera cam;
        Light keyLight;
        bool land;
        float camBlend;         // 0 = 타이틀 궤도, 1 = 플레이 시점
        Vector3 shakeOffset;

        // 월드
        Transform world, staticRoot;
        Material sandMat, bronzeMat, ironMat, brassMat, steelMat, bentMat, prePinMat, scrapMat;
        Material lineMat, dashMat, glowMat;
        readonly List<Plate> platePool = new List<Plate>();
        readonly List<Plate> rack = new List<Plate>();
        readonly List<Plate> scrapPlates = new List<Plate>();
        Plate curPlate, demoPlate;
        Placed demoPlaced;
        Placed cur;
        int bandIndex, platesInBand, lastBand;
        readonly Pin[] pins = new Pin[3];
        Pin prePin, demoPin;
        bool prePinIn;
        Vector3 trayPos;
        Transform tray, rackRail, chute;
        TextMeshPro rackLabel;
        readonly Transform[] scrapZones = new Transform[2];

        // 선
        LineRenderer plateEdge, grooveUnder, groove, arm, reticle, reticleX, reticleY, ghostCircle, fixCircle, onbPath, plateOutline, trueRing, pulseRing, rightMark;
        readonly LineRenderer[] radii = new LineRenderer[3], ticks = new LineRenderer[3], isoTicks = new LineRenderer[2], misses = new LineRenderer[3], ripples = new LineRenderer[3];
        readonly float[] rippleT = { 9, 9, 9 };
        int rippleNext;
        Transform shimmer;
        TextMeshPro revealText, centerLetter;
        readonly TextMeshPro[] vLabels = new TextMeshPro[3];
        ParticleSystem motes;

        // 입력
        enum DragKind { None, TrayPin, PrePin }
        DragKind drag;
        Pin dragPin;
        Vector3 dragTip, downWorld;
        Vector2 downScreen;
        bool dragMoved, dragFromAim, aiming;
        Vector3 aimPt;
        Pin aimPin;

        void SetAim(Vector3 p)
        {
            if (!aiming) aimPin = FirstFreePin();
            if (aimPin == null) return;
            aiming = true; aimPt = p;
            Play(clTick, 0.45f);
            hintTxt.text = "같은 자리를 한 번 더 누르면 박힌다";
        }

        void ClearAim()
        {
            if (!aiming) return;
            aiming = false; aimPin = null;
            reticle.gameObject.SetActive(false); reticleX.gameObject.SetActive(false); reticleY.gameObject.SetActive(false);
            PlacePinsInTray();
        }

        void UpdateAim()
        {
            if (!aiming || aimPin == null || drag != DragKind.None) return;
            var t = aimPin.root.transform;
            var goal = aimPt + Vector3.up * (1.05f + Mathf.Sin(Time.time * 4f) * 0.06f);
            t.position = Vector3.Lerp(t.position, goal, 1f - Mathf.Exp(-Time.deltaTime * 18f));
            t.rotation = Quaternion.Euler(6, 0, -5);
            ShowReticle(aimPt, true);
        }

        // 판 진행
        float playT, idleT, busyShakeT = 9f, trayShakeT = 9f, plateEnterT = 9f, bannerT;
        bool busy, frozen, roundPending, firstPlantDone, roundRetry;
        int onbMisses;   // 온보딩 무감점 재시도 횟수(3번까지 — 그 뒤엔 정상 규칙, 실패 상태가 사라지지 않게)
        Coroutine roundCo;
        int roundPinSlot = -1;
        bool roundOk;
        float hitStop;

        // UI
        Canvas canvas;
        RectTransform titleRoot, hudRoot, endRoot, badgeRt, ctaRt, ctaShine, endCtaRt, endCtaShine, timerFill;
        TextMeshProUGUI badgeBig, badgeSub, platesTxt, scoreTxt, comboTxt, timeTxt, bannerTxt, hintTxt, bestTxt;
        TextMeshProUGUI endTitle, endPlates, endScore, endCombo, endBest;
        Image bannerBg;
        float shownScore, endCountT;
        int shownTimeSec = -1, shownPlates = -1, shownScoreInt = -1;
        bool ctaDown, endCtaDown;
        float ctaPressT = 9f, badgeFlipT = 9f;
        string badgeNext;
        Sprite roundSpr, ringSpr;
        int best, bestScore;
        float endShownAt;

        // 소리
        AudioSource sfx;
        AudioClip clClang, clThunk, clScrape, clSeat, clChime, clTick, clRefuse, clPull;

        // ─────────────────────────────── 부팅
        void Awake()
        {
            MgfLook.Quality(30f);
            if (keepTypes.Length == 0) Debug.Log("keep");
            gen = new Gen(4242);
            BuildBank();
            BuildWorld();
            BuildUi();
            BuildSounds();
            MgfText.Prewarm("원찍어중심에핀을박아라각인시작외심내심의위치를나타내시오최고장점연속초시간종료명판이모두굽었다다시세꼭짓점까지거리가같지않다변그자리는둔각삼각형밖에있다직각빗변위뽑고잘못박힌먼저끌어놓아라원이끝날때까지뱃지를보고다음투입등장부등변이등분선수직삼각형의성질중학교학년·×=ABCOI0123456789°∠△−→—:,.!?()");
            best = PlayerPrefs.GetInt("wonjjigeo.best", 0);
            bestScore = PlayerPrefs.GetInt("wonjjigeo.bestScore", 0);
            ShowTitle();
            MgfBridge.Register(this);
        }

        // ── 문제 은행: 게임 판과 같은 Gen.Make · Words 로 만든다(별도 시험지 아님)
        void BuildBank()
        {
            var g = new Gen(777);
            var seen = new HashSet<string>();
            int guard = 0;
            // 첫 문항(온보딩 고정)도 은행에 넣는다
            AddBank(Gen.First(), seen);
            while (bank.Count < 360 && guard++ < 20000)
            {
                int band = 1 + (guard % 5 == 0 ? 0 : guard % 5 <= 2 ? 1 : 2);
                AddBank(g.Make(band, guard), seen);
            }
        }

        void AddBank(PlateSpec s, HashSet<string> seen)
        {
            string prompt = Words.Prompt(s);
            if (!seen.Add(prompt)) return;
            bank.Add(new MgfProblem
            {
                id = "w" + (bank.Count + 1),
                prompt = prompt,
                choices = null,
                answer = Words.Answer(s),
                answerNumeric = Words.AngleAtCenter(s),
                unitConcept = Words.Concept(s)
            });
        }

        // ─────────────────────────────── 월드 구성
        void BuildWorld()
        {
            world = new GameObject("World").transform;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Hex("5A4B3C") * 0.55f;
            RenderSettings.ambientEquatorColor = Hex("3A3029") * 0.5f;
            RenderSettings.ambientGroundColor = Hex("15110E");
            RenderSettings.skybox = null;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Hex("120E0B");
            RenderSettings.fogStartDistance = 34f;
            RenderSettings.fogEndDistance = 70f;

            cam = MgfLook.Camera(new Vector3(0, 22, -6), Vector3.zero, 36f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Hex("120E0B");
            cam.farClipPlane = 120f;

            // 작업등: 그림자를 드리우는 스폿 하나(키 라이트) + 약한 보조 방향광(그림자 없음)
            keyLight = new GameObject("WorkLamp").AddComponent<Light>();
            keyLight.type = LightType.Spot;
            keyLight.transform.position = new Vector3(-1.2f, 16f, 2.5f);
            keyLight.transform.LookAt(new Vector3(0, 0, -0.6f));
            keyLight.spotAngle = 78f;
            keyLight.innerSpotAngle = 30f;
            keyLight.range = 40f;
            keyLight.intensity = 1.9f;
            keyLight.color = Hex("FFE2B8");
            keyLight.shadows = LightShadows.Soft;
            keyLight.shadowStrength = 0.8f;
            keyLight.shadowBias = 0.02f;
            keyLight.shadowNormalBias = 0.2f;
            keyLight.renderMode = LightRenderMode.ForcePixel;
            var fill = new GameObject("Fill").AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.transform.rotation = Quaternion.Euler(58, 140, 0);
            fill.color = Hex("8FA7B8");
            fill.intensity = 0.28f;
            fill.shadows = LightShadows.None;
            if (MgfBridge.LowGfx)
            {
                // 소프트웨어 렌더러: 보조광을 끄고(오브젝트마다 추가 패스) 그림자는 단단한 그림자로 유지
                fill.enabled = false;
                keyLight.shadows = LightShadows.Hard;
                RenderSettings.ambientSkyColor = Hex("5A4B3C") * 0.75f;
            }

            // 재질(텍스처는 부팅 때 1회 굽는다)
            sandMat = MgfLook.Lit(Color.white, 0.08f, 0f, null, SandTex());
            sandMat.mainTextureScale = new Vector2(2.2f, 2.2f);
            var bronzeTex = BronzeTex();
            bronzeMat = MgfLook.Lit(Hex("C49A66"), 0.62f, 0.5f, Hex("120A04"), bronzeTex);
            bronzeMat.SetColor("_RimColor", new Color(1f, 0.85f, 0.6f, 0.35f));
            scrapMat = MgfLook.Lit(Hex("6A5040"), 0.25f, 0.55f, null, bronzeTex);
            ironMat = MgfLook.Lit(Color.white, 0.22f, 0.35f, null, IronTex());
            ironMat.SetColor("_RimColor", new Color(1, 0.9f, 0.8f, 0.05f));
            sandMat.SetColor("_RimColor", new Color(1, 1, 1, 0f));
            brassMat = MgfLook.Lit(Brass, 0.6f, 0.85f, Hex("1A1206"));
            steelMat = MgfLook.Lit(Hex("A8ADB2"), 0.7f, 0.9f);
            bentMat = MgfLook.Lit(Hex("7A4A38"), 0.3f, 0.6f);
            prePinMat = MgfLook.Lit(Hex("4A4E55"), 0.45f, 0.8f);
            lineMat = new Material(MgfLook.Shader("MgfAlpha")) { name = "WonLine" };
            lineMat.SetColor("_Color", Color.white);
            dashMat = new Material(MgfLook.Shader("MgfAlpha")) { name = "WonDash", mainTexture = DashTex() };
            dashMat.SetColor("_Color", Color.white);
            glowMat = MgfLook.Additive(Color.white, MgfLook.SoftDot);

            // 바닥·프레스 몸체(움직이지 않는 것은 staticRoot 아래에 두고 끝에서 정적 배칭 — 드로콜 절감)
            staticRoot = new GameObject("Static").transform;
            staticRoot.SetParent(world, false);
            var floor = MgfLook.Block("Floor", new Vector3(0, -1.2f, -2), new Vector3(70, 1, 60), 0.2f, MgfLook.Lit(Hex("1C1612"), 0.2f, 0.3f), staticRoot);
            Destroy(floor.GetComponent<Collider>());
            var bed = MgfLook.Block("Bed", new Vector3(0, -0.3f, 0), new Vector3((float)Layout.BedHX * 2, 0.6f, (float)Layout.BedHZ * 2), 0.05f, sandMat, staticRoot);
            Destroy(bed.GetComponent<Collider>());
            // 철 립 + 리벳
            float lx = (float)Layout.BedHX, lz = (float)Layout.BedHZ, lw = 0.75f;
            var rivetMat = MgfLook.Lit(Hex("5B4E44"), 0.55f, 0.8f);
            MakeLip(new Vector3(0, 0.12f, lz + lw / 2), new Vector3(lx * 2 + lw * 2, 0.5f, lw), rivetMat, 7, true);
            MakeLip(new Vector3(0, 0.12f, -lz - lw / 2), new Vector3(lx * 2 + lw * 2, 0.5f, lw), rivetMat, 7, true);
            MakeLip(new Vector3(-lx - lw / 2, 0.12f, 0), new Vector3(lw, 0.5f, lz * 2), rivetMat, 5, false);
            MakeLip(new Vector3(lx + lw / 2, 0.12f, 0), new Vector3(lw, 0.5f, lz * 2), rivetMat, 5, false);
            // 받침대(프레스 몸체) — 베드 밑 어두운 주물
            var body = MgfLook.Block("PressBody", new Vector3(0, -0.85f, 0), new Vector3(lx * 2 + 2.6f, 0.9f, lz * 2 + 2.6f), 0.3f, ironMat, staticRoot);
            Destroy(body.GetComponent<Collider>());

            StaticBatchingUtility.Combine(staticRoot.gameObject);

            // 스크랩 구역 표시(베드 오른쪽 가장자리가 점점 먹힌다)
            for (int i = 0; i < 2; i++)
            {
                var z = MgfLook.Block("ScrapZone" + i, new Vector3(lx - (float)Layout.ScrapW * (i + 0.5f), 0.01f, 0), new Vector3((float)Layout.ScrapW - 0.04f, 0.04f, lz * 2 - 0.1f), 0.02f, MgfLook.Lit(Hex("8C7D66"), 0.05f), world);
                Destroy(z.GetComponent<Collider>());
                z.SetActive(false);
                scrapZones[i] = z.transform;
            }

            // 핀 트레이
            tray = MgfLook.Block("Tray", Vector3.zero, new Vector3(4.6f, 0.35f, 1.5f), 0.18f, ironMat, world).transform;
            Destroy(tray.GetComponent<Collider>());
            for (int i = 0; i < 3; i++)
            {
                var hole = MgfLook.Prim(PrimitiveType.Cylinder, "Hole" + i, new Vector3(-1.4f + 1.4f * i, 0.16f, 0), new Vector3(0.42f, 0.02f, 0.42f), MgfLook.Lit(Hex("0E0B09"), 0.1f), tray, false);
                hole.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            // 좌측 완성 랙 · 우측 스크랩 슈트 (가로 화면 거터를 채운다)
            rackRail = MgfLook.Block("Rack", Vector3.zero, new Vector3(1.7f, 0.25f, 8.0f), 0.12f, ironMat, world).transform;
            Destroy(rackRail.GetComponent<Collider>());
            rackLabel = MgfText.World("각인 완료", Vector3.zero, 4.2f, Hex("8C7D66"), world);
            rackLabel.transform.rotation = Quaternion.Euler(90, 0, 0);
            chute = new GameObject("Chute").transform;
            chute.SetParent(world, false);
            var ch1 = MgfLook.Block("ChuteBed", new Vector3(0, -0.2f, 0), new Vector3(2.6f, 0.25f, 8.4f), 0.12f, MgfLook.Lit(Hex("3A2F28"), 0.3f, 0.5f), chute);
            Destroy(ch1.GetComponent<Collider>());
            for (int s = -1; s <= 1; s += 2)
            {
                var w = MgfLook.Block("ChuteWall", new Vector3(s * 1.25f, 0.1f, 0), new Vector3(0.22f, 0.6f, 8.4f), 0.08f, ironMat, chute);
                Destroy(w.GetComponent<Collider>());
            }
            var chLabel = MgfText.World("스크랩", new Vector3(0, 0.06f, -3.4f), 5, Hex("8C7D66"), chute);
            chLabel.transform.localRotation = Quaternion.Euler(90, 0, 0);

            // 명판 풀
            for (int i = 0; i < 14; i++) platePool.Add(MakePlate(i));

            // 핀
            for (int i = 0; i < 3; i++) { pins[i] = MakePin("Pin" + i, brassMat); pins[i].slot = i; }
            prePin = MakePin("PrePin", prePinMat); prePin.root.SetActive(false);
            demoPin = MakePin("DemoPin", brassMat);

            // 선
            grooveUnder = Line("GrooveUnder", 0.17f, 97, true);
            groove = Line("Groove", 0.09f, 97, true);
            arm = Line("Arm", 0.06f, 2, false);
            reticle = Line("Reticle", 0.04f, 49, true);
            reticleX = Line("RetX", 0.03f, 2, false);
            reticleY = Line("RetY", 0.03f, 2, false);
            ghostCircle = Line("Ghost", 0.07f, 97, true, dashMat);
            fixCircle = Line("FixCircle", 0.07f, 97, true, dashMat);
            onbPath = Line("OnbPath", 0.2f, 24, false, dashMat);
            plateOutline = Line("Outline", 0.08f, 3, true);
            plateEdge = Line("PlateEdge", 0.035f, 3, true);
            trueRing = Line("TrueRing", 0.06f, 49, true);
            pulseRing = Line("Pulse", 0.07f, 49, true);
            rightMark = Line("RightMark", 0.045f, 3, false);
            for (int i = 0; i < 3; i++)
            {
                radii[i] = Line("Radius" + i, 0.06f, 2, false);
                ticks[i] = Line("Tick" + i, 0.05f, 2, false);
                misses[i] = Line("Miss" + i, 0.07f, 2, false);
                ripples[i] = Line("Ripple" + i, 0.06f, 49, true);
            }
            for (int i = 0; i < 2; i++) isoTicks[i] = Line("IsoTick" + i, 0.05f, 2, false);
            shimmer = MgfLook.Prim(PrimitiveType.Quad, "Shimmer", Vector3.zero, Vector3.one * 0.9f, glowMat, world, false).transform;
            shimmer.rotation = Quaternion.Euler(90, 0, 0);
            shimmer.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            shimmer.gameObject.SetActive(false);

            for (int i = 0; i < 3; i++) vLabels[i] = FlatText(Geo.Names[i], 4.2f, Cream);
            revealText = FlatText("", 4.2f, Cream);
            revealText.fontSize = 5f;
            centerLetter = FlatText("O", 4f, VerdLit);

            motes = MakeMotes();
        }

        void MakeLip(Vector3 pos, Vector3 size, Material rivetMat, int rivets, bool alongX)
        {
            var lip = MgfLook.Block("Lip", pos, size, 0.12f, ironMat, staticRoot);
            Destroy(lip.GetComponent<Collider>());
            float len = alongX ? size.x : size.z;
            for (int i = 0; i < rivets; i++)
            {
                float t = (i + 0.5f) / rivets - 0.5f;
                var p = pos + (alongX ? new Vector3(t * len * 0.92f, size.y / 2, 0) : new Vector3(0, size.y / 2, t * len * 0.92f));
                var r = MgfLook.Prim(PrimitiveType.Sphere, "Rivet", p, new Vector3(0.24f, 0.12f, 0.24f), rivetMat, staticRoot, false);
                r.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        TextMeshPro FlatText(string s, float size, Color c)
        {
            var t = MgfText.World(s, Vector3.zero, size, c, world);
            t.transform.rotation = Quaternion.Euler(90, 0, 0);
            t.outlineWidth = 0.28f;
            t.outlineColor = new Color32(26, 20, 16, 255);
            t.gameObject.SetActive(false);
            return t;
        }

        LineRenderer Line(string name, float width, int count, bool loop, Material mat = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(world, false);
            go.transform.rotation = Quaternion.Euler(90, 0, 0);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.alignment = LineAlignment.TransformZ;
            lr.sharedMaterial = mat ?? lineMat;
            lr.widthMultiplier = width;
            lr.positionCount = count;
            lr.loop = loop;
            lr.numCapVertices = loop ? 0 : 3;
            lr.numCornerVertices = 2;
            lr.shadowCastingMode = ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.textureMode = mat == dashMat && mat != null ? LineTextureMode.Tile : LineTextureMode.Stretch;
            go.SetActive(false);
            return lr;
        }

        static void SetColor(LineRenderer lr, Color c) { lr.startColor = c; lr.endColor = c; }

        static void CirclePts(LineRenderer lr, Vector3 c, float r, int n, float frac = 1f)
        {
            int count = Mathf.Max(2, Mathf.CeilToInt((n - 1) * frac) + 1);
            if (lr.positionCount != count) lr.positionCount = count;
            for (int i = 0; i < count; i++)
            {
                float a = Mathf.Min(frac, (float)i / (n - 1)) * Mathf.PI * 2f;
                lr.SetPosition(i, c + new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r));
            }
        }

        static Vector3 W(V2 p, float y) => new Vector3((float)p.x, y, (float)p.y);
        static V2 V(Vector3 p) => new V2(p.x, p.z);

        // ── 명판 메시: 윗면(모서리 베벨로 안쪽으로 줄인 삼각형) + 베벨 띠 + 옆면. 면마다 정점 분리(평면 법선).
        Plate MakePlate(int i)
        {
            var go = new GameObject("Plate" + i);
            go.transform.SetParent(world, false);
            var mesh = new Mesh { name = "PlateMesh" };
            mesh.MarkDynamic();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = bronzeMat;
            mr.shadowCastingMode = ShadowCastingMode.On;
            mr.receiveShadows = true;
            go.SetActive(false);
            return new Plate { go = go, mesh = mesh, mr = mr };
        }

        readonly List<Vector3> mv = new List<Vector3>(64);
        readonly List<Vector3> mn = new List<Vector3>(64);
        readonly List<Vector2> mu = new List<Vector2>(64);
        readonly List<int> mt = new List<int>(96);

        void ShapePlate(Plate pl, V2[] P)
        {
            var c = new Vector3((float)((P[0].x + P[1].x + P[2].x) / 3), 0, (float)((P[0].y + P[1].y + P[2].y) / 3));
            pl.pivot = c;
            var o = new Vector3[3]; var inn = new Vector3[3];
            for (int i = 0; i < 3; i++) o[i] = W(P[i], 0) - c;
            // 안쪽 오프셋(베벨 폭 b): 꼭짓점을 각의 이등분선 방향으로 b/sin(θ/2) 만큼
            const float b = 0.13f;
            for (int i = 0; i < 3; i++)
            {
                var p = o[i]; var a = (o[(i + 1) % 3] - p).normalized; var d = (o[(i + 2) % 3] - p).normalized;
                float half = Vector3.Angle(a, d) * 0.5f * Mathf.Deg2Rad;
                inn[i] = p + (a + d).normalized * (b / Mathf.Max(0.2f, Mathf.Sin(half)));
            }
            // 윗면 삼각형이 위(+y)를 향하게 정렬
            if (Vector3.Cross(o[1] - o[0], o[2] - o[0]).y < 0) { (o[1], o[2]) = (o[2], o[1]); (inn[1], inn[2]) = (inn[2], inn[1]); }
            mv.Clear(); mn.Clear(); mu.Clear(); mt.Clear();
            float top = PlateH, bev = PlateH - 0.08f;
            // 윗면
            int s0 = mv.Count;
            for (int i = 0; i < 3; i++) { var v = inn[i] + Vector3.up * top; mv.Add(v); mn.Add(Vector3.up); mu.Add(new Vector2(v.x + c.x, v.z + c.z) * 0.22f); }
            mt.Add(s0); mt.Add(s0 + 1); mt.Add(s0 + 2);
            for (int i = 0; i < 3; i++)
            {
                int j = (i + 1) % 3;
                var ao = o[i] + Vector3.up * bev; var bo = o[j] + Vector3.up * bev;
                var ai = inn[i] + Vector3.up * top; var bi = inn[j] + Vector3.up * top;
                Quad(ao, bo, bi, ai, c);
                var ab = o[i]; var bb = o[j];
                Quad(ab, bb, bo, ao, c);
            }
            pl.mesh.Clear();
            pl.mesh.SetVertices(mv); pl.mesh.SetNormals(mn); pl.mesh.SetUVs(0, mu); pl.mesh.SetTriangles(mt, 0);
            pl.mesh.RecalculateBounds();
            for (int i = 0; i < 3; i++) lastInset[i] = inn[i] + c + Vector3.up * (top + 0.012f);
        }
        readonly Vector3[] lastInset = new Vector3[3];

        void Quad(Vector3 a, Vector3 b, Vector3 cc, Vector3 d, Vector3 c)
        {
            var n = Vector3.Cross(b - a, d - a).normalized;
            var mid = (a + b) * 0.5f;
            if (n.x * mid.x + n.z * mid.z < 0) n = -n;   // 로컬 원점 = 무게중심 → 바깥쪽을 향하게
            int s = mv.Count;
            mv.Add(a); mv.Add(b); mv.Add(cc); mv.Add(d);
            for (int k = 0; k < 4; k++) mn.Add(n);
            mu.Add(new Vector2(a.x + c.x, a.z + c.z + a.y) * 0.22f); mu.Add(new Vector2(b.x + c.x, b.z + c.z + b.y) * 0.22f);
            mu.Add(new Vector2(cc.x + c.x, cc.z + c.z + cc.y) * 0.22f); mu.Add(new Vector2(d.x + c.x, d.z + c.z + d.y) * 0.22f);
            // 법선 방향으로 감기 순서 맞추기
            // Unity: Cross(b−a, c−a) 가 향하는 쪽이 앞면
            if (Vector3.Dot(Vector3.Cross(b - a, cc - a), n) > 0) { mt.Add(s); mt.Add(s + 1); mt.Add(s + 2); mt.Add(s); mt.Add(s + 2); mt.Add(s + 3); }
            else { mt.Add(s); mt.Add(s + 2); mt.Add(s + 1); mt.Add(s); mt.Add(s + 3); mt.Add(s + 2); }
        }

        Plate TakePlate()
        {
            foreach (var p in platePool) if (!p.go.activeSelf) return p;
            // 랙이 꽉 차면 가장 오래된 것을 재사용
            var old = rack[0]; rack.RemoveAt(0); return old;
        }

        // ── 핀: 피벗 = 핀 끝. 몸통 그룹(body)을 굽혀 "굽은 핀"을 만든다.
        static Mesh coneMesh;
        Pin MakePin(string name, Material headMat)
        {
            var root = new GameObject(name);
            root.transform.SetParent(world, false);
            var body = new GameObject("Body").transform;
            body.SetParent(root.transform, false);
            if (!coneMesh) coneMesh = Cone(0.075f, 0.2f, 14);
            var tip = new GameObject("Tip");
            tip.transform.SetParent(body, false);
            tip.AddComponent<MeshFilter>().sharedMesh = coneMesh;
            tip.AddComponent<MeshRenderer>().sharedMaterial = steelMat;
            MgfLook.Prim(PrimitiveType.Cylinder, "Shaft", new Vector3(0, 0.2f + 0.2f, 0), new Vector3(0.15f, 0.2f, 0.15f), headMat, body, false);
            MgfLook.Prim(PrimitiveType.Cylinder, "Collar", new Vector3(0, 0.6f, 0), new Vector3(0.24f, 0.03f, 0.24f), headMat, body, false);
            MgfLook.Prim(PrimitiveType.Sphere, "Head", new Vector3(0, 0.7f, 0), new Vector3(0.46f, 0.26f, 0.46f), headMat, body, false);
            var p = new Pin { root = root, body = body, rends = root.GetComponentsInChildren<Renderer>() };
            foreach (var r in p.rends) { r.shadowCastingMode = ShadowCastingMode.On; r.receiveShadows = true; }
            return p;
        }

        static Mesh Cone(float r, float h, int seg)
        {
            var m = new Mesh { name = "Cone" };
            var v = new List<Vector3>(); var t = new List<int>();
            for (int i = 0; i < seg; i++)
            {
                float a0 = i * Mathf.PI * 2 / seg, a1 = (i + 1) * Mathf.PI * 2 / seg;
                int s = v.Count;
                v.Add(Vector3.zero); v.Add(new Vector3(Mathf.Cos(a1) * r, h, Mathf.Sin(a1) * r)); v.Add(new Vector3(Mathf.Cos(a0) * r, h, Mathf.Sin(a0) * r));
                t.Add(s); t.Add(s + 1); t.Add(s + 2);
            }
            m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        void SetPinMat(Pin p, Material head)
        {
            foreach (var r in p.rends) if (r.gameObject.name != "Tip") r.sharedMaterial = head;
        }

        ParticleSystem MakeMotes()
        {
            var go = new GameObject("Motes");
            go.transform.SetParent(world, false);
            go.transform.position = new Vector3(0, 3.5f, 0);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.playOnAwake = false; main.maxParticles = 40;
            main.startLifetime = 5f; main.startSpeed = 0.12f; main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.13f);
            main.startColor = new Color(1f, 0.88f, 0.66f, 0.55f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.004f;
            var em = ps.emission; em.rateOverTime = MgfBridge.LowGfx ? 3f : 7f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(10, 4, 8);
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.3f), new GradientAlphaKey(1, 0.7f), new GradientAlphaKey(0, 1) });
            col.color = g;
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.15f; noise.frequency = 0.3f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = glowMat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            ps.Play();
            return ps;
        }

        // ── 절차적 텍스처(부팅 1회)
        static float Hash(int x, int y, int s) { unchecked { int h = x * 374761393 + y * 668265263 + s * 982451653; h = (h ^ (h >> 13)) * 1274126177; return ((h ^ (h >> 16)) & 0xffff) / 65535f; } }
        static float VNoise(float x, float y, int s, int period)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0; fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
            int X0 = ((x0 % period) + period) % period, Y0 = ((y0 % period) + period) % period, X1 = (X0 + 1) % period, Y1 = (Y0 + 1) % period;
            float a = Hash(X0, Y0, s), b = Hash(X1, Y0, s), c = Hash(X0, Y1, s), d = Hash(X1, Y1, s);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }
        static float Fbm(float u, float v, int s, int baseP)
        {
            float sum = 0, amp = 0.5f; int p = baseP;
            for (int o = 0; o < 4; o++) { sum += VNoise(u * p, v * p, s + o, p) * amp; amp *= 0.5f; p *= 2; }
            return sum;
        }

        Texture2D SandTex()
        {
            const int S = 256;
            var t = new Texture2D(S, S, TextureFormat.RGBA32, true) { name = "Sand", wrapMode = TextureWrapMode.Repeat };
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float u = (float)x / S, v = (float)y / S;
                    float n = Fbm(u, v, 11, 8);
                    float grain = Hash(x, y, 3);
                    float k = 0.9f + n * 0.16f + (grain - 0.5f) * 0.1f;
                    if (grain > 0.985f) k *= 0.72f;
                    var c = Sand * k;
                    px[y * S + x] = new Color(Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b), 1);
                }
            t.SetPixels32(px); t.Apply(true, true);
            return t;
        }

        Texture2D BronzeTex()
        {
            const int S = 128;
            var t = new Texture2D(S, S, TextureFormat.RGBA32, true) { name = "Bronze", wrapMode = TextureWrapMode.Repeat };
            var px = new Color32[S * S];
            var baseC = Hex("B88A58"); var dark = Hex("6E4A2C"); var pat = Hex("4F8C78");
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float u = (float)x / S, v = (float)y / S;
                    float n = Fbm(u, v, 21, 4), m = Fbm(u, v, 57, 8);
                    var c = Color.Lerp(dark, baseC, 0.45f + n * 0.7f);
                    float patina = Mathf.Clamp01((m - 0.62f) * 5f);
                    c = Color.Lerp(c, pat, patina * 0.55f);
                    c *= 0.94f + Hash(x, y, 9) * 0.1f;
                    px[y * S + x] = new Color(c.r, c.g, c.b, 1);
                }
            t.SetPixels32(px); t.Apply(true, true);
            return t;
        }

        Texture2D IronTex()
        {
            const int S = 128;
            var t = new Texture2D(S, S, TextureFormat.RGBA32, true) { name = "Iron", wrapMode = TextureWrapMode.Repeat };
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float n = Fbm((float)x / S, (float)y / S, 33, 4);
                    var c = Color.Lerp(Hex("1E1916"), Hex("4A3E36"), n);
                    if (Hash(x, y, 5) > 0.97f) c = Color.Lerp(c, Hex("6B3A26"), 0.5f);
                    px[y * S + x] = new Color(c.r, c.g, c.b, 1);
                }
            t.SetPixels32(px); t.Apply(true, true);
            return t;
        }

        static Texture2D DashTex()
        {
            var t = new Texture2D(16, 4, TextureFormat.RGBA32, false) { name = "Dash", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var px = new Color32[64];
            for (int y = 0; y < 4; y++) for (int x = 0; x < 16; x++) px[y * 16 + x] = new Color32(255, 255, 255, (byte)(x < 9 ? 255 : 0));
            t.SetPixels32(px); t.Apply(false, true);
            return t;
        }

        static Sprite RoundSprite(int S, float rad, bool ring)
        {
            var t = new Texture2D(S, S, TextureFormat.RGBA32, false) { name = ring ? "Ring" : "Round", wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float a;
                    if (ring)
                    {
                        float dx = x + 0.5f - S / 2f, dy = y + 0.5f - S / 2f, d = Mathf.Sqrt(dx * dx + dy * dy);
                        float r = S / 2f - 2, w = S * 0.06f;
                        a = Mathf.Clamp01(1 - Mathf.Abs(d - (r - w)) / w * 1.0f) ;
                        a = Mathf.Clamp01(a * 2.2f);
                    }
                    else
                    {
                        float qx = Mathf.Max(Mathf.Abs(x + 0.5f - S / 2f) - (S / 2f - rad), 0), qy = Mathf.Max(Mathf.Abs(y + 0.5f - S / 2f) - (S / 2f - rad), 0);
                        a = Mathf.Clamp01(rad - Mathf.Sqrt(qx * qx + qy * qy) + 0.5f);
                    }
                    px[y * S + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            t.SetPixels32(px); t.Apply(false, true);
            float bd = ring ? 0 : rad + 2;
            return Sprite.Create(t, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(bd, bd, bd, bd));
        }

        // ─────────────────────────────── 소리(합성 — 외부 음원 없음)
        void BuildSounds()
        {
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false; sfx.spatialBlend = 0;
            clClang = Synth("clang", 0.9f, t => Partials(t, new[] { 523f, 1307f, 2211f, 3120f }, new[] { 1f, 0.55f, 0.35f, 0.2f }, new[] { 3.2f, 5f, 7f, 9f }) * 0.5f);
            clThunk = Synth("thunk", 0.45f, t => (Mathf.Sin(2 * Mathf.PI * 92 * t) * Mathf.Exp(-t * 11) + Noise(t) * 0.35f * Mathf.Exp(-t * 26)) * 0.6f);
            clScrape = Synth("scrape", 0.75f, t => Noise(t) * 0.22f * Mathf.Sin(Mathf.PI * t / 0.75f) + Mathf.Sin(2 * Mathf.PI * (900 + t * 500) * t) * 0.03f);
            clSeat = Synth("seat", 0.12f, t => Mathf.Sin(2 * Mathf.PI * 1850 * t) * Mathf.Exp(-t * 55) * 0.5f);
            clChime = Synth("chime", 1.2f, t => Partials(t, new[] { 880f, 1320f, 1760f }, new[] { 0.8f, 0.5f, 0.25f }, new[] { 2.2f, 3f, 4f }) * 0.45f);
            clTick = Synth("tick", 0.06f, t => Mathf.Sin(2 * Mathf.PI * 1200 * t) * Mathf.Exp(-t * 80) * 0.4f);
            clRefuse = Synth("refuse", 0.22f, t => Mathf.Sign(Mathf.Sin(2 * Mathf.PI * (t < 0.1f ? 240 : 180) * t)) * 0.18f * Mathf.Exp(-t * 9));
            clPull = Synth("pull", 0.3f, t => Mathf.Sin(2 * Mathf.PI * (300 + t * 1400) * t) * Mathf.Exp(-t * 8) * 0.35f);
        }

        static uint noiseSeed = 12345;
        static float Noise(float t) { noiseSeed = noiseSeed * 1664525u + 1013904223u; return ((noiseSeed >> 9) / 8388607f) * 2f - 1f; }
        static float Partials(float t, float[] f, float[] a, float[] d)
        {
            float s = 0; for (int i = 0; i < f.Length; i++) s += Mathf.Sin(2 * Mathf.PI * f[i] * t) * a[i] * Mathf.Exp(-t * d[i]);
            return s * Mathf.Min(1, t * 400);
        }
        static AudioClip Synth(string name, float dur, System.Func<float, float> f)
        {
            const int rate = 44100; int n = (int)(rate * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(f((float)i / rate), -1, 1) * (1f - (float)i / n);
            var c = AudioClip.Create("won-" + name, n, 1, rate, false);
            c.SetData(data, 0);
            return c;
        }
        void Play(AudioClip c, float v = 0.8f) { if (c && st.phase != "title") sfx.PlayOneShot(c, v); }

        // ─────────────────────────────── UI
        RectTransform R(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }
        Image Img(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color c, Sprite s = null)
        {
            var rt = R(name, parent, anchor, pos, size);
            var im = rt.gameObject.AddComponent<Image>();
            im.sprite = s; im.color = c; im.raycastTarget = false;
            if (s && s.border != Vector4.zero) im.type = Image.Type.Sliced;
            return im;
        }
        TextMeshProUGUI Txt(Transform parent, string s, Vector2 anchor, Vector2 pos, float size, Color c, float width = 360, TextAlignmentOptions al = TextAlignmentOptions.Center)
        {
            var rt = R("T", parent, anchor, pos, new Vector2(width, size * 1.5f));
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = MgfText.Font; t.fontSize = size; t.color = c; t.alignment = al; t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal; t.text = s;
            return t;
        }

        void BuildUi()
        {
            canvas = MgfText.Canvas;
            roundSpr = RoundSprite(64, 16, false);
            ringSpr = RoundSprite(128, 0, true);
            var ct = canvas.transform;

            // ── 타이틀
            titleRoot = R("Title", ct, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            titleRoot.anchorMin = Vector2.zero; titleRoot.anchorMax = Vector2.one; titleRoot.sizeDelta = Vector2.zero;
            var logo = R("Logo", titleRoot, new Vector2(0.5f, 1f), new Vector2(0, -150), new Vector2(360, 200));
            var ring = Img("LogoRing", logo, new Vector2(0.5f, 0.5f), new Vector2(-70, 6), new Vector2(150, 150), VerdLit, ringSpr);
            ring.gameObject.name = "LogoRing";
            var pinTex = Resources.Load<Texture2D>("WonJjigeo/pin");
            if (pinTex)
            {
                var pinSpr = Sprite.Create(pinTex, new Rect(0, 0, pinTex.width, pinTex.height), new Vector2(0.5f, 0.5f));
                var pi = Img("LogoPin", logo, new Vector2(0.5f, 0.5f), new Vector2(-18, 58), new Vector2(92, 92), Color.white, pinSpr);
                pi.rectTransform.localRotation = Quaternion.Euler(0, 0, 8);
            }
            var logoShadow = Txt(logo, "원 찍어", new Vector2(0.5f, 0.5f), new Vector2(4, -6), 92, new Color(0.05f, 0.03f, 0.02f, 0.85f), 380);
            var logoT = Txt(logo, "원 찍어", new Vector2(0.5f, 0.5f), Vector2.zero, 92, Cream, 380);
            logoT.outlineWidth = 0.22f; logoT.outlineColor = new Color32(31, 107, 90, 255);
            logoShadow.outlineWidth = 0.22f; logoShadow.outlineColor = new Color32(10, 6, 4, 220);
            var tagBg = Img("TagBg", titleRoot, new Vector2(0.5f, 1f), new Vector2(0, -262), new Vector2(230, 36), new Color(0.1f, 0.08f, 0.06f, 0.78f), roundSpr);
            var tagT = Txt(tagBg.transform, "중심에 핀을 박아라", new Vector2(0.5f, 0.5f), Vector2.zero, 21, Cream, 230);
            tagT.characterSpacing = 4;
            // 주 CTA (단 하나)
            ctaRt = Img("Cta", titleRoot, new Vector2(0.5f, 0f), new Vector2(0, 150), new Vector2(236, 70), Hex("B98A56"), roundSpr).rectTransform;
            Img("CtaEdge", ctaRt, new Vector2(0.5f, 0.5f), new Vector2(0, -5), new Vector2(236, 70), Hex("5A3E24"), roundSpr).transform.SetAsFirstSibling();
            var ctaFace = Img("CtaFace", ctaRt, new Vector2(0.5f, 0.5f), new Vector2(0, 2), new Vector2(226, 60), Hex("D6AC72"), roundSpr);
            var mask = R("ShineMask", ctaRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(226, 64));
            mask.gameObject.AddComponent<RectMask2D>();
            ctaShine = Img("Shine", mask, new Vector2(0.5f, 0.5f), new Vector2(-200, 0), new Vector2(34, 110), new Color(1, 1, 1, 0.35f)).rectTransform;
            ctaShine.localRotation = Quaternion.Euler(0, 0, -22);
            var ctaT = Txt(ctaRt, "각인 시작", new Vector2(0.5f, 0.5f), new Vector2(0, 2), 30, Hex("2A1A0E"), 220);
            ctaT.fontStyle = FontStyles.Bold;
            ctaFace.transform.SetSiblingIndex(1);
            bestTxt = Txt(titleRoot, "", new Vector2(0.5f, 0f), new Vector2(0, 92), 17, Hex("BFAF94"), 360);
            var badge = Img("GradeBadge", titleRoot, new Vector2(0.5f, 0f), new Vector2(0, 52), new Vector2(300, 34), new Color(0.12f, 0.1f, 0.08f, 0.85f), roundSpr);
            Txt(badge.transform, "중2 · 삼각형의 성질 · 외심과 내심", new Vector2(0.5f, 0.5f), Vector2.zero, 16, Hex("D8C9AE"), 300);

            // ── HUD
            hudRoot = R("Hud", ct, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            hudRoot.anchorMin = Vector2.zero; hudRoot.anchorMax = Vector2.one; hudRoot.sizeDelta = Vector2.zero;
            var tbg = Img("TimerBg", hudRoot, new Vector2(0.5f, 1f), new Vector2(0, -6), new Vector2(0, 8), new Color(0.1f, 0.08f, 0.06f, 0.9f));
            tbg.rectTransform.anchorMin = new Vector2(0, 1); tbg.rectTransform.anchorMax = new Vector2(1, 1); tbg.rectTransform.sizeDelta = new Vector2(0, 8);
            var tf = Img("TimerFill", tbg.transform, new Vector2(0, 0.5f), Vector2.zero, Vector2.zero, VerdLit);
            timerFill = tf.rectTransform;
            timerFill.anchorMin = new Vector2(0, 0); timerFill.anchorMax = new Vector2(1, 1); timerFill.pivot = new Vector2(0, 0.5f); timerFill.sizeDelta = Vector2.zero;
            // 미션 뱃지(청동 명패)
            var badgeImg = Img("Badge", hudRoot, new Vector2(0.5f, 1f), new Vector2(0, -58), new Vector2(214, 78), Hex("2A2420"), roundSpr);
            badgeRt = badgeImg.rectTransform;
            Img("BadgeFace", badgeRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(206, 70), Hex("3A302A"), roundSpr);
            badgeBig = Txt(badgeRt, "외심", new Vector2(0.5f, 0.5f), new Vector2(0, 10), 38, Cream, 190);
            badgeBig.outlineWidth = 0.2f; badgeBig.outlineColor = new Color32(31, 107, 90, 255);
            badgeSub = Txt(badgeRt, "외심의 위치를 나타내시오", new Vector2(0.5f, 0.5f), new Vector2(0, -22), 13, Hex("CDBB9C"), 200);
            badgeSub.textWrappingMode = TextWrappingModes.NoWrap; badgeSub.enableAutoSizing = true; badgeSub.fontSizeMin = 8; badgeSub.fontSizeMax = 13;
            badgeSub.rectTransform.sizeDelta = new Vector2(200, 20);
            platesTxt = Txt(hudRoot, "", new Vector2(0f, 1f), new Vector2(74, -40), 20, Cream, 140, TextAlignmentOptions.Left);
            scoreTxt = Txt(hudRoot, "", new Vector2(0f, 1f), new Vector2(74, -66), 17, Hex("CDBB9C"), 140, TextAlignmentOptions.Left);
            comboTxt = Txt(hudRoot, "", new Vector2(0f, 1f), new Vector2(74, -90), 17, VerdLit, 140, TextAlignmentOptions.Left);
            timeTxt = Txt(hudRoot, "", new Vector2(1f, 1f), new Vector2(-66, -92), 30, Cream, 90, TextAlignmentOptions.Right);
            Txt(hudRoot, "초", new Vector2(1f, 1f), new Vector2(-8, -98), 15, Hex("CDBB9C"), 24, TextAlignmentOptions.Left);
            bannerBg = Img("Banner", hudRoot, new Vector2(0.5f, 1f), new Vector2(0, -122), new Vector2(360, 38), new Color(0.1f, 0.08f, 0.06f, 0.82f), roundSpr);
            bannerTxt = Txt(bannerBg.transform, "", new Vector2(0.5f, 0.5f), Vector2.zero, 17, Cream, 350);
            hintTxt = Txt(hudRoot, "", new Vector2(0.5f, 0f), new Vector2(0, 40), 17, Hex("E9D9BC"), 370);
            hintTxt.outlineWidth = 0.2f; hintTxt.outlineColor = new Color32(18, 14, 11, 255);

            // ── 결과
            endRoot = R("End", ct, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            endRoot.anchorMin = Vector2.zero; endRoot.anchorMax = Vector2.one; endRoot.sizeDelta = Vector2.zero;
            var plaque = Img("EndPlaque", endRoot, new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(320, 330), Hex("2A2420"), roundSpr);
            Img("EndFace", plaque.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(308, 318), Hex("3A302A"), roundSpr);
            var eRing = Img("EndRing", plaque.transform, new Vector2(0.5f, 1f), new Vector2(0, -8), new Vector2(64, 64), VerdLit, ringSpr);
            endTitle = Txt(plaque.transform, "", new Vector2(0.5f, 1f), new Vector2(0, -68), 26, Cream, 300);
            endPlates = Txt(plaque.transform, "", new Vector2(0.5f, 0.5f), new Vector2(0, 42), 54, Cream, 300);
            endPlates.outlineWidth = 0.2f; endPlates.outlineColor = new Color32(31, 107, 90, 255);
            endScore = Txt(plaque.transform, "", new Vector2(0.5f, 0.5f), new Vector2(0, -12), 19, Hex("CDBB9C"), 300);
            endCombo = Txt(plaque.transform, "", new Vector2(0.5f, 0.5f), new Vector2(0, -40), 17, Hex("CDBB9C"), 300);
            endBest = Txt(plaque.transform, "", new Vector2(0.5f, 0.5f), new Vector2(0, -68), 16, VerdLit, 300);
            endCtaRt = Img("EndCta", endRoot, new Vector2(0.5f, 0.5f), new Vector2(0, -180), new Vector2(236, 66), Hex("B98A56"), roundSpr).rectTransform;
            Img("EndCtaFace", endCtaRt, new Vector2(0.5f, 0.5f), new Vector2(0, 2), new Vector2(226, 56), Hex("D6AC72"), roundSpr);
            var m2 = R("ShineMask", endCtaRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(226, 60));
            m2.gameObject.AddComponent<RectMask2D>();
            endCtaShine = Img("Shine", m2, new Vector2(0.5f, 0.5f), new Vector2(-200, 0), new Vector2(30, 100), new Color(1, 1, 1, 0.35f)).rectTransform;
            endCtaShine.localRotation = Quaternion.Euler(0, 0, -22);
            Txt(endCtaRt, "다시 각인", new Vector2(0.5f, 0.5f), new Vector2(0, 2), 28, Hex("2A1A0E"), 220);
            _ = eRing;
        }

        bool Over(RectTransform rt, Vector2 screen) => rt && rt.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(rt, screen, null);

        // ─────────────────────────────── 화면 전환
        void ShowTitle()
        {
            st.phase = "title";
            titleRoot.gameObject.SetActive(true);
            hudRoot.gameObject.SetActive(false);
            endRoot.gameObject.SetActive(false);
            bestTxt.text = best > 0 ? $"최고 각인 {best}장 · {bestScore}점" : "90초 동안 몇 장이나 새길까";
            foreach (var p in pins) { p.root.SetActive(false); }
            // 데모 명판(첫 문항과 같은 모양)
            if (demoPlate == null) demoPlate = TakePlate();
            if (demoPlaced == null) demoPlaced = Layout.Place(Gen.First(), 0, new System.Random(3));
            var dp = demoPlaced;
            var P = new V2[3]; for (int i = 0; i < 3; i++) P[i] = dp.P[i] - dp.O;
            ShapePlate(demoPlate, P);
            demoPlate.go.SetActive(true);
            demoPlate.go.transform.position = demoPlate.pivot;
            demoPlate.go.transform.localScale = Vector3.one;
            demoPlate.go.transform.rotation = Quaternion.identity;
            demoPin.root.SetActive(true);
            camBlend = 0f;
            MgfBridge.NotifyChanged();
        }

        void Begin()
        {
            FinishRound();
            StopAllCoroutines();
            roundCo = null; roundPending = false;
            st.score = 0; st.lives = 3; st.level = 1; st.solved = 0; st.combo = 0; st.maxCombo = 0; st.plates = 0; st.scraps = 0;
            st.phase = "playing";
            Time.timeScale = 1f; hitStop = 0;
            shownScore = 0; shownPlates = -1; shownScoreInt = -1; shownTimeSec = -1;
            playT = 0; idleT = 0; busy = false; frozen = true; onbMisses = 0; firstPlantDone = false; lastBand = 1; bandIndex = 0; platesInBand = 0;
            rng = new System.Random(System.Environment.TickCount);
            gen.Reseed(System.Environment.TickCount ^ 0x5bd1e995);
            // 개발용 미리보기: ?wjt=55 면 그 시각(밴드)부터 시작한다(게시 흐름과 무관, QA 는 쓰지 않는다)
            try
            {
                string url = Application.absoluteURL ?? "";
                int i = url.IndexOf("wjt=");
                if (i >= 0 && int.TryParse(url.Substring(i + 4).Split('&')[0], out int t0)) { playT = Mathf.Clamp(t0, 0, 85); }
            }
            catch (System.Exception) { }
            titleRoot.gameObject.SetActive(false);
            endRoot.gameObject.SetActive(false);
            hudRoot.gameObject.SetActive(true);
            if (demoPlate != null) { demoPlate.go.SetActive(false); demoPlate = null; }
            demoPin.root.SetActive(false);
            foreach (var p in rack) p.go.SetActive(false);
            rack.Clear();
            foreach (var p in scrapPlates) p.go.SetActive(false);
            scrapPlates.Clear();
            for (int i = 0; i < 2; i++) scrapZones[i].gameObject.SetActive(false);
            if (curPlate != null) curPlate.go.SetActive(false);
            curPlate = null;
            HideRoundLines();
            for (int i = 0; i < 3; i++)
            {
                pins[i].bent = false; SetPinMat(pins[i], brassMat); pins[i].root.SetActive(true);
                pins[i].body.localRotation = Quaternion.identity;
            }
            aiming = false; aimPin = null; dragFromAim = false; drag = DragKind.None;
            PlacePinsInTray();
            SpawnPlate(Gen.First());
            ShowBanner("명판의 외심 자리를 누르면 핀이 박힌다", 0);
            MgfBridge.NotifyChanged();
        }

        void EndGame()
        {
            bool timeUp = st.lives > 0;
            st.phase = st.plates > 0 && timeUp ? "clear" : "gameover";
            MgfBridge.NotifyChanged();
        }

        void ShowEnd()
        {
            hudRoot.gameObject.SetActive(true);
            endRoot.gameObject.SetActive(true);
            bannerBg.gameObject.SetActive(false);
            hintTxt.text = "";
            bool newBest = st.plates > best || (st.plates == best && st.score > bestScore && st.plates > 0);
            if (newBest) { best = st.plates; bestScore = st.score; PlayerPrefs.SetInt("wonjjigeo.best", best); PlayerPrefs.SetInt("wonjjigeo.bestScore", bestScore); PlayerPrefs.Save(); }
            endTitle.text = st.lives <= 0 ? "핀이 모두 굽었다" : "시간 종료";
            endCountT = 0; endShownAt = Time.time;
            endPlates.text = st.plates == 0 ? "명판 0장" : "각인 0장";
            endScore.text = "";
            endCombo.text = st.plates == 0 ? "외심은 세 꼭짓점, 내심은 세 변까지 거리가 같다" : $"최대 연속 {st.maxCombo}";
            endBest.text = newBest && st.plates > 0 ? "최고 기록 갱신" : best > 0 ? $"최고 각인 {best}장" : "";
            Play(st.lives <= 0 ? clThunk : clChime, 0.7f);
        }

        // ─────────────────────────────── 명판 투입
        int CurrentBand() => playT < 25f ? 1 : playT < 55f ? 2 : 3;

        /// <summary>명판 위 표시(꼭짓점 이름·이등변 틱·직각 표시·역추적 원). 등장 때와 온보딩 재시도 때 쓴다.</summary>
        void ShowPlateMarks()
        {
            // 꼭짓점 이름(삼각형 바깥쪽), 이등변 틱, 직각 표시
            var inc = Geo.Incenter(cur.spec, cur.P);
            for (int i = 0; i < 3; i++)
            {
                var dir = (cur.P[i] - inc); double l = dir.Len; dir = dir * (1.0 / l);
                vLabels[i].transform.position = W(cur.P[i] + dir * 0.42, PlateH + 0.02f);
                vLabels[i].gameObject.SetActive(true);
            }
            for (int i = 0; i < 2; i++) isoTicks[i].gameObject.SetActive(false);
            if (cur.spec.kind == Kind.Iso)
            {
                for (int k = 0; k < 2; k++)
                {
                    var a = cur.P[0]; var b = cur.P[k + 1];
                    var m = (a + b) * 0.5; var d = b - a; var n = new V2(-d.y, d.x) * (0.17 / d.Len);
                    isoTicks[k].SetPosition(0, W(m - n, PlateH + 0.03f)); isoTicks[k].SetPosition(1, W(m + n, PlateH + 0.03f));
                    SetColor(isoTicks[k], Cream);
                    isoTicks[k].gameObject.SetActive(true);
                }
            }
            rightMark.gameObject.SetActive(false);
            int rv = cur.spec.RightVertex;
            if (rv >= 0)
            {
                var v0 = cur.P[rv]; var e1 = cur.P[(rv + 1) % 3] - v0; var e2 = cur.P[(rv + 2) % 3] - v0;
                e1 = e1 * (0.34 / e1.Len); e2 = e2 * (0.34 / e2.Len);
                rightMark.SetPosition(0, W(v0 + e1, PlateH + 0.03f)); rightMark.SetPosition(1, W(v0 + e1 + e2, PlateH + 0.03f)); rightMark.SetPosition(2, W(v0 + e2, PlateH + 0.03f));
                SetColor(rightMark, Cream);
                rightMark.gameObject.SetActive(true);
            }

            // 역추적: 원이 먼저 그려져 있다
            ghostCircle.gameObject.SetActive(cur.spec.task == Task.Reverse);
            if (cur.spec.task == Task.Reverse)
            {
                var c = cur.spec.incenter ? cur.ans : cur.O;
                float r = (float)(cur.spec.incenter ? cur.inR : cur.R);
                CirclePts(ghostCircle, W(c, PlateH + 0.03f), r, 97);
                SetColor(ghostCircle, new Color(Cream.r, Cream.g, Cream.b, 0.85f));
            }
        }

        void SpawnPlate(PlateSpec spec = null)
        {
            int band = CurrentBand();
            if (band != lastBand)
            {
                lastBand = band; platesInBand = 0;
                ShowBanner(band == 2 ? "내심 명판 투입 · 뱃지를 읽어라" : "직각·둔각 명판 투입", 2.4f);
            }
            if (spec == null) spec = gen.Make(band, platesInBand);
            platesInBand++;
            st.level = band;
            cur = Layout.Place(spec, st.scraps, rng);
            curPlate = TakePlate();
            ShapePlate(curPlate, cur.P);
            curPlate.go.SetActive(true);
            curPlate.mr.sharedMaterial = bronzeMat;
            curPlate.go.transform.rotation = Quaternion.identity;
            curPlate.go.transform.localScale = Vector3.one;
            curPlate.go.transform.position = curPlate.pivot + Vector3.down * 0.3f;
            plateEnterT = 0f;
            for (int i = 0; i < 3; i++) plateEdge.SetPosition(i, lastInset[i]);
            SetColor(plateEdge, new Color(1f, 0.86f, 0.62f, 0.55f));
            plateEdge.gameObject.SetActive(false);
            ShowPlateMarks();
            // 오류 찾기: 잘못 박힌 핀과 그 핀이 만든 원
            prePinIn = spec.task == Task.Fix;
            prePin.root.SetActive(prePinIn);
            fixCircle.gameObject.SetActive(prePinIn);
            if (prePinIn)
            {
                var wp = spec.fixAtCentroid ? cur.G : cur.other;   // M4 / M1
                prePin.root.transform.position = W(wp, PlateH - 0.04f);
                prePin.root.transform.rotation = Quaternion.identity;
                prePin.body.localRotation = Quaternion.identity;
                float r = (float)(spec.incenter ? AvgSideDist(wp) : AvgVertDist(wp));
                CirclePts(fixCircle, W(wp, PlateH + 0.03f), r, 97);
                SetColor(fixCircle, new Color(Rust.r * 1.4f, Rust.g * 1.4f, Rust.b * 1.4f, 0.9f));
            }

            ClearAim();
            st.mission = spec.Mission;
            st.task = spec.task.ToString();
            string sub = spec.task == Task.Reverse ? $"그려진 원의 중심인 {spec.Mission}의 위치를 나타내시오"
                : spec.task == Task.Fix ? $"잘못 박힌 핀을 뽑고 {spec.Mission}의 위치를 나타내시오"
                : $"{spec.Mission}의 위치를 나타내시오";
            if (badgeBig.text != spec.Mission) { badgeNext = spec.Mission; badgeFlipT = 0; }
            badgeSub.text = sub;
            hintTxt.text = spec.task == Task.Fix ? "잘못 박힌 회색 핀을 끌어 뽑아라" : "";
            idleT = 0;
            MgfBridge.NotifyChanged();
        }

        double AvgVertDist(V2 p) => ((p - cur.P[0]).Len + (p - cur.P[1]).Len + (p - cur.P[2]).Len) / 3;
        double AvgSideDist(V2 p) => (Geo.LineDist(p, cur.P[0], cur.P[1]) + Geo.LineDist(p, cur.P[1], cur.P[2]) + Geo.LineDist(p, cur.P[2], cur.P[0])) / 3;

        static V2 Foot(V2 x, V2 p, V2 q)
        {
            var d = q - p; double t = ((x.x - p.x) * d.x + (x.y - p.y) * d.y) / (d.x * d.x + d.y * d.y);
            return p + d * t;
        }

        void ShowBanner(string s, float secs)
        {
            bannerTxt.text = s;
            bannerBg.gameObject.SetActive(true);
            bannerT = secs;   // 0 = 첫 성공까지 유지
        }

        // ─────────────────────────────── 트레이·핀
        void PlacePinsInTray()
        {
            for (int i = 0; i < 3; i++)
            {
                var p = pins[i];
                if (drag == DragKind.TrayPin && dragPin == p) continue;
                if (roundPending && roundPinSlot == i) continue;
                if (aiming && aimPin == p) continue;
                p.root.SetActive(true);
                if (p.bent)
                {
                    p.root.transform.position = SlotPos(i) + new Vector3(0.1f, 0.22f, 0.05f);
                    p.root.transform.rotation = Quaternion.Euler(0, 0, 82);
                }
                else
                {
                    p.root.transform.position = SlotPos(i) + Vector3.up * 0.18f;
                    p.root.transform.rotation = Quaternion.identity;
                }
            }
        }

        Vector3 SlotPos(int i) => tray.position + new Vector3(-1.4f + 1.4f * i, 0, 0);

        /// <summary>화면 좌표에서 핀(끝~머리 선분)까지의 거리로 고른다 — 원근 때문에 머리가 끝보다 위로 보이는 것을 흡수.</summary>
        Pin NearestTrayPin(Vector2 screen)
        {
            Pin best = null;
            float bd = Mathf.Max(34f, 0.085f * Mathf.Min(Screen.width, Screen.height));
            foreach (var p in pins)
            {
                if (p.bent) continue;
                if (roundPending && roundPinSlot == p.slot) continue;
                if (aiming && aimPin == p) continue;
                Vector2 a = cam.WorldToScreenPoint(SlotPos(p.slot) + Vector3.up * 0.18f);
                Vector2 b = cam.WorldToScreenPoint(SlotPos(p.slot) + Vector3.up * 1.0f);
                var ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(screen - a, ab) / Mathf.Max(1e-3f, ab.sqrMagnitude));
                float d = Vector2.Distance(screen, a + ab * t);
                if (d < bd) { bd = d; best = p; }
            }
            return best;
        }

        Pin FirstFreePin()
        {
            foreach (var p in pins) if (!p.bent && !(roundPending && roundPinSlot == p.slot)) return p;
            return null;
        }

        // ─────────────────────────────── 레이아웃·카메라
        void Layout2()
        {
            bool l = cam.aspect >= 1.0f;
            if (l != land || tray.position == Vector3.zero)
            {
                land = l;
                tray.position = land ? new Vector3(0, -0.12f, -5.75f) : new Vector3(0, -0.12f, -6.1f);
                rackRail.position = land ? new Vector3(-8.2f, -0.2f, 0) : new Vector3(0, -0.2f, -8.5f);
                rackRail.rotation = land ? Quaternion.identity : Quaternion.Euler(0, 90, 0);
                chute.gameObject.SetActive(land);
                chute.position = new Vector3(8.2f, -0.1f, 0);
                if (st.phase != "title") PlacePinsInTray();
                for (int i = 0; i < rack.Count; i++) SnapRack(rack[i], i);
                // 세로는 폭 기준, 가로는 높이 기준으로 UI 를 맞춘다(가로에서 UI 가 무대를 덮지 않게)
                canvas.GetComponent<CanvasScaler>().matchWidthOrHeight = land ? 1f : 0f;
                rackLabel.transform.position = land ? new Vector3(-8.2f, 0.1f, -3.9f) : new Vector3(0f, 0.1f, -9.85f);
                rackLabel.transform.rotation = land ? Quaternion.Euler(90, 0, 0) : Quaternion.Euler(90, 0, 0);
                // 힌트 줄: 세로는 트레이 아래 빈 곳, 가로는 오른쪽 거터(슈트 위)
                var hr = hintTxt.rectTransform;
                if (land) { hr.anchorMin = hr.anchorMax = new Vector2(1f, 0.5f); hr.anchoredPosition = new Vector2(-128, 40); hr.sizeDelta = new Vector2(220, 120); }
                else { hr.anchorMin = hr.anchorMax = new Vector2(0.5f, 0f); hr.anchoredPosition = new Vector2(0, 58); hr.sizeDelta = new Vector2(370, 60); }
            }
        }

        Vector3 RackSlot(int k)
        {
            k %= 7;
            return land ? new Vector3(-8.2f, 0.05f + k * 0.03f, 3.2f - k * 1.08f) : new Vector3(-3.3f + k * 1.1f, 0.05f + k * 0.03f, -8.5f);
        }

        void SnapRack(Plate p, int k)
        {
            p.go.transform.position = RackSlot(k);
            p.go.transform.localScale = Vector3.one * 0.26f;
            p.go.transform.rotation = Quaternion.Euler(0, k * 23f, 0);
        }

        void UpdateCamera(float dt)
        {
            float pitch = 76f, fov = 36f;
            cam.fieldOfView = fov;
            float th = Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
            float a = Mathf.Max(0.3f, cam.aspect);
            // 보여야 하는 영역(월드 XZ)
            float hw = land ? 10.2f : 4.95f;
            float zTop = 4.85f, zBot = land ? -6.7f : -9.5f;
            float hudTop = land ? 0.21f : 0.19f;       // 화면 위쪽 HUD 몫
            float needH = (zTop - zBot) / (1f - hudTop - 0.03f);
            float sp = Mathf.Sin(pitch * Mathf.Deg2Rad);
            float dH = needH * 0.5f * sp / th;
            float dW = hw / (th * a);
            float d = Mathf.Max(dH, dW);
            float visH = 2f * d * th / sp;
            float cz = zTop + visH * hudTop - visH * 0.5f;
            if (!land) cz = Mathf.Max(cz, (zTop + zBot) * 0.5f + 0.3f);
            var target = new Vector3(0, 0, cz);
            var fwd = new Vector3(0, -Mathf.Sin(pitch * Mathf.Deg2Rad), Mathf.Cos(pitch * Mathf.Deg2Rad));
            var playPos = target - fwd * d;
            var playRot = Quaternion.LookRotation(fwd, Vector3.forward);

            // 타이틀: 베드 위를 천천히 도는 낮은 궤도
            float t = Time.time;
            float orbit = Mathf.Sin(t * 0.18f) * 22f;
            float tp = land ? 50f : 60f;
            float td = land ? 15f : 25f;
            var tFwd = Quaternion.Euler(0, orbit, 0) * new Vector3(0, -Mathf.Sin(tp * Mathf.Deg2Rad), Mathf.Cos(tp * Mathf.Deg2Rad));
            var tTarget = new Vector3(0, 0, land ? -0.6f : -1.2f);
            var titlePos = tTarget - tFwd * td;
            var titleRot = Quaternion.LookRotation(tFwd, Vector3.up);

            float goal = st.phase == "title" ? 0f : 1f;
            camBlend = Mathf.MoveTowards(camBlend, goal, dt * 1.6f);
            float e = camBlend * camBlend * (3 - 2 * camBlend);
            cam.transform.position = Vector3.Lerp(titlePos, playPos, e) + shakeOffset;
            cam.transform.rotation = Quaternion.Slerp(titleRot, playRot, e);
        }

        // ─────────────────────────────── 매 프레임
        void Update()
        {
            float dt = Time.deltaTime;
            Layout2();
            UpdateCamera(dt);
            UpdateUi(dt);
            if (hitStop > 0) { hitStop -= Time.unscaledDeltaTime; if (hitStop <= 0) Time.timeScale = 1f; }

            if (st.phase == "title") { TitleDemo(); TitleInput(); return; }
            if (st.phase == "gameover" || st.phase == "clear") { EndInput(); return; }

            // 명판 등장(mask-reveal-up: 베드 밑에서 떠올라 오버슈트)
            if (curPlate != null && plateEnterT < 1f)
            {
                plateEnterT = Mathf.Min(1f, plateEnterT + dt / 0.42f);
                float k = plateEnterT;
                float y = Mathf.LerpUnclamped(-0.3f, 0f, 1f + 2.4f * Mathf.Pow(k - 1f, 3) + 1.4f * Mathf.Pow(k - 1f, 2));
                curPlate.go.transform.position = curPlate.pivot + Vector3.up * y;
                if (plateEnterT >= 1f && !busy) plateEdge.gameObject.SetActive(true);
            }

            // 시간(명판 스윕 중·온보딩 대기 중에는 멈춘다)
            if (!busy && !frozen)
            {
                playT += dt;
                if (playT >= PlayTime) { playT = PlayTime; EndGame(); ShowEnd(); return; }
            }
            if (bannerT > 0) { bannerT -= dt; if (bannerT <= 0) bannerBg.gameObject.SetActive(false); }

            PlayInput();
            UpdateAim();
            UpdateOnboarding(dt);
            UpdateRipples(dt);
            UpdateWiggles(dt);
            dashMat.mainTextureOffset = new Vector2(-Time.time * 1.6f, 0);
        }

        void TitleDemo()
        {
            if (demoPlate == null) return;
            // 3.6초 주기: 핀이 떠 있다가 내리꽂히고, 녹청 원이 스윕한 뒤 사라진다(유령 원)
            float c = Time.time % 3.6f;
            var dp = demoPlaced;
            var ansW = W(dp.ans - dp.O, 0);   // 데모 명판은 O 를 원점에 둔 좌표로 만들었다
            float lift = c < 1.0f ? 1.2f + Mathf.Sin(Time.time * 3f) * 0.08f : c < 1.12f ? Mathf.Lerp(1.2f, 0f, (c - 1.0f) / 0.12f) : 0f;
            demoPin.root.transform.position = ansW + Vector3.up * (PlateH - 0.04f + lift);
            demoPin.root.transform.rotation = Quaternion.Euler(0, 0, c < 1f ? Mathf.Sin(Time.time * 2.4f) * 6f : 0f);
            if (c > 1.12f && c < 3.3f)
            {
                float f = Mathf.Clamp01((c - 1.2f) / 0.8f);
                groove.gameObject.SetActive(true); grooveUnder.gameObject.SetActive(true);
                CirclePts(groove, ansW + Vector3.up * (PlateH + 0.03f), (float)dp.R, 97, f);
                CirclePts(grooveUnder, ansW + Vector3.up * (PlateH + 0.02f), (float)dp.R, 97, f);
                float fade = c > 2.8f ? 1f - (c - 2.8f) / 0.5f : 1f;
                SetColor(groove, new Color(VerdLit.r, VerdLit.g, VerdLit.b, fade));
                SetColor(grooveUnder, new Color(0.05f, 0.1f, 0.08f, 0.8f * fade));
            }
            else { groove.gameObject.SetActive(false); grooveUnder.gameObject.SetActive(false); }
        }


        void TitleInput()
        {
            ctaShine.anchoredPosition = new Vector2(-200 + (Time.time % 2.6f) / 2.6f * 520f, 0);
            float br = 1f + Mathf.Sin(Time.time * 2.2f) * 0.025f;
            if (MgfPointer.Down) { ctaDown = true; ctaPressT = 0; }
            if (ctaDown)
            {
                ctaRt.localScale = Vector3.one * 0.93f;
                if (MgfPointer.Up) { ctaDown = false; ctaRt.localScale = Vector3.one * 1.08f; Begin(); sfx.PlayOneShot(clClang, 0.6f); }
            }
            else ctaRt.localScale = Vector3.Lerp(ctaRt.localScale, Vector3.one * br, Time.deltaTime * 10f);
        }

        void EndInput()
        {
            endCtaShine.anchoredPosition = new Vector2(-200 + (Time.time % 2.6f) / 2.6f * 520f, 0);
            if (Time.time - endShownAt < 0.6f) return;
            if (MgfPointer.Down && Over(endCtaRt, MgfPointer.Position)) { endCtaDown = true; endCtaRt.localScale = Vector3.one * 0.93f; }
            if (endCtaDown && MgfPointer.Up) { endCtaDown = false; endCtaRt.localScale = Vector3.one; Begin(); sfx.PlayOneShot(clClang, 0.6f); }
        }

        // ─────────────────────────────── 입력(플레이)
        bool PointerWorld(out Vector3 p) => MgfPointer.OnPlane(cam, 0.1f, out p);

        void PlayInput()
        {
            if (MgfPointer.Down)
            {
                st.pokes++;
                MgfBridge.NotifyChanged();
                downScreen = MgfPointer.Position;
                dragMoved = false;
                if (!PointerWorld(out var wp)) return;
                downWorld = wp;
                if (busy)
                {
                    // 스윕 중: 명판만 좌우로 흔들고 이유 한 줄
                    Refuse(null, "원이 끝날 때까지 기다려라");
                    return;
                }
                var tp = NearestTrayPin(MgfPointer.Position);
                if (prePinIn && (V(wp) - V(prePin.root.transform.position)).Len < 0.7)
                {
                    drag = DragKind.PrePin;
                    Play(clTick, 0.5f);
                }
                else if (tp != null)
                {
                    ClearAim();
                    drag = DragKind.TrayPin; dragPin = tp;
                    Play(clTick, 0.5f);
                    dragTip = wp;
                }
                else if (aiming && (V(wp) - V(aimPt)).Len < 0.6)
                {
                    // 떠 있는 조준 핀을 다시 누름: 그대로 떼면 박고, 끌면 옮겨서 박는다
                    drag = DragKind.TrayPin; dragPin = aimPin; dragFromAim = true;
                    dragTip = aimPt;
                }
                else if (Layout.OnBed(V(wp), st.scraps) && FirstFreePin() != null)
                {
                    // 베드 탭: 트레이 핀이 그 점 위로 날아온다. 떼면 그 자리에 바로 박힌다(끌면 옮겨서 박는다)
                    var a = wp; a.y = PlateH;
                    ClearAim();
                    drag = DragKind.TrayPin; dragPin = FirstFreePin(); dragFromAim = true;
                    aimPt = a; dragTip = a;
                    dragPin.root.transform.position = a + Vector3.up * 1.05f;
                    dragPin.root.transform.rotation = Quaternion.Euler(6, 0, -5);
                    ShowReticle(a, true);
                    Play(clTick, 0.45f);
                }
                else
                {
                    // 빈 곳 탭: 그 자리에 리플 + 핀 트레이가 흔들린다
                    Ripple(wp);
                    trayShakeT = 0;
                    Play(clTick, 0.25f);
                    if (!firstPlantDone || idleT > 3f) hintTxt.text = "명판 위 한 점을 누르면 그 자리에 핀이 박힌다";
                    return;
                }
            }
            if (drag == DragKind.None) return;
            if (!PointerWorld(out var cw)) return;
            bool released = MgfPointer.Up || !MgfPointer.Held;   // 같은 프레임 탭·캔버스 밖에서 뗀 경우도 놓은 것으로
            if ((MgfPointer.Position - downScreen).magnitude > 12f) dragMoved = true;

            if (drag == DragKind.TrayPin)
            {
                // 터치면 손가락에 가리지 않게 핀 끝을 화면 위쪽으로 조금 띄운다(조준점이 곧 핀 끝)
                var tip = cw + (Input.touchCount > 0 ? new Vector3(0, 0, 0.9f) : Vector3.zero);
                tip.y = PlateH;
                dragTip = tip;
                if (dragMoved)
                {
                    float wob = Mathf.Sin(Time.time * 9f) * 3f;
                    dragPin.root.transform.position = tip + Vector3.up * 1.05f;
                    dragPin.root.transform.rotation = Quaternion.Euler(8 + wob, 0, -6);
                    ShowReticle(tip, true);
                }
                if (released)
                {
                    drag = DragKind.None;
                    reticle.gameObject.SetActive(false); reticleX.gameObject.SetActive(false); reticleY.gameObject.SetActive(false);
                    bool fromAim = dragFromAim; dragFromAim = false;
                    if (!dragMoved && fromAim) tip = aimPt;          // 베드를 탭했다 → 누른 자리에 박는다
                    else if (!dragMoved)
                    {
                        // 탭만 했다: 핀이 흔들리며 끌어라는 점선이 다시 재생된다
                        pinWiggle = dragPin; pinWiggleT = 0; idleT = 8.1f;
                        hintTxt.text = "명판 위를 누르거나 핀을 끌어 놓아라";
                        PlacePinsInTray();
                        return;
                    }
                    var v = V(tip);
                    if (!Layout.OnBed(v, st.scraps))
                    {
                        ClearAim();
                        Play(clRefuse, 0.5f);
                        hintTxt.text = "명판이 있는 베드 위에 놓아라";
                        PlacePinsInTray();
                        return;
                    }
                    if (prePinIn)
                    {
                        ClearAim();
                        Refuse(prePin.root.transform, "먼저 잘못 박힌 회색 핀을 뽑아라");
                        PlacePinsInTray();
                        return;
                    }
                    aiming = false; aimPin = null;
                    Plant(v, dragPin);
                }
            }
            else if (drag == DragKind.PrePin)
            {
                var basePos = W(cur.spec.fixAtCentroid ? cur.G : cur.other, PlateH - 0.04f);
                float pull = Mathf.Min(1.4f, (cw - downWorld).magnitude);
                prePin.root.transform.position = basePos + Vector3.up * pull * 0.8f + (cw - downWorld) * 0.35f;
                if (released)
                {
                    drag = DragKind.None;
                    if (pull > 0.6f) PullPrePin();
                    else { prePin.root.transform.position = basePos; Refuse(prePin.root.transform, "더 멀리 끌어서 뽑아라"); }
                }
            }
        }

        void PullPrePin()
        {
            if (!prePinIn) return;
            prePinIn = false;
            StartCoroutine(FlyAway(prePin.root.transform));
            fixCircle.gameObject.SetActive(false);
            Play(clPull, 0.7f);
            hintTxt.text = $"이제 {cur.spec.Mission}에 새 핀을 박아라";
            MgfBridge.NotifyChanged();
        }

        IEnumerator FlyAway(Transform t)
        {
            var a = t.position; var b = a + new Vector3(3f, 3f, 2f);
            for (float e = 0; e < 0.35f; e += Time.deltaTime) { t.position = Vector3.Lerp(a, b, e / 0.35f); t.rotation = Quaternion.Euler(0, 0, e * 400f); yield return null; }
            t.gameObject.SetActive(false);
        }

        // 거절 연출(대상 좌우 흔들림 + 이유 한 문장 + 전용 거절음) — 화면 흔들림은 쓰지 않는다
        Transform wiggleT; float wiggleTime = 9f; Vector3 wiggleBase;
        Pin pinWiggle; float pinWiggleT = 9f;
        void Refuse(Transform target, string why)
        {
            if (target == null && curPlate != null) target = curPlate.go.transform;
            if (wiggleT != null && wiggleTime < 0.25f) wiggleT.position = wiggleBase;
            wiggleT = target; wiggleTime = 0; wiggleBase = target ? target.position : Vector3.zero;
            hintTxt.text = why;
            Play(clRefuse, 0.5f);
        }

        void UpdateWiggles(float dt)
        {
            if (wiggleT != null && wiggleTime < 0.25f)
            {
                wiggleTime += dt;
                float k = wiggleTime / 0.25f;
                wiggleT.position = wiggleBase + Vector3.right * Mathf.Sin(k * Mathf.PI * 5) * 0.12f * (1 - k);
                if (wiggleTime >= 0.25f) wiggleT.position = wiggleBase;
            }
            if (trayShakeT < 0.3f)
            {
                trayShakeT += dt;
                float k = trayShakeT / 0.3f;
                var basePos = land ? new Vector3(0, -0.12f, -5.75f) : new Vector3(0, -0.12f, -6.1f);
                tray.position = basePos + Vector3.right * Mathf.Sin(k * Mathf.PI * 4) * 0.1f * (1 - k);
                if (trayShakeT >= 0.3f) tray.position = basePos;
                PlacePinsInTray();
            }
            if (pinWiggle != null && pinWiggleT < 0.5f && drag == DragKind.None)
            {
                pinWiggleT += dt;
                float k = pinWiggleT / 0.5f;
                pinWiggle.root.transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(k * Mathf.PI * 6) * 14f * (1 - k));
                pinWiggle.root.transform.position = SlotPos(pinWiggle.slot) + Vector3.up * (0.18f + Mathf.Sin(k * Mathf.PI) * 0.35f);
            }
        }

        void ShowReticle(Vector3 tip, bool on)
        {
            reticle.gameObject.SetActive(on); reticleX.gameObject.SetActive(on); reticleY.gameObject.SetActive(on);
            if (!on) return;
            var c = new Color(Cream.r, Cream.g, Cream.b, 0.95f);
            CirclePts(reticle, tip + Vector3.up * 0.04f, 0.3f, 49);
            SetColor(reticle, c); SetColor(reticleX, c); SetColor(reticleY, c);
            reticleX.SetPosition(0, tip + new Vector3(-0.5f, 0.04f, 0)); reticleX.SetPosition(1, tip + new Vector3(0.5f, 0.04f, 0));
            reticleY.SetPosition(0, tip + new Vector3(0, 0.04f, -0.5f)); reticleY.SetPosition(1, tip + new Vector3(0, 0.04f, 0.5f));
        }

        void Ripple(Vector3 wp)
        {
            int i = rippleNext++ % 3;
            rippleT[i] = 0;
            ripples[i].gameObject.SetActive(true);
            ripples[i].transform.position = wp;   // 중심 기억용
        }

        void UpdateRipples(float dt)
        {
            for (int i = 0; i < 3; i++)
            {
                if (rippleT[i] > 0.6f) continue;
                rippleT[i] += dt;
                float k = Mathf.Clamp01(rippleT[i] / 0.6f);
                CirclePts(ripples[i], ripples[i].transform.position + Vector3.up * 0.05f, 0.15f + k * 0.9f, 49);
                SetColor(ripples[i], new Color(Cream.r, Cream.g, Cream.b, 1 - k));
                if (rippleT[i] > 0.6f) ripples[i].gameObject.SetActive(false);
            }
        }

        // ─────────────────────────────── 온보딩: 트레이 핀 위 맥동 링 + 핀 → 명판 점선 경로 + 명판 윤곽
        void UpdateOnboarding(float dt)
        {
            bool show = st.phase == "playing" && !busy && curPlate != null && (!firstPlantDone || idleT > 8f) && drag == DragKind.None && !prePinIn && !aiming;
            if (!busy && drag == DragKind.None) idleT += dt;
            st.onboarding = frozen;
            pulseRing.gameObject.SetActive(show);
            onbPath.gameObject.SetActive(show);
            plateOutline.gameObject.SetActive(show);
            if (!show) return;
            var p = FirstFreePin();
            if (p == null) return;
            bool big = idleT > 8f;   // 8초 동안 안 하면 더 크게
            var from = SlotPos(p.slot) + Vector3.up * 0.9f;
            float pr = (big ? 0.7f : 0.5f) + Mathf.Repeat(Time.time, 1f) * 0.35f;
            CirclePts(pulseRing, SlotPos(p.slot) + new Vector3(0, 0.95f, 0.1f), pr, 49);
            SetColor(pulseRing, new Color(VerdLit.r, VerdLit.g, VerdLit.b, 1 - Mathf.Repeat(Time.time, 1f)));
            // 경로 끝 = 트레이에 가장 가까운 명판 변의 바깥 0.3 — 정답 자리를 가리키지 않는다(명판 위로 끌라는 뜻만)
            V2 best = cur.P[0]; double bd = double.MaxValue; var fp = V(from);
            for (int i = 0; i < 3; i++)
            {
                var f = Foot(fp, cur.P[i], cur.P[(i + 1) % 3]);
                double t = ((f - cur.P[i]).Len) / (cur.P[(i + 1) % 3] - cur.P[i]).Len;
                if (t < 0.2 || t > 0.8) f = (cur.P[i] + cur.P[(i + 1) % 3]) * 0.5;
                double d = (f - fp).Len;
                if (d < bd) { bd = d; best = f; }
            }
            var to = W(best, 0.4f) + (from - W(best, 0.4f)).normalized * 0.3f;
            int n = onbPath.positionCount;
            for (int i = 0; i < n; i++)
            {
                float k = (float)i / (n - 1);
                var q = Vector3.Lerp(from, to, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.8f;
                onbPath.SetPosition(i, q);
            }
            onbPath.widthMultiplier = big ? 0.3f : 0.2f;
            SetColor(onbPath, new Color(Cream.r, Cream.g, Cream.b, 0.95f));
            for (int i = 0; i < 3; i++) plateOutline.SetPosition(i, W(cur.P[i], PlateH + 0.04f));
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 5f);
            SetColor(plateOutline, new Color(VerdLit.r, VerdLit.g, VerdLit.b, 0.4f + 0.5f * pulse));
        }

        // ─────────────────────────────── 핀 박기 → 판정 → 연출
        void Plant(V2 p, Pin pin)
        {
            if (st.phase != "playing" || cur == null || pin == null) return;
            bool ok = Geo.Hit(p, cur.ans, cur.tol);
            roundOk = ok;
            roundPinSlot = pin.slot;
            // 온보딩(첫 성공 전) 오답은 목숨·핀을 깎지 않고 같은 명판을 다시 박게 한다
            roundRetry = !ok && frozen && onbMisses < 3;
            if (roundRetry) onbMisses++;
            if (!roundRetry) frozen = false;
            if (!firstPlantDone) { firstPlantDone = true; }
            if (roundRetry) st.combo = 0;
            else if (ok)
            {
                st.combo++; st.maxCombo = Mathf.Max(st.maxCombo, st.combo);
                st.score += 100 * Mathf.Min(3, st.combo);
                st.solved++; st.plates++;
            }
            else
            {
                st.combo = 0;
                pin.bent = true;
                st.lives--;
                st.scraps = Mathf.Min(2, st.scraps + 1);
                if (st.lives <= 0) EndGame();
            }
            busy = true; roundPending = true;
            hintTxt.text = "";
            MgfBridge.NotifyChanged();
            roundCo = StartCoroutine(RoundCo(p, pin, ok));
        }

        IEnumerator RoundCo(V2 p, Pin pin, bool ok)
        {
            var spec = cur.spec;
            var tip = W(p, PlateH - 0.04f);
            var pt = pin.root.transform;
            ghostCircle.gameObject.SetActive(false);
            // 예비 동작: 살짝 들렸다가 내리꽂힌다
            var start = tip + Vector3.up * 1.05f;
            for (float e = 0; e < 0.07f; e += Time.deltaTime) { pt.position = Vector3.Lerp(start, start + Vector3.up * 0.25f, e / 0.07f); pt.rotation = Quaternion.identity; yield return null; }
            for (float e = 0; e < 0.07f; e += Time.deltaTime) { pt.position = Vector3.Lerp(start + Vector3.up * 0.25f, tip, (e / 0.07f) * (e / 0.07f)); yield return null; }
            pt.position = tip;
            // 히트스톱 + 스쿼시 + 모래 먼지
            pin.body.localScale = new Vector3(1.25f, 0.8f, 1.25f);
            MgfFx.Burst(tip + Vector3.up * 0.1f, Sand * 0.9f, 10, 1.4f, 0.12f);
            Play(ok ? clClang : clThunk, 0.85f);
            Time.timeScale = 0.02f; hitStop = 0.09f;
            yield return null;
            while (hitStop > 0) yield return null;
            for (float e = 0; e < 0.12f; e += Time.deltaTime) { pin.body.localScale = Vector3.Lerp(new Vector3(1.25f, 0.8f, 1.25f), Vector3.one, e / 0.12f); yield return null; }
            pin.body.localScale = Vector3.one;

            V2 c = p; float r;
            if (ok)
            {
                // 정답으로 판정된 뒤에만: 핀이 중심 홈에 "딸깍" 안착(정확한 원을 새기기 위해)
                var seat = W(cur.ans, PlateH - 0.04f);
                for (float e = 0; e < 0.1f; e += Time.deltaTime) { pt.position = Vector3.Lerp(tip, seat, e / 0.1f); yield return null; }
                pt.position = seat;
                Play(clSeat, 0.6f);
                c = cur.ans;
                r = (float)(spec.incenter ? cur.inR : cur.R);
            }
            else r = (float)(spec.incenter ? AvgSideDist(p) : AvgVertDist(p));

            // 컴퍼스 팔이 한 바퀴 스윕하며 홈을 판다
            var cw = W(c, PlateH + 0.03f);
            groove.gameObject.SetActive(true); grooveUnder.gameObject.SetActive(true); arm.gameObject.SetActive(true);
            var gc = ok ? Verd : Rust;
            SetColor(groove, gc); SetColor(grooveUnder, new Color(0.06f, 0.05f, 0.04f, 0.75f)); SetColor(arm, Cream);
            Play(clScrape, 0.55f);
            const float sweep = 0.72f;
            for (float e = 0; e < sweep; e += Time.deltaTime)
            {
                float f = Mathf.SmoothStep(0, 1, e / sweep);
                CirclePts(groove, cw, r, 97, f);
                CirclePts(grooveUnder, cw + Vector3.down * 0.005f, r, 97, f);
                float a = f * Mathf.PI * 2;
                arm.SetPosition(0, cw + Vector3.up * 0.3f); arm.SetPosition(1, cw + new Vector3(Mathf.Cos(a) * r, 0.02f, Mathf.Sin(a) * r));
                yield return null;
            }
            CirclePts(groove, cw, r, 97); CirclePts(grooveUnder, cw + Vector3.down * 0.005f, r, 97);
            arm.gameObject.SetActive(false);

            if (ok)
            {
                // 잠금: spring-scale-in 대신 원 굵기 펄스 + 색 번쩍 + 홈을 따라 빛 한 바퀴(shimmer)
                Play(clChime, 0.6f);
                ShowRadii(c, true);
                revealText.text = spec.incenter ? "세 변에 접한다 · 내심" : "OA=OB=OC · 외심";
                revealText.transform.position = cw + new Vector3(0, 0.1f, Mathf.Min(r, 1.6f) * 0.55f + 0.2f);
                revealText.gameObject.SetActive(true);
                shimmer.gameObject.SetActive(true);
                for (float e = 0; e < 0.9f; e += Time.deltaTime)
                {
                    float k = e / 0.9f;
                    groove.widthMultiplier = 0.09f + Mathf.Sin(Mathf.Min(1, k * 3) * Mathf.PI) * 0.07f;
                    SetColor(groove, Color.Lerp(Color.Lerp(VerdLit, Color.white, 0.5f), VerdLit, Mathf.Min(1, k * 2.5f)));
                    float a = k * Mathf.PI * 2 + Mathf.PI * 0.5f;
                    shimmer.position = cw + new Vector3(Mathf.Cos(a) * r, 0.05f, Mathf.Sin(a) * r);
                    shimmer.localScale = Vector3.one * (0.9f + Mathf.Sin(k * Mathf.PI) * 0.6f);
                    revealText.transform.localScale = Vector3.one * (k < 0.15f ? Mathf.Lerp(0.6f, 1.08f, k / 0.15f) : Mathf.Lerp(1.08f, 1f, (k - 0.15f) / 0.85f));
                    yield return null;
                }
                shimmer.gameObject.SetActive(false);
                groove.widthMultiplier = 0.09f;
                MgfFx.Glow(cw, VerdLit, 6 + 4 * Mathf.Min(3, st.combo), 0.4f);
                // 핀은 뽑혀 트레이로, 명판은 좌측 랙으로
                var from = pt.position;
                var plateFrom = curPlate.go.transform.position;
                var rackTo = RackSlot(rack.Count);
                for (float e = 0; e < 0.45f; e += Time.deltaTime)
                {
                    float k = e / 0.45f, s = k * k * (3 - 2 * k);
                    pt.position = Vector3.Lerp(from, SlotPos(pin.slot) + Vector3.up * 0.18f, s) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 1.2f;
                    curPlate.go.transform.position = Vector3.Lerp(plateFrom, rackTo, s) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.6f;
                    curPlate.go.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.26f, s);
                    HideMarks();
                    yield return null;
                }
            }
            else
            {
                // 무엇이 왜 안 맞았는지 그림으로: 꼭짓점(또는 변)까지의 틈 + 서로 다른 세 거리 + 진짜 중심 0.8초
                for (int i = 0; i < 3; i++)
                {
                    V2 target, onCircle;
                    if (spec.incenter)
                    {
                        target = Foot(p, cur.P[i], cur.P[(i + 1) % 3]);
                        var d = target - p; onCircle = d.Len < 1e-6 ? target : p + d * (r / d.Len);   // 핀이 꼭짓점·변 위면 0 나눗셈 회피
                    }
                    else
                    {
                        target = cur.P[i];
                        var d = target - p; onCircle = d.Len < 1e-6 ? target : p + d * (r / d.Len);   // 핀이 꼭짓점·변 위면 0 나눗셈 회피
                    }
                    misses[i].SetPosition(0, W(onCircle, PlateH + 0.05f)); misses[i].SetPosition(1, W(target, PlateH + 0.05f));
                    SetColor(misses[i], new Color(0.95f, 0.45f, 0.35f, 1));
                    misses[i].gameObject.SetActive(true);
                }
                ShowRadii(p, false);
                trueRing.gameObject.SetActive(true);
                CirclePts(trueRing, W(cur.ans, PlateH + 0.05f), (float)cur.tol + 0.05f, 49);
                SetColor(trueRing, VerdLit);
                centerLetter.text = spec.incenter ? "I" : "O";
                centerLetter.transform.position = W(cur.ans, PlateH + 0.06f) + new Vector3(0.38f, 0, 0.32f);
                centerLetter.gameObject.SetActive(true);
                hintTxt.text = WrongWhy(p);
                yield return new WaitForSeconds(0.95f);
                if (roundRetry)
                {
                    // 온보딩: 핀은 굽지 않고 트레이로 돌아오고, 명판은 그대로 남는다
                    yield return new WaitForSeconds(0.6f);
                    var pf = pt.position;
                    for (float e = 0; e < 0.3f; e += Time.deltaTime)
                    {
                        float k = e / 0.3f;
                        pt.position = Vector3.Lerp(pf, SlotPos(pin.slot) + Vector3.up * 0.18f, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 1.0f;
                        yield return null;
                    }
                    var why = hintTxt.text;
                    CleanupRound();
                    hintTxt.text = why + ". 이 명판에 다시 박아라";
                    yield break;
                }
                // 핀 머리가 굽고, 명판은 오른쪽 스크랩으로 튕겨 나간다
                Play(clThunk, 0.5f);
                for (float e = 0; e < 0.2f; e += Time.deltaTime) { pin.body.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(0, 38, e / 0.2f)); yield return null; }
                SetPinMat(pin, bentMat);
                int si = scrapPlates.Count < 2 ? scrapPlates.Count : 1;
                var scrapTo = new Vector3((float)Layout.BedHX - (float)Layout.ScrapW * (si + 0.5f), 0.02f + si * 0.02f, 0.4f - si * 0.8f);
                var plateFrom = curPlate.go.transform.position;
                var pinFrom = pt.position;
                for (float e = 0; e < 0.4f; e += Time.deltaTime)
                {
                    float k = e / 0.4f, s = 1 - (1 - k) * (1 - k);
                    curPlate.go.transform.position = Vector3.Lerp(plateFrom, scrapTo, s) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.9f;
                    curPlate.go.transform.rotation = Quaternion.Euler(0, s * 140f, Mathf.Sin(k * Mathf.PI) * 25f);
                    curPlate.go.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.34f, s);
                    pt.position = Vector3.Lerp(pinFrom, SlotPos(pin.slot) + new Vector3(0.1f, 0.22f, 0.05f), s) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 1.4f;
                    HideMarks();
                    yield return null;
                }
            }
            CleanupRound();
        }

        string WrongWhy(V2 p)
        {
            var s = cur.spec;
            if ((p - cur.other).Len < 1.3 * cur.tol) return s.incenter ? "그 자리는 외심이다. 내심은 세 변까지 거리가 같다" : "그 자리는 내심이다. 외심은 세 꼭짓점까지 거리가 같다";
            if (!s.incenter && s.kind == Kind.Obtuse && Geo.InsideTri(p, cur.P)) return "둔각삼각형의 외심은 삼각형 밖에 있다";
            if (!s.incenter && s.kind == Kind.Right) return "직각삼각형의 외심은 빗변 위에 있다";
            if (s.incenter && !Geo.InsideTri(p, cur.P)) return "내심은 언제나 삼각형 안에 있다";
            return s.incenter ? "세 변까지의 거리가 같지 않다" : "세 꼭짓점까지의 거리가 같지 않다";
        }

        void ShowRadii(V2 c, bool equal)
        {
            var s = cur.spec;
            for (int i = 0; i < 3; i++)
            {
                V2 end = s.incenter ? Foot(c, cur.P[i], cur.P[(i + 1) % 3]) : cur.P[i];
                radii[i].SetPosition(0, W(c, PlateH + 0.05f)); radii[i].SetPosition(1, W(end, PlateH + 0.05f));
                SetColor(radii[i], equal ? VerdLit : new Color(0.9f, 0.55f, 0.45f, 0.9f));
                radii[i].gameObject.SetActive(true);
                ticks[i].gameObject.SetActive(equal);
                if (equal)
                {
                    var m = (c + end) * 0.5; var d = end - c; var n = new V2(-d.y, d.x) * (0.16 / d.Len);
                    ticks[i].SetPosition(0, W(m - n, PlateH + 0.06f)); ticks[i].SetPosition(1, W(m + n, PlateH + 0.06f));
                    SetColor(ticks[i], Cream);
                }
            }
        }

        void HideMarks()
        {
            for (int i = 0; i < 3; i++) { radii[i].gameObject.SetActive(false); ticks[i].gameObject.SetActive(false); misses[i].gameObject.SetActive(false); vLabels[i].gameObject.SetActive(false); }
            for (int i = 0; i < 2; i++) isoTicks[i].gameObject.SetActive(false);
            groove.gameObject.SetActive(false); grooveUnder.gameObject.SetActive(false); arm.gameObject.SetActive(false);
            trueRing.gameObject.SetActive(false); centerLetter.gameObject.SetActive(false); revealText.gameObject.SetActive(false);
            plateEdge.gameObject.SetActive(false);
            rightMark.gameObject.SetActive(false); ghostCircle.gameObject.SetActive(false); fixCircle.gameObject.SetActive(false);
        }

        void HideRoundLines()
        {
            HideMarks();
            shimmer.gameObject.SetActive(false);
            reticle.gameObject.SetActive(false); reticleX.gameObject.SetActive(false); reticleY.gameObject.SetActive(false);
            prePin.root.SetActive(false); prePinIn = false;
        }

        /// <summary>한 판의 뒷정리(연출 도중 QA 훅이 오면 즉시 이 상태로 건너뛴다). 여러 번 불려도 안전.</summary>
        void CleanupRound()
        {
            if (!roundPending) return;
            roundPending = false;
            roundCo = null;
            if (wiggleT != null && wiggleTime < 0.25f) wiggleT.position = wiggleBase;
            wiggleT = null;
            Time.timeScale = 1f; hitStop = 0;
            HideRoundLines();
            var pin = pins[roundPinSlot];
            pin.body.localScale = Vector3.one;
            pin.body.localRotation = pin.bent ? Quaternion.Euler(0, 0, 38) : Quaternion.identity;
            if (pin.bent) SetPinMat(pin, bentMat);
            roundPinSlot = -1;
            if (roundRetry && curPlate != null)
            {
                roundRetry = false;
                curPlate.go.transform.position = curPlate.pivot;
                curPlate.go.transform.rotation = Quaternion.identity;
                curPlate.go.transform.localScale = Vector3.one;
                plateEdge.gameObject.SetActive(true);
                ShowPlateMarks();
                busy = false; idleT = 0;
                PlacePinsInTray();
                MgfBridge.NotifyChanged();
                return;
            }
            roundRetry = false;
            if (curPlate != null)
            {
                if (roundOk)
                {
                    rack.Add(curPlate);
                    SnapRack(curPlate, rack.Count - 1);
                }
                else
                {
                    int si = Mathf.Min(scrapPlates.Count, 1);
                    if (scrapPlates.Count >= 2) { scrapPlates[1].go.SetActive(false); scrapPlates.RemoveAt(1); }
                    scrapPlates.Add(curPlate);
                    curPlate.mr.sharedMaterial = scrapMat;
                    curPlate.go.transform.position = new Vector3((float)Layout.BedHX - (float)Layout.ScrapW * (si + 0.5f), 0.02f + si * 0.02f, 0.4f - si * 0.8f);
                    curPlate.go.transform.localScale = Vector3.one * 0.34f;
                    curPlate.go.transform.rotation = Quaternion.Euler(0, 140f, 0);
                    for (int i = 0; i < 2; i++) scrapZones[i].gameObject.SetActive(i < st.scraps);
                }
            }
            curPlate = null;
            busy = false;
            PlacePinsInTray();
            if (st.phase == "playing")
            {
                if (bannerT == 0 && bannerBg.gameObject.activeSelf) ShowBanner("뱃지를 보고 다음 명판에 박아라", 2.2f);
                SpawnPlate();
            }
            else ShowEnd();
            MgfBridge.NotifyChanged();
        }

        void FinishRound()
        {
            if (!roundPending) return;
            if (roundCo != null) StopCoroutine(roundCo);
            CleanupRound();
        }

        // ─────────────────────────────── UI 갱신(문자열은 값이 바뀔 때만)
        void UpdateUi(float dt)
        {
            if (st.phase == "title") return;
            if (st.phase == "gameover" || st.phase == "clear")
            {
                endCountT += dt;
                float k = Mathf.Clamp01(endCountT / 1.1f);
                int shown = Mathf.RoundToInt(st.plates * (1 - Mathf.Pow(1 - k, 3)));
                if (st.plates > 0 && shown != shownPlates) { shownPlates = shown; endPlates.text = $"각인 {shown}장"; endPlates.transform.localScale = Vector3.one * 1.12f; }
                endPlates.transform.localScale = Vector3.Lerp(endPlates.transform.localScale, Vector3.one, dt * 8f);
                int sc = Mathf.RoundToInt(st.score * (1 - Mathf.Pow(1 - k, 3)));
                if (sc != shownScoreInt) { shownScoreInt = sc; endScore.text = $"점수 {sc}"; }
                return;
            }
            // 점수 카운트업 + 스케일 펀치
            shownScore = Mathf.MoveTowards(shownScore, st.score, Mathf.Max(60f, Mathf.Abs(st.score - shownScore) * 6f) * dt);
            int si = Mathf.RoundToInt(shownScore);
            if (si != shownScoreInt) { shownScoreInt = si; scoreTxt.text = $"점수 {si}"; }
            if (st.plates != shownPlates)
            {
                shownPlates = st.plates; platesTxt.text = $"각인 {st.plates}장";
                platesTxt.transform.localScale = Vector3.one * 1.25f;
                comboTxt.text = st.combo >= 2 ? $"연속 ×{Mathf.Min(3, st.combo)}" : "";
            }
            if (st.combo < 2 && comboTxt.text != "") comboTxt.text = "";
            platesTxt.transform.localScale = Vector3.Lerp(platesTxt.transform.localScale, Vector3.one, dt * 8f);
            int ts = Mathf.CeilToInt(PlayTime - playT);
            if (ts != shownTimeSec) { shownTimeSec = ts; timeTxt.text = ts.ToString(); timeTxt.color = ts <= 10 ? Hex("E07A5F") : Cream; }
            timerFill.localScale = new Vector3(Mathf.Clamp01(1 - playT / PlayTime), 1, 1);
            // 뱃지 swap-text(뒤집기)
            if (badgeFlipT < 0.3f)
            {
                badgeFlipT += dt;
                float k = badgeFlipT / 0.3f;
                if (k >= 0.5f && badgeNext != null) { badgeBig.text = badgeNext; badgeNext = null; }
                badgeRt.localScale = new Vector3(1, Mathf.Abs(Mathf.Cos(k * Mathf.PI)), 1);
                if (badgeFlipT >= 0.3f) badgeRt.localScale = Vector3.one;
            }
        }

        // ─────────────────────────────── IMgfGame (QA 훅) — 실제 입력과 같은 Plant()/PullPrePin() 경로를 탄다
        public void TestStart() => Begin();

        void EnsurePlaying()
        {
            if (st.phase != "playing") Begin();
            FinishRound();
            if (st.phase != "playing") Begin();
            drag = DragKind.None;
            ClearAim();
        }

        public void TestAnswerCorrect()
        {
            EnsurePlaying();
            if (prePinIn) PullPrePin();
            Plant(cur.ans, FirstFreePin());
        }

        public void TestAnswerWrong()
        {
            EnsurePlaying();
            if (prePinIn) PullPrePin();
            frozen = false;   // 훅은 온보딩 무감점 재시도를 건너뛰고 실제 오답 경로를 탄다
            Plant(cur.G, FirstFreePin());   // 무게중심(M4) — 생성기가 정답에서 2.5 tol 이상 떨어뜨려 둔 자리
        }

        public string StateJson() => JsonUtility.ToJson(st);

        public string ProblemBankJson() => MgfJson.Bank(bank);
    }
}
