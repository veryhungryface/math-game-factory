// 원 찍어 v3 — 모눈 명판 위에서 외심·내심을 「정의」와 「성질」로 찾는다. (DESIGN-v3.md)
//
// 1단계(거리로 찾기): 핀을 끌어 모눈 격자점 위로 옮긴다(속도 상한 = 초당 6걸음). 핀이 설 때마다
//   · 외심 명판: 핀 P 에서 세 꼭짓점까지 세 선과 길이(모눈 칸, 소수 둘째 자리)
//   · 내심 명판: 핀 P 에서 세 변에 내린 수선(수선의 발·직각 표시)과 길이
//   가 따라온다. 두 길이가 같으면 그 두 선이 초록, 세 길이가 정확히 같아지는 격자점에서 세 선이 빨개진다.
//   「핀 박기」로 확정 → 정답이면 외접원/내접원이 스윕하며 각인, 오답이면 세 길이의 차이를 보여 준 뒤 핀 하나를 잃는다.
// 다리 단계(WonBridge.cs): 외심·내심 각각 연속 첫 시도 정답 2회면 열린다. 거리가 같은 점 3개를 찍으면 발자국이 한 줄로 서고
//   「변 AB의 수직이등분선」/「∠A의 이등분선」이 드러난다(교과서 성질 문장 + 2단계 예고). 목숨·제한 시간 없음.
// 2단계(작도로 찾기, WonBuild.cs): 거리선 없음. 변 탭 = 수직이등분선, 꼭짓점 탭 = 각의 이등분선. 두 선의 교점에 핀이 서면 「핀 박기」.
//   필름 접기는 선이 생길 때의 0.7초 연출로만 남았다(WonFilm.cs).
// 판정은 WonGeo.cs 의 정수 연산(핀 좌표 = 정답 격자점인가)뿐이다. 코드는 정답 자리로 핀을 옮기거나 선을 대신 고르지 않는다.
using System.Collections;
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mgf.WonJjigeo
{
    public partial class WonJjigeoGame : MonoBehaviour, IMgfGame
    {
        [System.Serializable]
        class State : MgfState
        {
            public string stage = "1";               // "1"(거리) · "bridge"(다리 단계) · "2"(작도)
            public int plates, attempts, firstTry, combo, maxCombo, px, py, dots;   // dots = 다리 단계 발자국 수
            public string mission = "", tri = "";   // tri = 명판 꼭짓점 격자 좌표(명판이 바뀔 때만 바뀐다)
            public List<string> lines = new List<string>();   // 2단계: 그은 선 이름(「AB의 수직이등분선」「∠B의 이등분선」)
            public string taps = "";                 // 2단계: 탭 대상 화면 위치(꼭짓점 A,B,C; 변 AB,BC,CA — 정규화, 명판마다 1회)
            public bool red, onboarding;
        }

        readonly State st = new State();
        enum Step { Dist, Bridge, Build }
        Step step = Step.Dist;
        static readonly int[] MAll = { 0, 1, 2 }, MAB = { 0, 1 }, MAC = { 0, 2 };
        int[] mIdx = MAll;       // 측정 선 번호: 1단계·오답 연출 = 셋, 외심 다리 = PA·PB, 내심 다리 = 변 AB·변 AC
        Gen gen;
        System.Random rng = new System.Random(20260926);
        readonly List<MgfProblem> bank = new List<MgfProblem>();

        Prob cur;
        Plate curPlate;
        int idx, streakO, streakI, tutRetries;
        bool attemptCounted;
        float plateT, plateLimit, plateEnterT = 9f, bannerT, idleT;
        bool busy, roundPending, roundOk, roundRetry;
        Coroutine roundCo;
        float hitStop;
        Vector3 shakeOffset;
        int onb;               // 첫 명판 온보딩 단계 0: 끌기 전 · 1: 옮겨 봄 · 2: 빨간색 봄 · 3: 끝
        int lastAlarmSec = -1;

        // 핀(트레이의 핀 하나가 모눈 위로 올라와 옮겨 다닌다)
        Pin curPin, demoPin;
        RP pinPos, pinTarget, pinNext;
        bool stepping, pinReady;
        float stepT, pinRiseT = 9f, hopRate = Rules.StepRate;
        Vector3 stepFrom, stepTo, riseFrom;
        Transform pinGlow;

        // 측정 선
        readonly LineRenderer[] gl = new LineRenderer[3], ext = new LineRenderer[3], ra = new LineRenderer[3], tk = new LineRenderer[3];
        readonly TextMeshPro[] lbl = new TextMeshPro[3];
        readonly TextMeshPro[] vLabels = new TextMeshPro[3];
        readonly LineRenderer[] lblPill = new LineRenderer[3];   // 길이 글자 뒤 어두운 알약(모래 위에서도 읽히게)
        readonly float[] lblHalf = new float[3], lblH = new float[3];
        readonly long[] shownH = { -1, -1, -1 };
        bool measureOn, feedbackMode;
        readonly bool[] pairEq = new bool[3];
        float redT;
        LineRenderer groove, grooveUnder, arm, trueRing, rightMark, pulseRing, onbPath, targetRing;
        readonly LineRenderer[] ripples = new LineRenderer[3];
        readonly float[] rippleT = { 9, 9, 9 };
        int rippleNext;
        TextMeshPro revealText, centerLetter;
        Transform shimmer;

        // 입력
        enum Press { None, Pin, PlantBtn }
        Press press;
        Vector2 downScreen;
        bool ctaDown, endCtaDown;
        float endShownAt, plantPressT = 9f;

        // UI 상태
        float shownScore, endCountT;
        int shownScoreInt = -1, best, bestScore;
        string lastGaugeKey = "";

        // 타이틀 데모
        Prob demoProb;
        Plate demoPlate;
        static readonly IP[] demoPath = { new IP(0, -7), new IP(0, -6), new IP(0, -5), new IP(1, -4), new IP(1, -3), new IP(1, -2), new IP(2, -1), new IP(2, 0), new IP(2, 1) };

        // ─────────────────────────────── 부팅
        void Awake()
        {
            MgfLook.Quality(30f);
            if (!MgfBridge.LowGfx) QualitySettings.shadowResolution = ShadowResolution.VeryHigh;   // 명판·핀 그림자 가장자리 계단 완화
            if (keepTypes.Length == 0) Debug.Log("keep");
            gen = new Gen();
            BuildBank();
            BuildWorld();
            BuildUi();
            BuildSounds();
            MgfText.Prewarm("다리작도발자국점찍기같은직선그러면만나는그은남은통과이제없이로찾는다✓개더사이흐리게다음→①②③곳골라어디에모일까보자원찍어중심에핀을박아라각인시작외심내심의위치를찾으시오최고장점연속초시간종료명판이모두굽었다다시세꼭짓점까지거리가같지않다변그자리는둔각삼각형밖에있다직각빗변위뽑고잘못박힌먼저끌어놓아라원이끝날때까지다음투입등장부등변이등분선수직삼각형의성질중학교학년단계해금필름접기주름포개어교점트레이싱거리선없다옮겨라길이따라바뀐빨개진다눌러첫시도정답률완주판수선발모눈칸바깥안쪽이미이웃한다른되돌아간다만나는한점보너스·×=ABCOIP0123456789°∠△−→:,.!?()/+");
            best = PlayerPrefs.GetInt("wonjjigeo.v3.best", 0);
            bestScore = PlayerPrefs.GetInt("wonjjigeo.v3.bestScore", 0);
            ShowTitle();
            MgfBridge.Register(this);
        }

        // ── 문제 은행: 게임 판과 같은 Gen.Next · Gen.MakeBridge · Words 로 만든다(별도 시험지 아님).
        //    1단계(거리)·다리 단계(거리가 같은 점 → 이등분선)·2단계(작도: 화면 발문 + 교과서형 「변 AB와 변 BC의 수직이등분선을 그어 …」).
        void BuildBank()
        {
            var r = new System.Random(777);
            var seen = new HashSet<string>();
            AddBank(Gen.First(), -1, seen);
            for (int k = 0; k < 6000 && bank.Count < 460; k++)
            {
                int kind = k % 5;
                if (kind == 0 || kind == 1) AddBank(gen.Next(1, 1 + k % 6, r), -1, seen);
                else if (kind == 2) AddBank(gen.MakeBridge(k % 2 == 0 ? Mission.Circum : Mission.In, r), -1, seen);
                else { var p = gen.Next(2, k % 6, r); AddBank(p, kind == 3 ? -1 : r.Next(3), seen); }
            }
        }

        void AddBank(Prob p, int variant, HashSet<string> seen)
        {
            string prompt = Words.Prompt(p, variant);
            if (!seen.Add(prompt)) return;
            bank.Add(new MgfProblem
            {
                id = "w" + (bank.Count + 1),
                prompt = prompt,
                choices = null,
                answer = Words.Answer(p, variant),
                answerNumeric = Words.Numeric(p),
                unitConcept = Words.Concept(p)
            });
        }

        void BuildLines()
        {
            for (int i = 0; i < 3; i++)
            {
                gl[i] = Line("Gauge" + i, 0.055f, 2, false);
                ext[i] = Line("Ext" + i, 0.04f, 2, false, dashMat);
                ra[i] = Line("RightAngle" + i, 0.035f, 3, false);
                tk[i] = Line("Tick" + i, 0.045f, 2, false);
                lbl[i] = FlatText("", 3.4f, Cream);
                vLabels[i] = FlatText(Geo.Names[i], 4.4f, Ink);
                vLabels[i].outlineColor = new Color32(246, 238, 222, 255); vLabels[i].outlineWidth = 0.28f;
                lblPill[i] = Line("LenPill" + i, 0.3f, 2, false);
                lblPill[i].numCapVertices = 5;
                SetColor(lblPill[i], new Color(0.1f, 0.075f, 0.06f, 0.72f));
                ripples[i] = Line("Ripple" + i, 0.05f, 33, true);
            }
            groove = Line("Groove", 0.09f, 97, true);
            grooveUnder = Line("GrooveUnder", 0.17f, 97, true);
            arm = Line("Arm", 0.06f, 2, false);
            trueRing = Line("TrueRing", 0.06f, 33, true);
            rightMark = Line("RightMark", 0.04f, 3, false);
            pulseRing = Line("Pulse", 0.07f, 49, true);
            onbPath = Line("OnbPath", 0.11f, 24, false, dashMat);
            targetRing = Line("Target", 0.04f, 25, true);
            revealText = FlatText("", 4.2f, Cream);
            centerLetter = FlatText("O", 4f, Match);
            var sh = MgfLook.Prim(PrimitiveType.Quad, "Shimmer", Vector3.zero, Vector3.one * 0.9f, glowMat, world, false);
            sh.transform.rotation = Quaternion.Euler(90, 0, 0);
            sh.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            shimmer = sh.transform; sh.SetActive(false);
            var pg = MgfLook.Prim(PrimitiveType.Quad, "PinGlow", Vector3.zero, Vector3.one, MgfLook.Additive(new Color(1f, 0.35f, 0.25f, 1f), MgfLook.SoftDot), world, false);
            pg.transform.rotation = Quaternion.Euler(90, 0, 0);
            pg.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            pinGlow = pg.transform; pg.SetActive(false);
            BuildConsViz();
            BuildBridgeFx();
        }

        // ─────────────────────────────── 화면 전환
        void ShowTitle()
        {
            st.phase = "title";
            titleRoot.gameObject.SetActive(true);
            hudRoot.gameObject.SetActive(false);
            endRoot.gameObject.SetActive(false);
            bestTxt.text = best > 0 ? $"최고 {best}장 · {bestScore}점" : "1단계 거리로 · 2단계 작도로";
            mIdx = MAll;
            foreach (var p in pins) p.root.SetActive(false);
            demoProb = Gen.First();
            if (demoPlate == null) demoPlate = TakePlate();
            ShapePlate(demoPlate, demoProb.V);
            demoPlate.go.SetActive(true);
            demoPlate.go.transform.position = demoPlate.pivot; demoPlate.go.transform.localScale = Vector3.one; demoPlate.go.transform.rotation = Quaternion.identity;
            demoPin.root.SetActive(true);
            cur = demoProb;
            camBlend = 0f;
            MgfBridge.NotifyChanged();
        }

        void Begin()
        {
            FinishRound();
            StopAllCoroutines();
            roundCo = null; roundPending = false; foldCo = null; clampCo = null; ghostCo = null; bridgeCo = null;
            step = Step.Dist;
            st.score = 0; st.lives = Rules.Lives; st.level = 1; st.stage = "1"; st.solved = 0; st.combo = 0; st.maxCombo = 0;
            st.plates = 0; st.attempts = 0; st.firstTry = 0; st.red = false; st.dots = 0; st.lines.Clear(); st.taps = "";
            ghostPlays = 0; bridgeIdx = 0; bridgeDone = false; att2 = 0; ok2 = 0;
            st.phase = "playing";
            Time.timeScale = 1f; hitStop = 0;
            shownScore = 0; shownScoreInt = -1; lastGaugeKey = "";
            idx = 0; streakO = 0; streakI = 0; busy = false; onb = 0;
            rng = new System.Random(System.Environment.TickCount);
            titleRoot.gameObject.SetActive(false);
            endRoot.gameObject.SetActive(false);
            hudRoot.gameObject.SetActive(true);
            if (demoPlate != null) { demoPlate.go.SetActive(false); demoPlate = null; }
            demoPin.root.SetActive(false);
            foreach (var p in rack) p.go.SetActive(false);
            rack.Clear();
            if (curPlate != null) curPlate.go.SetActive(false);
            curPlate = null;
            ResetClamps();
            cur = null;
            ClearBuild(); HideBridge(); StopFoldFx(); HideCard();
            HideRoundFx();
            for (int i = 0; i < 3; i++) { pins[i].bent = false; SetPinMat(pins[i], brassMat); pins[i].root.SetActive(true); pins[i].body.localRotation = Quaternion.identity; }
            curPin = null;
            PlacePinsInTray();
            SpawnPlate(gen.Next(1, 0, rng));
            MgfBridge.NotifyChanged();
        }

        void EndGame()
        {
            bool done = st.lives > 0 && step == Step.Build && idx >= Rules.Stage2Plates;
            st.phase = done ? "clear" : "gameover";
            MgfBridge.NotifyChanged();
        }

        float Accuracy => st.attempts > 0 ? (float)st.firstTry / st.attempts : 0f;
        int att2, ok2;   // 2단계만의 첫 시도(승리 연출 조건)
        float Accuracy2 => att2 > 0 ? (float)ok2 / att2 : 0f;

        void ShowEnd()
        {
            hudRoot.gameObject.SetActive(true);
            endRoot.gameObject.SetActive(true);
            bannerBg.gameObject.SetActive(false);
            hintTxt.text = "";
            HideCard(); StopGhost(); ClearBuild(); HideBridge();
            HideMeasure();
            bool victory = st.phase == "clear" && Accuracy >= Rules.ClearAccuracy && Accuracy2 >= Rules.ClearAccuracy;
            bool newBest = st.plates > best || (st.plates == best && st.score > bestScore && st.plates > 0);
            if (newBest) { best = st.plates; bestScore = st.score; PlayerPrefs.SetInt("wonjjigeo.v3.best", best); PlayerPrefs.SetInt("wonjjigeo.v3.bestScore", bestScore); PlayerPrefs.Save(); }
            endTitle.text = st.phase == "gameover" ? "핀이 모두 굽었다" : victory ? "완주 · 두 단계 모두 각인"
                : Accuracy2 < Rules.ClearAccuracy ? "완주 · 작도 선 고르기가 흔들렸다" : "완주 · 첫 시도 정답률이 낮다";
            endCountT = 0; endShownAt = Time.time; shownScoreInt = -1;
            endPlates.text = $"각인 {st.plates}장";
            endScore.text = "";
            endCombo.text = st.attempts == 0 ? "외심은 세 꼭짓점, 내심은 세 변까지 거리가 같다"
                : $"첫 시도 정답 {st.firstTry}/{st.attempts} · 최대 연속 {st.maxCombo}" + (step == Step.Dist ? "\n2단계(작도)는 외심·내심을 2번씩 연속으로 맞히면 열린다" : "");
            endBest.text = newBest && st.plates > 0 ? "최고 기록 갱신" : best > 0 ? $"최고 {best}장" : "";
            Play(victory ? clUnlock : st.phase == "gameover" ? clThunk : clChime, 0.7f);
            if (victory) MgfFx.Glow(new Vector3(0, 1f, 0), Match, 24, 0.8f);
        }

        // ─────────────────────────────── 명판 투입
        void SpawnPlate(Prob p)
        {
            cur = p;
            st.mission = p.bridge ? (p.m == Mission.Circum ? "외심 다리" : "내심 다리") : p.Center;
            st.level = step == Step.Dist ? 1 : step == Step.Bridge ? 2 : 3;
            st.tri = $"{p.V[0].x},{p.V[0].y},{p.V[1].x},{p.V[1].y},{p.V[2].x},{p.V[2].y}";
            curPlate = TakePlate();
            ShapePlate(curPlate, p.V);
            curPlate.go.SetActive(true);
            curPlate.mr.sharedMaterial = bronzeMat;
            curPlate.go.transform.rotation = Quaternion.identity;
            curPlate.go.transform.localScale = Vector3.one;
            curPlate.go.transform.position = curPlate.pivot + Vector3.down * 0.3f;
            plateEnterT = 0f;
            ClearBuild();
            HideBridge();
            StopFoldFx();
            ShowPlateMarks();
            // 핀: 트레이의 첫 성한 핀이 출발점으로 올라온다
            curPin = FirstFreePin();
            pinPos = pinTarget = RP.Of(Rules.Start);
            stepping = false; pinReady = false; pinRiseT = 0f;
            riseFrom = SlotPos(curPin.slot) + Vector3.up * 0.18f;
            PlacePinsInTray();
            plateT = 0; lastAlarmSec = -1;
            // 0 = 무제한: 다리 단계(학습)·안내 명판(1단계 첫 외심·첫 내심, 2단계 첫 외심·첫 내심)
            plateLimit = step == Step.Bridge || p.tutorial ? 0f : step == Step.Dist ? Rules.T1 : Rules.T2;
            tutRetries = 0; attemptCounted = false; idleT = 0; tapsSet = false; st.taps = "";
            for (int i = 0; i < 3; i++) shownH[i] = -1;
            st.red = false; st.px = pinPos.Lattice.x; st.py = pinPos.Lattice.y;
            badgeBig.outlineColor = p.m == Mission.Circum ? new Color32(31, 107, 90, 255) : new Color32(120, 60, 20, 255);
            if (step == Step.Bridge)
            {
                badgeBig.text = "같은 거리";
                badgeSub.text = p.m == Mission.Circum ? "A, B에서 거리가 같은 점 3개" : "변 AB, AC에서 거리가 같은 점 3개";
            }
            else
            {
                badgeBig.text = p.Center;
                badgeSub.text = step == Step.Dist ? $"△ABC의 {p.Center} {p.Letter}를 찾으시오" : $"선 두 개를 그어 {p.Center} {p.Letter}를 찾으시오";
            }
            MgfFx.Punch(badgeRt, 0.12f, 0.3f);
            mIdx = step == Step.Bridge ? (p.m == Mission.Circum ? MAB : MAC) : MAll;
            measureOn = step != Step.Build;
            feedbackMode = false;
            lastGaugeKey = "";
            plantTxt.text = step == Step.Bridge ? "점 찍기" : "핀 박기";
            if (step == Step.Bridge) SetupBridgePlate();
            if (step == Step.Build) { LayoutHandles(); ShowTrace(true); }
            if (measureOn) UpdateMeasure(true); else { HideMeasure(); RefreshBuildGauge(); }
            // 안내
            if (step == Step.Dist && idx == 0) { onb = 0; ShowBanner("핀을 끌어 옮겨라 · 세 꼭짓점까지의 길이가 따라 바뀐다", 0); }
            else if (step == Step.Dist && p.tutorial) ShowBanner("내심: 핀에서 세 변까지의 거리가 모두 같아지는 점", 0);
            else if (step == Step.Bridge && p.m == Mission.Circum) ShowBanner("A, B에서 거리가 같은 점을 3개 찾아 찍어라\nPA=PB가 되면 선이 초록으로 바뀐다", 0);
            else if (step == Step.Bridge) ShowBanner("두 변 AB, AC에서 거리가 같은 점을 3개 찾아 찍어라\n두 거리가 같아지면 선이 초록으로 바뀐다", 0);
            else if (step == Step.Build && p.tutorial && p.m == Mission.Circum) ShowBanner("외심 = 세 변의 수직이등분선의 교점\n변을 누르면 그 변의 수직이등분선이 그어진다", 0);
            else if (step == Step.Build && p.tutorial) ShowBanner("내심 = 세 내각의 이등분선의 교점\n꼭짓점을 누르면 그 각의 이등분선이 그어진다", 0);
            else if (bannerT == 0 && bannerBg.gameObject.activeSelf) bannerBg.gameObject.SetActive(false);
            if (step == Step.Build && p.tutorial) StartGhost();
            st.onboarding = step == Step.Dist && idx == 0;
            hintTxt.text = "";
            MgfBridge.NotifyChanged();
        }

        /// <summary>명판 위 표시: 꼭짓점 이름(삼각형 바깥쪽)·직각 표시.</summary>
        void ShowPlateMarks()
        {
            for (int i = 0; i < 3; i++)
            {
                // 이름표: 그 꼭짓점의 각을 반으로 나눈 방향의 반대쪽(삼각형 바깥) — 둔각에서도 변과 겹치지 않는다. 판 안으로 당긴다.
                var u = cur.V[(i + 1) % 3] - cur.V[i]; var w = cur.V[(i + 2) % 3] - cur.V[i];
                var d = -(new Vector2(u.x, u.y).normalized + new Vector2(w.x, w.y).normalized).normalized;
                float off = cur.bridge && cur.m == Mission.Circum && i < 2 ? 1.3f : 0.95f;   // 외심 다리의 A·B 고리와 겹치지 않게
                var pos = GW(cur.V[i].x + d.x * off, cur.V[i].y + d.y * off, LineY + 0.01f);
                pos.x = Mathf.Clamp(pos.x, -FX + 0.3f, FX - 0.3f); pos.z = Mathf.Clamp(pos.z, -FZ + 0.3f, FZ - 0.3f);
                vLabels[i].transform.position = pos;
                // 외심 다리에서는 C 를 흐리게(A·B 만 쓴다)
                bool dim = cur.bridge && cur.m == Mission.Circum && i == 2;
                vLabels[i].color = dim ? new Color(Ink.r, Ink.g, Ink.b, 0.3f) : Ink;
                vLabels[i].gameObject.SetActive(true);
            }
            rightMark.gameObject.SetActive(false);
            if (cur.rightV >= 0)
            {
                var v0 = cur.V[cur.rightV]; var e1 = cur.V[(cur.rightV + 1) % 3] - v0; var e2 = cur.V[(cur.rightV + 2) % 3] - v0;
                var a = new Vector2(e1.x, e1.y).normalized * 0.7f; var b = new Vector2(e2.x, e2.y).normalized * 0.7f;
                rightMark.SetPosition(0, GW(v0.x + a.x, v0.y + a.y, LineY)); rightMark.SetPosition(1, GW(v0.x + a.x + b.x, v0.y + a.y + b.y, LineY)); rightMark.SetPosition(2, GW(v0.x + b.x, v0.y + b.y, LineY));
                SetColor(rightMark, Cream);
                rightMark.gameObject.SetActive(true);
            }
        }

        void ShowBanner(string s, float secs)
        {
            bannerTxt.text = s;
            bannerBg.gameObject.SetActive(true);
            bannerT = secs;   // 0 = 다음 교체까지 유지
            MgfFx.Punch(bannerBg.rectTransform, 0.06f, 0.2f);
        }

        // ─────────────────────────────── 트레이·핀
        Vector3 SlotPos(int i) => tray.position + new Vector3(-1.4f + 1.4f * i, 0, 0);

        void PlacePinsInTray()
        {
            for (int i = 0; i < 3; i++)
            {
                var p = pins[i];
                if (p == curPin && st.phase == "playing") continue;
                p.root.SetActive(true);
                if (p.bent) { p.root.transform.position = SlotPos(i) + new Vector3(0.1f, 0.22f, 0.05f); p.root.transform.rotation = Quaternion.Euler(0, 0, 82); }
                else { p.root.transform.position = SlotPos(i) + Vector3.up * 0.18f; p.root.transform.rotation = Quaternion.identity; }
            }
        }

        Pin FirstFreePin() { foreach (var p in pins) if (!p.bent) return p; return pins[0]; }

        void SetTarget(RP t)
        {
            if (!pinTarget.SameAs(t))
            {
                pinTarget = t;
                ShowTargetRing();
            }
            idleT = 0;
        }

        void ShowTargetRing()
        {
            targetRing.gameObject.SetActive(!pinTarget.SameAs(pinPos));
            CirclePts(targetRing, PW(pinTarget, LineY + 0.01f), 0.22f, 25);
            SetColor(targetRing, new Color(1f, 0.95f, 0.8f, 0.8f));
        }

        /// <summary>다음 한 걸음: 격자점이면 목표 격자점 쪽으로 가로·세로·대각 한 칸. 목표가 주름 교점(비격자)이면 가장 가까운 격자점까지 간 뒤 한 번에 올라선다.</summary>
        RP NextStep()
        {
            var tl = pinTarget.IsLattice ? pinTarget.Lattice : new IP(Mathf.RoundToInt((float)pinTarget.fx), Mathf.RoundToInt((float)pinTarget.fy));
            if (!pinPos.IsLattice) return RP.Of(new IP(Mathf.RoundToInt((float)pinPos.fx), Mathf.RoundToInt((float)pinPos.fy)));
            var p = pinPos.Lattice;
            if (p.Same(tl)) return pinTarget;
            return RP.Of(new IP(p.x + System.Math.Sign(tl.x - p.x), p.y + System.Math.Sign(tl.y - p.y)));
        }

        void UpdatePin(float dt)
        {
            if (curPin == null) return;
            var t = curPin.root.transform;
            if (pinRiseT < 1f)
            {
                pinRiseT = Mathf.Min(1f, pinRiseT + dt / 0.38f);
                float k = pinRiseT, s = k * k * (3 - 2 * k);
                t.position = Vector3.Lerp(riseFrom, PW(pinPos, FilmY), s) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 1.2f;
                t.rotation = Quaternion.Euler(0, 0, Mathf.Sin(k * Mathf.PI) * 20f);
                if (pinRiseT >= 1f) { pinReady = true; Play(clSeat, 0.35f); }
                return;
            }
            if (!pinReady || busy || roundPending) return;
            if (!stepping && !pinPos.SameAs(pinTarget))
            {
                // 2단계: 핀은 학생이 그은 두 선의 교점으로 한 번에 폴짝 옮겨 선다(격자 훑기가 없으니 걸음 상한이 필요 없다)
                pinNext = step == Step.Build ? pinTarget : NextStep();
                stepFrom = PW(pinPos, FilmY); stepTo = PW(pinNext, FilmY);
                stepT = 0; stepping = true;
                hopRate = step == Step.Build ? 1f / (0.26f + 0.025f * Vector3.Distance(stepFrom, stepTo) / CELL) : Rules.StepRate;
            }
            if (stepping)
            {
                stepT += dt * hopRate;
                if (stepT >= 1f) { stepT = 1f; stepping = false; pinPos = pinNext; OnArrive(); }
            }
            float e = stepping ? stepT * stepT * (3 - 2 * stepT) : 1f;
            float hopH = step == Step.Build ? 0.35f + 0.04f * Vector3.Distance(stepFrom, stepTo) : 0.12f;
            var pos = stepping ? Vector3.Lerp(stepFrom, stepTo, e) + Vector3.up * Mathf.Sin(stepT * Mathf.PI) * hopH : PW(pinPos, FilmY);
            t.position = pos;
            float lean = stepping ? 9f : 0f;
            var dir = stepTo - stepFrom;
            t.rotation = Quaternion.Euler(dir.z * lean * 2f, 0, -dir.x * lean * 2f);
        }

        void OnArrive()
        {
            Play(clTick, 0.22f);
            var lp = pinPos.IsLattice ? pinPos.Lattice : new IP(Mathf.RoundToInt((float)pinPos.fx), Mathf.RoundToInt((float)pinPos.fy));
            st.px = lp.x; st.py = lp.y;
            if (pinPos.SameAs(pinTarget)) targetRing.gameObject.SetActive(false);
            if (step == Step.Build && pinPos.SameAs(pinTarget)) { Play(clSeat, 0.45f); RefreshBuildGauge(); }
            if (measureOn) UpdateMeasure(false);
            // 안내 명판: 세 길이가 같아지는 격자점에 닿으면 그 칸에서 멈춘다(정답을 스쳐 지나가 「같다」 배너만 남는 일 방지)
            if (st.red && cur != null && cur.tutorial && step == Step.Dist && !pinPos.SameAs(pinTarget))
            {
                pinTarget = pinPos; targetRing.gameObject.SetActive(false);
                if (press == Press.Pin) press = Press.None;   // 끌던 손가락이 계속 끌어도 이 명판에선 멈춘 자리를 지킨다
            }
            if (onb == 0 && step == Step.Dist && idx == 0 && !pinPos.Is(Rules.Start)) { onb = 1; ShowBanner("세 길이가 모두 같아지는 점을 찾아라 · 같아지면 빨개진다", 0); }
            MgfBridge.NotifyChanged();
        }

        Vector3 PinTipWorld() => curPin != null ? curPin.root.transform.position : Vector3.zero;

        // ─────────────────────────────── 측정 선(1단계·다리 단계·오답 연출)
        bool UsedIdx(int i) { foreach (var k in mIdx) if (k == i) return true; return false; }

        /// <summary>핀이 격자점에 설 때: 길이(표시용 정수 반올림)와 정확한 같음(정수 판정)을 갱신한다. 문자열은 값이 바뀔 때만 만든다.</summary>
        void UpdateMeasure(bool force)
        {
            if (cur == null) return;
            var mode = cur.m;
            for (int k = 0; k < mIdx.Length; k++)
            {
                int i = mIdx[k];
                long h = cur.DistH(pinPos, i, mode);
                if (h != shownH[i] || force)
                {
                    shownH[i] = h; lbl[i].text = Geo.Fmt(h); gVal[k].text = Geo.Fmt(h);
                    var sz = lbl[i].GetPreferredValues(lbl[i].text); lblH[i] = sz.y; lblHalf[i] = Mathf.Max(0.01f, sz.x * 0.5f - sz.y * 0.2f);
                }
            }
            if (step == Step.Bridge && !feedbackMode && st.phase != "title")
            {
                UpdateBridgeEq();
                st.red = false;
                PaintMeasure();
                for (int i = 0; i < 3; i++) lbl[i].gameObject.SetActive(measureOn && UsedIdx(i));
                return;
            }
            for (int i = 0; i < 3; i++) pairEq[i] = cur.EqualPair(pinPos, i, (i + 1) % 3, mode);
            bool red = pairEq[0] && pairEq[1];
            if (red && !st.red && !feedbackMode && st.phase != "title")
            {
                // 세 거리가 딱 같아진 순간: 세 선이 빨개지고, 딩, 핀 발밑이 빛난다
                redT = 0; Play(clMatch, 0.75f); camPush = 1f;
                MgfFx.Glow(PinTipWorld() + Vector3.up * 0.1f, Match, 8, 0.35f);
                MgfFx.Punch(gaugeRt, 0.08f, 0.25f);
                if (onb <= 1 && step == Step.Dist && idx == 0) { onb = 2; ShowBanner("세 꼭짓점까지 거리가 같다! 「핀 박기」를 눌러라", 0); }
            }
            // 빨간 상태에서 벗어났으면 첫 명판 안내도 「찾아라」로 되돌린다 — 「같다」 배너가 거짓으로 남지 않게
            if (!red && st.red && onb == 2 && step == Step.Dist && idx == 0 && !feedbackMode && st.phase != "title")
            { onb = 1; ShowBanner("세 길이가 모두 같아지는 점을 찾아라 · 같아지면 빨개진다", 0); }
            st.red = red;
            PaintMeasure();
            for (int i = 0; i < 3; i++) { lbl[i].gameObject.SetActive(measureOn); }
        }

        Color LineColor(int i)
        {
            if (step == Step.Bridge && !feedbackMode)
            {
                if (wobT < 0.6f) return Amber;
                return bridgeGreen ? Pair : bridgeOutside ? Amber : new Color(Cream.r, Cream.g, Cream.b, 0.92f);
            }
            if (st.red) return Match;
            bool pe = pairEq[i] || pairEq[(i + 2) % 3];   // 선 i 가 들어간 두 쌍: (i,i+1), (i-1,i)
            if (feedbackMode) return pe ? Pair : Amber;
            return pe ? Pair : new Color(Cream.r, Cream.g, Cream.b, 0.92f);
        }

        void PaintMeasure()
        {
            GaugeList(false);
            string key = cur.m + "" + step + (cur.bridge ? "b" : "");
            if (key != lastGaugeKey)
            {
                lastGaugeKey = key;
                if (step == Step.Bridge)
                {
                    gName[0].text = cur.m == Mission.Circum ? "PA" : "변 AB까지"; gName[1].text = cur.m == Mission.Circum ? "PB" : "변 AC까지"; gName[2].text = "발자국";
                }
                else
                {
                    string[] names = cur.m == Mission.Circum ? new[] { "PA", "PB", "PC" } : new[] { "변 AB까지", "변 BC까지", "변 CA까지" };
                    for (int i = 0; i < 3; i++) gName[i].text = names[i];
                }
                gaugeHead.text = MeasureHead();
            }
            for (int k = 0; k < mIdx.Length; k++)
            {
                int i = mIdx[k];
                var c = LineColor(i);
                SetColor(gl[i], c); SetColor(ra[i], c); SetColor(ext[i], new Color(c.r, c.g, c.b, 0.6f));
                lbl[i].color = st.red ? new Color(1f, 0.85f, 0.8f) : c;
                gVal[k].color = st.red ? new Color(1f, 0.5f, 0.42f) : c;
                gBar[k].color = c;
            }
            if (step == Step.Bridge && !feedbackMode)
            {
                gVal[2].text = $"{dots.Count}/{Rules.BridgeDots}";
                gVal[2].color = dots.Count > 0 ? Pair : new Color(1, 1, 1, 0.5f);
                gBar[2].color = dots.Count > 0 ? Pair : new Color(1, 1, 1, 0.15f);
                gaugeHead.text = bridgeGreen ? (cur.m == Mission.Circum ? "PA = PB · 「점 찍기」를 눌러라" : "두 변까지 거리가 같다 · 「점 찍기」를 눌러라")
                    : bridgeOutside ? "∠A의 바깥이다 · 두 변 사이에서 찾아라" : MeasureHead();
                return;
            }
            if (st.red) gaugeHead.text = cur.m == Mission.Circum ? "PA = PB = PC  세 꼭짓점까지 거리가 같다" : "세 변까지 거리가 같다";
            else if (!feedbackMode) gaugeHead.text = MeasureHead();
        }

        string MeasureHead()
        {
            if (step == Step.Bridge) return cur.m == Mission.Circum ? "점 P에서 두 꼭짓점 A, B까지의 거리(모눈 칸)" : "점 P에서 두 변 AB, AC까지의 거리(모눈 칸)";
            return cur.m == Mission.Circum ? "점 P에서 세 꼭짓점까지의 거리(모눈 칸)" : "점 P에서 세 변까지의 거리(모눈 칸)";
        }

        void HideMeasure()
        {
            for (int i = 0; i < 3; i++) { gl[i].gameObject.SetActive(false); ext[i].gameObject.SetActive(false); ra[i].gameObject.SetActive(false); tk[i].gameObject.SetActive(false); lbl[i].gameObject.SetActive(false); lblPill[i].gameObject.SetActive(false); }
            pinGlow.gameObject.SetActive(false);
            measureOn = false;
        }

        /// <summary>매 프레임: 선 끝점만 옮긴다(핀이 걸음 사이를 미끄러질 때도 선이 따라온다).</summary>
        void DrawMeasure(Vector3 tip, double gx, double gy)
        {
            if (!measureOn || cur == null) return;
            var P = new Vector3(tip.x, LineY, tip.z);
            float pulse = st.red ? 1f + 0.35f * Mathf.Sin(redT * 14f) * Mathf.Exp(-redT * 2.5f) : 1f;
            bool bridgeMode = step == Step.Bridge && !feedbackMode;
            float wob = bridgeMode && wobT < 0.6f ? Mathf.Sin(wobT * 38f) * 0.12f * (1f - wobT / 0.6f) : 0f;
            for (int i = 0; i < 3; i++)
            {
                if (!UsedIdx(i))
                {
                    gl[i].gameObject.SetActive(false); ext[i].gameObject.SetActive(false); ra[i].gameObject.SetActive(false); tk[i].gameObject.SetActive(false); lbl[i].gameObject.SetActive(false); lblPill[i].gameObject.SetActive(false);
                    continue;
                }
                Vector3 end;
                ext[i].gameObject.SetActive(false); ra[i].gameObject.SetActive(false);
                if (cur.m == Mission.Circum) end = IW(cur.V[i], LineY);
                else
                {
                    cur.Foot(gx, gy, i, out double fx, out double fy, out double t);
                    end = GW(fx, fy, LineY);
                    if (t < 0 || t > 1)
                    {
                        var v = t < 0 ? cur.V[i] : cur.V[(i + 1) % 3];
                        ext[i].SetPosition(0, IW(v, LineY - 0.005f)); ext[i].SetPosition(1, end);
                        ext[i].gameObject.SetActive(true);
                    }
                    // 직각 표시(수선의 발)
                    var toP = new Vector2(P.x - end.x, P.z - end.z);
                    if (toP.magnitude > 0.2f)
                    {
                        var a = cur.V[i]; var b = cur.V[(i + 1) % 3];
                        var sdir = new Vector2(b.x - a.x, b.y - a.y).normalized; var ndir = toP.normalized;
                        const float q = 0.17f;
                        ra[i].SetPosition(0, end + new Vector3(sdir.x, 0, sdir.y) * q);
                        ra[i].SetPosition(1, end + new Vector3(sdir.x + ndir.x, 0, sdir.y + ndir.y) * q);
                        ra[i].SetPosition(2, end + new Vector3(ndir.x, 0, ndir.y) * q);
                        ra[i].gameObject.SetActive(true);
                    }
                }
                gl[i].SetPosition(0, P); gl[i].SetPosition(1, end);
                gl[i].widthMultiplier = (st.red || (bridgeMode && bridgeGreen) ? 0.08f : 0.05f) * pulse;
                gl[i].gameObject.SetActive(true);
                // 길이 글자: 선의 가운데에서 선에 수직으로 조금 비켜서(다리 단계 오답이면 좌우로 흔들린다)
                var mid = (P + end) * 0.5f; var d = end - P; float len = d.magnitude;
                if (len > 0.45f)
                {
                    var n = new Vector3(-d.z, 0, d.x) / len;
                    if (Vector3.Dot(n, mid - new Vector3(0, 0, -2)) < 0) n = -n;
                    var lp = mid + n * 0.34f + Vector3.up * 0.02f + Vector3.right * wob;
                    lbl[i].transform.position = lp;
                    lbl[i].gameObject.SetActive(true);
                    lblPill[i].widthMultiplier = lblH[i] + 0.06f;
                    lblPill[i].SetPosition(0, lp + new Vector3(-lblHalf[i], -0.01f, 0)); lblPill[i].SetPosition(1, lp + new Vector3(lblHalf[i], -0.01f, 0));
                    if (!lblPill[i].gameObject.activeSelf) lblPill[i].gameObject.SetActive(true);
                }
                else { lbl[i].gameObject.SetActive(false); lblPill[i].gameObject.SetActive(false); }
                bool eq = bridgeMode ? bridgeGreen : st.red || pairEq[i] || pairEq[(i + 2) % 3];
                if (eq && len > 0.3f) TickAt(tk[i], P, end, 1, LineColor(i)); else tk[i].gameObject.SetActive(false);
            }
            pinGlow.gameObject.SetActive(st.red);
            if (st.red)
            {
                redT += Time.deltaTime;
                pinGlow.position = new Vector3(tip.x, FilmY + 0.02f, tip.z);
                pinGlow.localScale = Vector3.one * (0.9f + 0.25f * Mathf.Sin(Time.time * 8f));
            }
        }

        /// <summary>같은 길이 표시(교과서의 짧은 빗금). 길이 0 이면 그리지 않는다(0 나눗셈 방지).</summary>
        void TickAt(LineRenderer t, Vector3 a, Vector3 b, int n, Color c)
        {
            var d = b - a; float len = d.magnitude;
            if (len < 1e-3f) { t.gameObject.SetActive(false); return; }
            var m = (a + b) * 0.5f; var u = d / len; var nrm = new Vector3(-u.z, 0, u.x) * 0.14f;
            t.SetPosition(0, m - nrm); t.SetPosition(1, m + nrm);
            SetColor(t, c);
            t.gameObject.SetActive(true);
        }

        // ─────────────────────────────── 매 프레임
        void Update()
        {
            float dt = Time.deltaTime;
            DoLayout();
            UpdateCamera(dt);
            UpdateUi(dt);
            if (hitStop > 0) { hitStop -= Time.unscaledDeltaTime; if (hitStop <= 0) Time.timeScale = 1f; }
            dashMat.mainTextureOffset = new Vector2(-Time.time * 1.6f, 0);
            UpdateRipples(dt);
            UpdateWiggles(dt);
            UpdateConsViz(dt);
            UpdateBridgeFx(dt);
            UpdateCard(dt);

            // 소프트웨어 렌더러: 타이틀(궤도 카메라로 넓게 보이는 화면)에서는 그림자를 끄고, 플레이에서만 단단한 그림자
            if (MgfBridge.LowGfx) { var want = st.phase == "title" ? LightShadows.None : LightShadows.Hard; if (keyLight.shadows != want) keyLight.shadows = want; }
            if (st.phase == "title") { TitleDemo(); TitleInput(); return; }
            if (st.phase == "gameover" || st.phase == "clear") { EndInput(); return; }

            if (curPlate != null && plateEnterT < 1f)
            {
                plateEnterT = Mathf.Min(1f, plateEnterT + dt / 0.42f);
                float k = plateEnterT;
                float y = Mathf.LerpUnclamped(-0.3f, 0f, 1f + 2.4f * Mathf.Pow(k - 1f, 3) + 1.4f * Mathf.Pow(k - 1f, 2));
                curPlate.go.transform.position = curPlate.pivot + Vector3.up * y;
            }
            if (bannerT > 0) { bannerT -= dt; if (bannerT <= 0) bannerBg.gameObject.SetActive(false); }

            // 명판 제한 시간(연출 중·해금 연출 중·무제한 명판은 흐르지 않는다)
            if (!busy && !roundPending && plateLimit > 0 && pinReady)
            {
                plateT += dt;
                int left = Mathf.CeilToInt(plateLimit - plateT);
                if (left <= 5 && left != lastAlarmSec && left > 0) { lastAlarmSec = left; Play(clAlarm, 0.5f); }
                if (plateT >= plateLimit) { Timeout(); return; }
            }

            PlayInput();
            UpdatePin(dt);
            if (curPin != null && cur != null)
            {
                var tip = PinTipWorld();
                DrawMeasure(tip, tip.x / CELL, tip.z / CELL);
            }
            UpdateOnboarding(dt);
            UpdateHandles();
            UpdateTaps();
            UpdateGhostIdle();
            idleT += dt;
        }

        // ── 타이틀: 데모 명판 위에서 핀이 한 칸씩 옮겨 가며 세 선의 길이가 바뀌다가, 세 길이가 같아지는 순간 빨개지고 박혀 원이 스윕한다
        float demoT;
        int demoStep = -1;
        void TitleDemo()
        {
            demoT += Time.deltaTime;
            const float stepDur = 0.34f;
            float total = demoPath.Length * stepDur + 3.2f;
            if (demoT > total) { demoT = 0; demoStep = -1; }
            int s = Mathf.Min(demoPath.Length - 1, Mathf.FloorToInt(demoT / stepDur));
            float f = Mathf.Clamp01((demoT - s * stepDur) / stepDur);
            var t = demoPin.root.transform;
            Vector3 a = IW(demoPath[s], FilmY), b = IW(demoPath[Mathf.Min(s + 1, demoPath.Length - 1)], FilmY);
            bool moving = s < demoPath.Length - 1;
            float e = moving ? f * f * (3 - 2 * f) : 0f;
            var pos = Vector3.Lerp(a, b, e) + Vector3.up * (moving ? Mathf.Sin(f * Mathf.PI) * 0.12f : 0f);
            if (s != demoStep)
            {
                demoStep = s;
                pinPos = RP.Of(demoPath[s]);
                measureOn = true;
                for (int i = 0; i < 3; i++) shownH[i] = -1;
                UpdateMeasure(true);
                st.red = pinPos.Is(demoProb.ans);
                if (st.red) { redT = 0; }
            }
            float after = demoT - (demoPath.Length - 1) * stepDur;
            // 빨개진 뒤 0.7초: 핀이 쾅 박히고 외접원이 스윕
            float lift = 0f;
            if (!moving && after > 0.7f && after < 0.85f) lift = Mathf.Sin((after - 0.7f) / 0.15f * Mathf.PI) * 0.4f;
            t.position = pos + Vector3.up * lift;
            t.rotation = Quaternion.identity;
            DrawMeasure(t.position, t.position.x / CELL, t.position.z / CELL);
            if (!moving && after > 0.85f && after < 3.0f)
            {
                float k = Mathf.Clamp01((after - 0.85f) / 0.8f);
                var c = IW(demoProb.ans, LineY);
                float r = Mathf.Sqrt(demoProb.R2) * CELL;
                groove.gameObject.SetActive(true); grooveUnder.gameObject.SetActive(true);
                CirclePts(groove, c, r, 97, k); CirclePts(grooveUnder, c + Vector3.down * 0.005f, r, 97, k);
                float fade = after > 2.6f ? 1f - (after - 2.6f) / 0.4f : 1f;
                SetColor(groove, new Color(Match.r, Match.g, Match.b, fade)); SetColor(grooveUnder, new Color(0.05f, 0.02f, 0.02f, 0.7f * fade));
            }
            else { groove.gameObject.SetActive(false); grooveUnder.gameObject.SetActive(false); }
            for (int i = 0; i < 3; i++) vLabels[i].gameObject.SetActive(true);
            if (demoStep == 0) ShowPlateMarks();
        }

        void TitleInput()
        {
            ctaShine.anchoredPosition = new Vector2(-200 + (Time.time % 2.6f) / 2.6f * 520f, 0);
            float br = 1f + Mathf.Sin(Time.time * 2.2f) * 0.025f;
            if (MgfPointer.Down) { ctaDown = true; }
            if (ctaDown)
            {
                ctaRt.localScale = Vector3.one * 0.93f;
                if (MgfPointer.Up || !MgfPointer.Held) { ctaDown = false; ctaRt.localScale = Vector3.one * 1.08f; HideMeasure(); groove.gameObject.SetActive(false); grooveUnder.gameObject.SetActive(false); Begin(); sfx.PlayOneShot(clClang, 0.6f); }
            }
            else ctaRt.localScale = Vector3.Lerp(ctaRt.localScale, Vector3.one * br, Time.deltaTime * 10f);
        }

        void EndInput()
        {
            endCtaShine.anchoredPosition = new Vector2(-200 + (Time.time % 2.6f) / 2.6f * 520f, 0);
            if (Time.time - endShownAt < 0.6f) return;
            if (MgfPointer.Down)
            {
                if (Over(endCtaRt, MgfPointer.Position, 10)) { endCtaDown = true; endCtaRt.localScale = Vector3.one * 0.93f; }
                else { MgfFx.Punch(endCtaRt, 0.1f, 0.25f); Play(clTick, 0.4f); }
            }
            if (endCtaDown && (MgfPointer.Up || !MgfPointer.Held)) { endCtaDown = false; endCtaRt.localScale = Vector3.one; Begin(); sfx.PlayOneShot(clClang, 0.6f); }
        }

        // ─────────────────────────────── 입력(플레이)
        bool PointerGrid(out double gx, out double gy, out Vector3 w)
        {
            gx = gy = 0;
            if (!MgfPointer.OnPlane(cam, FilmY, out w)) return false;
            gx = w.x / CELL; gy = w.z / CELL;
            return true;
        }

        static bool NearFilm(Vector3 w) => Mathf.Abs(w.x) <= FX + 1.1f && Mathf.Abs(w.z) <= FZ + 1.1f;

        void PlayInput()
        {
            // 키보드(데스크톱): 방향키로 한 칸(1단계·다리 단계), 스페이스·엔터로 박기/찍기
            if (pinReady && !busy && !roundPending)
            {
                int kx = (Input.GetKeyDown(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKeyDown(KeyCode.LeftArrow) ? 1 : 0);
                int ky = (Input.GetKeyDown(KeyCode.UpArrow) ? 1 : 0) - (Input.GetKeyDown(KeyCode.DownArrow) ? 1 : 0);
                if ((kx != 0 || ky != 0) && step != Step.Build)
                {
                    var b = pinTarget.IsLattice ? pinTarget.Lattice : new IP(Mathf.RoundToInt((float)pinTarget.fx), Mathf.RoundToInt((float)pinTarget.fy));
                    SetTarget(RP.Of(new IP(Mathf.Clamp(b.x + kx, -Rules.GX, Rules.GX), Mathf.Clamp(b.y + ky, -Rules.GY, Rules.GY))));
                }
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) Plant();
            }

            if (MgfPointer.Down)
            {
                downScreen = MgfPointer.Position;
                if (Over(plantRt, downScreen, 10))
                {
                    press = Press.PlantBtn; plantRt.localScale = Vector3.one * 0.92f; Play(clTick, 0.4f);
                    return;
                }
                if (busy || roundPending) { Refuse(null, step == Step.Bridge ? "직선이 드러나는 중이다" : "원이 끝날 때까지 기다려라"); press = Press.None; return; }
                if (!pinReady) { press = Press.None; if (PointerGrid(out _, out _, out var w0)) Ripple(w0); return; }
                if (!PointerGrid(out double gx, out double gy, out var w)) { press = Press.None; return; }
                if (!NearFilm(w))
                {
                    press = Press.None; Ripple(w);
                    hintTxt.text = step == Step.Build ? "변을 누르면 수직이등분선 · 꼭짓점을 누르면 각의 이등분선" : "모눈 위를 누르면 핀이 그쪽으로 옮겨 간다";
                    Play(clRefuse, 0.35f);
                    return;
                }
                if (step == Step.Build) { press = Press.None; BuildTap(gx, gy, w); return; }
                press = Press.Pin;
                SetTarget(SnapLattice(gx, gy));
                Ripple(w);
                return;
            }
            bool released = MgfPointer.Up || !MgfPointer.Held;
            if (press == Press.PlantBtn)
            {
                if (released)
                {
                    press = Press.None; plantRt.localScale = Vector3.one;
                    if (Over(plantRt, MgfPointer.Position, 24)) Plant();
                }
                return;
            }
            if (press == Press.None) return;
            if (!PointerGrid(out double hx, out double hy, out _)) return;
            if (press == Press.Pin)
            {
                SetTarget(SnapLattice(hx, hy));
                if (released) press = Press.None;
            }
        }

        static RP SnapLattice(double gx, double gy)
        {
            int x = Mathf.Clamp(Mathf.RoundToInt((float)gx), -Rules.GX, Rules.GX), y = Mathf.Clamp(Mathf.RoundToInt((float)gy), -Rules.GY, Rules.GY);
            return RP.Of(new IP(x, y));
        }

        // 거절 연출(대상 좌우 흔들림 + 이유 한 문장 + 전용 거절음) — 화면 흔들림은 오답 전용이라 쓰지 않는다
        Transform wiggleT; float wiggleTime = 9f; Vector3 wiggleBase;
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
            if (plantPressT < 0.4f) { plantPressT += dt; }
        }

        void Ripple(Vector3 wp)
        {
            int i = rippleNext++ % 3;
            rippleT[i] = 0;
            ripples[i].gameObject.SetActive(true);
            ripples[i].transform.position = new Vector3(wp.x, LineY, wp.z);
        }

        void UpdateRipples(float dt)
        {
            for (int i = 0; i < 3; i++)
            {
                if (rippleT[i] > 0.5f) continue;
                rippleT[i] += dt;
                float k = Mathf.Clamp01(rippleT[i] / 0.5f);
                CirclePts(ripples[i], ripples[i].transform.position, 0.12f + k * 0.6f, 33);
                SetColor(ripples[i], new Color(Cream.r, Cream.g, Cream.b, 0.8f * (1 - k)));
                if (rippleT[i] > 0.5f) ripples[i].gameObject.SetActive(false);
            }
        }

        // ─────────────────────────────── 온보딩: 핀 위 맥동 링 + 핀에서 모눈 안쪽으로 흐르는 점선(정답 자리를 가리키지 않는다)
        void UpdateOnboarding(float dt)
        {
            bool first = step == Step.Dist && idx == 0 && onb == 0 && pinReady && !busy;
            bool canPlant = step != Step.Build || markers.Count > 0;
            bool pulseBtn = (step == Step.Dist && idx == 0 && onb == 2) || (st.red && cur != null && cur.tutorial && step == Step.Dist)
                || (step == Step.Bridge && (bridgeGreen || bridgeDone)) || (step == Step.Build && markers.Count > 0 && PinAtMarker() && !stepping);
            st.onboarding = step == Step.Dist && idx == 0 && onb < 3;
            pulseRing.gameObject.SetActive(first);
            onbPath.gameObject.SetActive(first);
            if (first)
            {
                bool big = idleT > 8f;
                var from = PinTipWorld() + Vector3.up * 0.05f;
                float pr = (big ? 0.55f : 0.4f) + Mathf.Repeat(Time.time, 1f) * 0.3f;
                CirclePts(pulseRing, new Vector3(from.x, LineY, from.z), pr, 49);
                SetColor(pulseRing, new Color(Match.r, Match.g, Match.b, 1 - Mathf.Repeat(Time.time, 1f)));
                var to = GW(-3, -2, LineY);
                int n = onbPath.positionCount;
                for (int i = 0; i < n; i++)
                {
                    float k = (float)i / (n - 1);
                    onbPath.SetPosition(i, Vector3.Lerp(from, to, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.5f);
                }
                onbPath.widthMultiplier = big ? 0.16f : 0.11f;
                SetColor(onbPath, new Color(Cream.r, Cream.g, Cream.b, 0.95f));
            }
            // 박기 버튼 맥동(빨간색이 떴을 때 안내 명판에서만)
            float s = pulseBtn ? 1f + 0.06f * Mathf.Sin(Time.time * 9f) : 1f;
            if (press != Press.PlantBtn) plantRt.localScale = Vector3.Lerp(plantRt.localScale, Vector3.one * s, dt * 12f);
            plantShine.anchoredPosition = new Vector2(-220 + (Time.time % 2.2f) / 2.2f * 480f, 0);
            plantFace.color = !canPlant ? Hex("6E5046") : (st.red && step == Step.Dist) ? Hex("E4513A") : step == Step.Bridge ? (bridgeGreen || bridgeDone ? Hex("2E9C7C") : Hex("3B6F60")) : Hex("C8452F");
        }

        // ─────────────────────────────── 박기 → 판정 → 연출
        void Plant() => Plant(false);

        /// <summary>1단계·2단계 「핀 박기」, 다리 단계 「점 찍기」(→ Stamp). force = QA 오답 훅(교점이 판 밖일 때 출발점에 박기).</summary>
        void Plant(bool force)
        {
            if (step == Step.Bridge) { Stamp(); return; }
            if (st.phase != "playing" || cur == null || busy || roundPending || curPin == null || !pinReady) { if (st.phase == "playing" && (busy || roundPending)) Refuse(null, "원이 끝날 때까지 기다려라"); return; }
            if (step == Step.Build && !force)
            {
                // 2단계: 학생이 그은 두 선의 교점에서만 박는다(교점이 없으면 무감점 안내)
                if (markers.Count == 0) { Refuse(plantRt, lines.Count < 2 ? "선 두 개를 그어 교점을 만들어라" : "두 선이 판 밖에서 만난다 · 다른 선을 그어라"); return; }
                if (!PinAtMarker()) { Refuse(plantRt, "교점 표식을 눌러 핀을 옮겨라"); return; }
                if (stepping) { stepping = false; pinPos = pinTarget; curPin.root.transform.position = PW(pinPos, FilmY); }
            }
            if (stepping) { stepping = false; }          // 걸음 도중이면 마지막으로 선 격자점에 박는다
            StopGhost();
            pinTarget = pinPos; targetRing.gameObject.SetActive(false);
            bool ok = pinPos.Is(cur.ans);                  // 정수 판정: 핀 좌표 = 정답 격자점
            roundOk = ok;
            roundRetry = !ok && cur.tutorial && tutRetries < 2;
            if (!attemptCounted) { attemptCounted = true; st.attempts++; if (ok) st.firstTry++; if (step == Step.Build) { att2++; if (ok) ok2++; } }
            if (roundRetry) { tutRetries++; st.combo = 0; }
            else if (ok)
            {
                st.combo++; st.maxCombo = Mathf.Max(st.maxCombo, st.combo);
                int left = plateLimit > 0 ? Mathf.Max(0, Mathf.CeilToInt(plateLimit - plateT)) : 0;
                st.score += ((step == Step.Dist ? 100 : 150) + 5 * left + (concurrentBonus ? 50 : 0)) * Mathf.Min(3, st.combo);
                st.solved++; st.plates++;
                if (tutRetries == 0) { if (cur.m == Mission.Circum) streakO++; else streakI++; }
            }
            else
            {
                st.combo = 0;
                curPin.bent = true;
                st.lives--;
                if (cur.m == Mission.Circum) streakO = 0; else streakI = 0;
            }
            if (tutRetries > 0 && ok) { if (cur.m == Mission.Circum) streakO = 0; else streakI = 0; }
            busy = true; roundPending = true;
            hintTxt.text = "";
            MgfBridge.NotifyChanged();
            roundCo = StartCoroutine(RoundCo(ok, false));
        }

        void Timeout()
        {
            if (roundPending || busy || cur == null || curPin == null) return;
            StopGhost(); StopFoldFx();
            stepping = false; pinTarget = pinPos;
            roundOk = false; roundRetry = false;
            if (!attemptCounted) { attemptCounted = true; st.attempts++; if (step == Step.Build) att2++; }
            st.combo = 0; curPin.bent = true; st.lives--;
            if (cur.m == Mission.Circum) streakO = 0; else streakI = 0;
            busy = true; roundPending = true;
            MgfBridge.NotifyChanged();
            roundCo = StartCoroutine(RoundCo(false, true));
        }

        IEnumerator RoundCo(bool ok, bool timeout)
        {
            var spec = cur;
            var pt = curPin.root.transform;
            var tip = PW(pinPos, FilmY);
            if (!timeout)
            {
                // 예비 동작 → 내리꽂기 → 히트스톱 + 스쿼시
                var up = tip + Vector3.up * 0.55f;
                for (float e = 0; e < 0.08f; e += Time.deltaTime) { pt.position = Vector3.Lerp(tip, up, e / 0.08f); pt.rotation = Quaternion.identity; yield return null; }
                for (float e = 0; e < 0.06f; e += Time.deltaTime) { float k = e / 0.06f; pt.position = Vector3.Lerp(up, tip + Vector3.down * 0.06f, k * k); yield return null; }
                pt.position = tip + Vector3.down * 0.06f;
                curPin.body.localScale = new Vector3(1.25f, 0.8f, 1.25f);
                MgfFx.Burst(tip + Vector3.up * 0.1f, ok ? new Color(1f, 0.85f, 0.6f) : Sand * 0.9f, 10, 1.4f, 0.1f);
                Play(ok ? clClang : clThunk, 0.85f);
                Time.timeScale = 0.02f; hitStop = 0.09f;
                yield return null;
                while (hitStop > 0) yield return null;
                for (float e = 0; e < 0.12f; e += Time.deltaTime) { curPin.body.localScale = Vector3.Lerp(new Vector3(1.25f, 0.8f, 1.25f), Vector3.one, e / 0.12f); yield return null; }
                curPin.body.localScale = Vector3.one;
            }

            if (ok)
            {
                // 컴퍼스 팔이 한 바퀴 돌며 외접원(세 꼭짓점을 지남)/내접원(세 변에 접함)을 새긴다 — 지나는 점마다 불꽃
                var c = IW(spec.ans, LineY);
                float r = spec.m == Mission.Circum ? Mathf.Sqrt(spec.R2) * CELL : spec.inR * CELL;
                var hits = new float[3];
                for (int i = 0; i < 3; i++)
                {
                    Vector2 d;
                    if (spec.m == Mission.Circum) d = new Vector2(spec.V[i].x - spec.ans.x, spec.V[i].y - spec.ans.y);
                    else { spec.Foot(spec.ans.x, spec.ans.y, i, out double fx, out double fy, out _); d = new Vector2((float)(fx - spec.ans.x), (float)(fy - spec.ans.y)); }
                    hits[i] = Mathf.Repeat(Mathf.Atan2(d.y, d.x), Mathf.PI * 2) / (Mathf.PI * 2);
                }
                bool[] fired = new bool[3];
                groove.gameObject.SetActive(true); grooveUnder.gameObject.SetActive(true); arm.gameObject.SetActive(true);
                SetColor(groove, Match); SetColor(grooveUnder, new Color(0.08f, 0.02f, 0.02f, 0.7f)); SetColor(arm, Cream);
                groove.widthMultiplier = 0.09f;
                Play(clScrape, 0.5f);
                const float sweep = 0.75f;
                for (float e = 0; e < sweep; e += Time.deltaTime)
                {
                    float f = Mathf.SmoothStep(0, 1, e / sweep);
                    CirclePts(groove, c, r, 97, f); CirclePts(grooveUnder, c + Vector3.down * 0.005f, r, 97, f);
                    float a = f * Mathf.PI * 2;
                    arm.SetPosition(0, c + Vector3.up * 0.3f); arm.SetPosition(1, c + new Vector3(Mathf.Cos(a) * r, 0.02f, Mathf.Sin(a) * r));
                    for (int i = 0; i < 3; i++)
                        if (!fired[i] && f >= hits[i])
                        {
                            fired[i] = true;
                            var hp = c + new Vector3(Mathf.Cos(hits[i] * Mathf.PI * 2) * r, 0.05f, Mathf.Sin(hits[i] * Mathf.PI * 2) * r);
                            MgfFx.Glow(hp, new Color(1f, 0.6f, 0.45f), 5, 0.3f);
                            Play(clPing, 0.5f);
                        }
                    yield return null;
                }
                CirclePts(groove, c, r, 97); CirclePts(grooveUnder, c + Vector3.down * 0.005f, r, 97);
                arm.gameObject.SetActive(false);
                Play(clChime, 0.6f);
                // 수학이 드러나는 순간: 같은 길이의 세 선(1단계 선 그대로) + 한 줄
                measureOn = true; feedbackMode = false;
                UpdateMeasure(true);
                revealText.text = spec.m == Mission.Circum ? "OA=OB=OC → 외심 O" : "세 변까지 거리가 같다 → 내심 I";
                revealText.transform.position = c + new Vector3(0, 0.1f, Mathf.Min(r, 2.2f) * 0.6f + 0.45f);
                revealText.gameObject.SetActive(true);
                shimmer.gameObject.SetActive(true);
                for (float e = 0; e < 0.95f; e += Time.deltaTime)
                {
                    float k = e / 0.95f;
                    groove.widthMultiplier = 0.09f + Mathf.Sin(Mathf.Min(1, k * 3) * Mathf.PI) * 0.07f;
                    SetColor(groove, Color.Lerp(Color.Lerp(Match, Color.white, 0.5f), Match, Mathf.Min(1, k * 2.5f)));
                    float a = k * Mathf.PI * 2 + Mathf.PI * 0.5f;
                    shimmer.position = c + new Vector3(Mathf.Cos(a) * r, 0.05f, Mathf.Sin(a) * r);
                    shimmer.localScale = Vector3.one * (0.8f + Mathf.Sin(k * Mathf.PI) * 0.6f);
                    revealText.transform.localScale = Vector3.one * (k < 0.15f ? Mathf.Lerp(0.6f, 1.08f, k / 0.15f) : Mathf.Lerp(1.08f, 1f, (k - 0.15f) / 0.85f));
                    yield return null;
                }
                shimmer.gameObject.SetActive(false);
                MgfFx.Glow(c, Match, 6 + 4 * Mathf.Min(3, st.combo), 0.4f);
                if (onb == 2 || (step == Step.Dist && idx == 0)) onb = 3;
                // 핀은 트레이로, 명판은 랙(가로) 또는 화면 위로
                var from = pt.position; var plateFrom = curPlate.go.transform.position;
                var to = land ? RackSlot(rack.Count) : new Vector3(-4.5f, 2.5f, 7.5f);
                HideMeasure(); HideMarks();
                for (float e = 0; e < 0.45f; e += Time.deltaTime)
                {
                    float k = e / 0.45f, s = k * k * (3 - 2 * k);
                    pt.position = Vector3.Lerp(from, SlotPos(curPin.slot) + Vector3.up * 0.18f, s) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 1.2f;
                    curPlate.go.transform.position = Vector3.Lerp(plateFrom, to, s) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.6f;
                    curPlate.go.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.26f, s);
                    yield return null;
                }
            }
            else
            {
                // 왜 틀렸는지 그림으로: 핀에서의 세 거리선(1단계 선) — 서로 다른 길이는 주황, 같은 쌍은 초록
                Play(timeout ? clAlarm : clThunk, 0.6f);
                measureOn = true; feedbackMode = true;
                if (step == Step.Build) DimCons(true);   // 거리선(PA·PB·PC)이 돋보이게 작도 선은 잠깐 흐리게
                for (int i = 0; i < 3; i++) shownH[i] = -1;
                UpdateMeasure(true);
                gaugeHead.text = spec.m == Mission.Circum ? "세 꼭짓점까지의 거리가 같지 않다" : "세 변까지의 거리가 같지 않다";
                if (!roundRetry)
                {
                    trueRing.gameObject.SetActive(true);
                    CirclePts(trueRing, IW(spec.ans, LineY + 0.01f), 0.3f, 33);
                    SetColor(trueRing, Match);
                    centerLetter.text = spec.m == Mission.Circum ? "O" : "I";
                    centerLetter.transform.position = IW(spec.ans, LineY + 0.02f) + new Vector3(0.42f, 0, 0.36f);
                    centerLetter.gameObject.SetActive(true);
                    shakeOffset = Vector3.zero;
                    MgfFx.Shake(cam, 0.08f, 0.22f);
                }
                hintTxt.text = timeout ? "시간 초과 · " + WrongWhy() : WrongWhy();
                yield return new WaitForSeconds(roundRetry ? 1.3f : 1.25f);
                if (roundRetry)
                {
                    // 안내 명판: 핀은 굽지 않고 그 자리에서 다시 옮길 수 있다
                    pt.position = tip;
                    feedbackMode = false; measureOn = step == Step.Dist;
                    var why = hintTxt.text;
                    if (!measureOn)
                    {
                        // 2단계 안내 명판: 선을 지우고 핀을 출발점으로 — 다른 작도를 골라 다시
                        HideMeasure(); ClearBuild();
                        pinPos = pinTarget = RP.Of(Rules.Start); pt.position = PW(pinPos, FilmY);
                        why += " · 선을 다시 골라 그어라";
                    }
                    else { for (int i = 0; i < 3; i++) shownH[i] = -1; UpdateMeasure(true); why += " · 다시 옮겨 박아라"; }
                    busy = false; roundPending = false; roundCo = null;
                    if (!measureOn) RefreshBuildGauge();
                    hintTxt.text = why;
                    MgfBridge.NotifyChanged();
                    yield break;
                }
                Play(clThunk, 0.5f);
                for (float e = 0; e < 0.2f; e += Time.deltaTime) { curPin.body.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(0, 38, e / 0.2f)); yield return null; }
                SetPinMat(curPin, bentMat);
                HideMeasure(); HideMarks();
                var plateFrom = curPlate.go.transform.position; var pinFrom = pt.position;
                var scrapTo = land ? new Vector3(8.2f, 0.3f, -2.5f) : new Vector3(7.5f, 0.5f, 0.5f);
                for (float e = 0; e < 0.42f; e += Time.deltaTime)
                {
                    float k = e / 0.42f, s = 1 - (1 - k) * (1 - k);
                    curPlate.go.transform.position = Vector3.Lerp(plateFrom, scrapTo, s) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.9f;
                    curPlate.go.transform.rotation = Quaternion.Euler(0, s * 140f, Mathf.Sin(k * Mathf.PI) * 25f);
                    curPlate.go.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.3f, s);
                    pt.position = Vector3.Lerp(pinFrom, SlotPos(curPin.slot) + new Vector3(0.1f, 0.22f, 0.05f), s) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 1.4f;
                    yield return null;
                }
            }
            CleanupRound();
        }

        string WrongWhy()
        {
            var s = cur;
            var p = pinPos;
            if (step == Step.Build && lines.Count >= 2)
            {
                // 핀이 선 교점을 지나는 두 선의 종류로 이유를 말한다(정수 판정 Line.Through)
                int nP = 0, nB = 0;
                foreach (var l in lines) if (l.geo.Through(p)) { if (l.perp) nP++; else nB++; }
                if (s.m == Mission.Circum && nB >= 2 && nP == 0) return "각의 이등분선의 교점은 내심이다\n외심은 세 변의 수직이등분선의 교점";
                if (s.m == Mission.In && nP >= 2 && nB == 0) return "수직이등분선의 교점은 외심이다\n내심은 세 내각의 이등분선의 교점";
                if (nP > 0 && nB > 0) return "수직이등분선과 각의 이등분선이 만난 점은\n외심도 내심도 아니다";
            }
            if (s.m == Mission.Circum && s.kind == Kind.Obtuse && p.IsLattice && s.InsideStrict(p.Lattice)) return "둔각삼각형의 외심은 삼각형 밖에 있다";
            if (s.m == Mission.Circum && s.kind == Kind.Right) return "직각삼각형의 외심은 빗변의 중점이다";
            if (s.m == Mission.In && p.IsLattice && !s.InsideStrict(p.Lattice)) return "내심은 언제나 삼각형 안에 있다";
            return s.m == Mission.Circum ? "세 꼭짓점까지의 거리가 같지 않다" : "세 변까지의 거리가 같지 않다";
        }

        void HideMarks()
        {
            for (int i = 0; i < 3; i++) vLabels[i].gameObject.SetActive(false);
            groove.gameObject.SetActive(false); grooveUnder.gameObject.SetActive(false); arm.gameObject.SetActive(false);
            trueRing.gameObject.SetActive(false); centerLetter.gameObject.SetActive(false); revealText.gameObject.SetActive(false);
            rightMark.gameObject.SetActive(false); targetRing.gameObject.SetActive(false);
            triStatic.gameObject.SetActive(false);
            ClearBuild();
        }

        void HideRoundFx()
        {
            HideMarks(); HideMeasure();
            shimmer.gameObject.SetActive(false);
            pulseRing.gameObject.SetActive(false); onbPath.gameObject.SetActive(false);
        }

        /// <summary>한 판의 뒷정리(연출 도중 QA 훅이 오면 즉시 이 상태로 건너뛴다). 여러 번 불려도 안전.</summary>
        void CleanupRound()
        {
            if (!roundPending) return;
            roundPending = false; roundCo = null;
            if (wiggleT != null && wiggleTime < 0.25f) wiggleT.position = wiggleBase;
            wiggleT = null;
            Time.timeScale = 1f; hitStop = 0;
            HideRoundFx();
            feedbackMode = false;
            if (curPin != null)
            {
                curPin.body.localScale = Vector3.one;
                curPin.body.localRotation = curPin.bent ? Quaternion.Euler(0, 0, 38) : Quaternion.identity;
                if (curPin.bent) SetPinMat(curPin, bentMat);
            }
            if (curPlate != null)
            {
                if (roundOk && land) { rack.Add(curPlate); SnapRack(curPlate, rack.Count - 1); }
                else curPlate.go.SetActive(false);
            }
            curPlate = null;
            curPin = null;
            busy = false;
            PlacePinsInTray();
            if (st.lives <= 0) { EndGame(); ShowEnd(); MgfBridge.NotifyChanged(); return; }
            if (step == Step.Dist)
            {
                // 해금 → 다리 단계(다음 삼각형을 바로 띄운다 — 빈 판 금지)
                if (streakO >= Rules.UnlockStreak && streakI >= Rules.UnlockStreak) StartBridge();
                else { idx++; SpawnPlate(gen.Next(1, idx, rng)); }
            }
            else
            {
                idx++;
                if (idx >= Rules.Stage2Plates) { EndGame(); ShowEnd(); }
                else SpawnPlate(gen.Next(2, idx, rng));
            }
            MgfBridge.NotifyChanged();
        }

        void FinishRound()
        {
            if (!roundPending) return;
            if (roundCo != null) StopCoroutine(roundCo);
            CleanupRound();
        }

        // ─────────────────────────────── UI 갱신(문자열은 값이 바뀔 때만)
        int shownStage = -1, shownPipKey = -1, shownProg = -1, shownCombo = -1;
        void UpdateUi(float dt)
        {
            if (st.phase == "title") return;
            if (st.phase == "gameover" || st.phase == "clear")
            {
                endCountT += dt;
                float k = Mathf.Clamp01(endCountT / 1.1f);
                int sc = Mathf.RoundToInt(st.score * (1 - Mathf.Pow(1 - k, 3)));
                if (sc != shownScoreInt) { shownScoreInt = sc; endScore.text = $"점수 {sc}"; endScore.transform.localScale = Vector3.one * 1.08f; }
                endScore.transform.localScale = Vector3.Lerp(endScore.transform.localScale, Vector3.one, dt * 8f);
                return;
            }
            shownScore = Mathf.MoveTowards(shownScore, st.score, Mathf.Max(60f, Mathf.Abs(st.score - shownScore) * 6f) * dt);
            int si = Mathf.RoundToInt(shownScore);
            if (si != shownScoreInt) { shownScoreInt = si; scoreTxt.text = $"{si}점"; scoreTxt.transform.localScale = Vector3.one * 1.15f; }
            scoreTxt.transform.localScale = Vector3.Lerp(scoreTxt.transform.localScale, Vector3.one, dt * 8f);
            if (st.combo != shownCombo) { shownCombo = st.combo; comboTxt.text = st.combo >= 2 ? $"연속 ×{Mathf.Min(3, st.combo)}" : ""; }
            if ((int)step != shownStage)
            {
                shownStage = (int)step;
                stageTxt.text = step == Step.Dist ? "1단계 · 거리" : step == Step.Bridge ? "다리 단계" : "2단계 · 작도";
                MgfFx.Punch(stageTxt.transform, 0.2f, 0.3f);
            }
            int pk = (int)step * 100 + Mathf.Min(2, streakO) * 10 + Mathf.Min(2, streakI);
            if (pk != shownPipKey)
            {
                shownPipKey = pk;
                bool s1 = step == Step.Dist;
                for (int j = 0; j < 2; j++)
                {
                    pipO[j].gameObject.SetActive(s1); pipI[j].gameObject.SetActive(s1);
                    pipO[j].color = streakO > j ? VerdLit : new Color(1, 1, 1, 0.18f);
                    pipI[j].color = streakI > j ? Amber : new Color(1, 1, 1, 0.18f);
                }
                foreach (Transform ch in progRt) if (ch.GetComponent<TextMeshProUGUI>() != progTxt) ch.gameObject.SetActive(s1);
            }
            int prog = step == Step.Build ? idx : step == Step.Bridge ? 100 + bridgeIdx : -1;
            if (prog != shownProg)
            {
                shownProg = prog;
                progTxt.text = step == Step.Build ? $"명판 {Mathf.Min(idx + 1, Rules.Stage2Plates)}/{Rules.Stage2Plates}" : step == Step.Bridge ? $"다리 {bridgeIdx + 1}/2" : "";
            }
            // 제한 시간 막대
            float frac = plateLimit > 0 ? Mathf.Clamp01(1 - plateT / plateLimit) : 1f;
            timerFill.localScale = new Vector3(frac, 1, 1);
            var tc = plateLimit <= 0 ? new Color(Cream.r, Cream.g, Cream.b, 0.35f) : frac < 0.3f ? Match : VerdLit;
            timerFill.GetComponent<Image>().color = tc;
            if (st.red && step == Step.Dist) gaugeBg.color = Color.Lerp(new Color(0.35f, 0.06f, 0.04f, 0.96f), new Color(0.09f, 0.07f, 0.055f, 0.94f), Mathf.Clamp01(redT * 0.8f) * 0.5f);
            else gaugeBg.color = new Color(0.09f, 0.07f, 0.055f, 0.94f);
        }

        // ─────────────────────────────── IMgfGame (QA 훅) — 실제 입력과 같은 경로(Plant·Stamp·TryAddLine)를 탄다
        public void TestStart() => Begin();

        void EnsurePlaying()
        {
            if (st.phase != "playing") Begin();
            FinishRound();
            FinishBridgeReveal();
            if (st.phase != "playing") Begin();
            StopGhost();
            press = Press.None;
            if (curPin != null && !pinReady) { pinRiseT = 1f; pinReady = true; }
            if (curPlate != null && plateEnterT < 1f) { plateEnterT = 1f; curPlate.go.transform.position = curPlate.pivot; }
        }

        void TeleportPin(RP p)
        {
            stepping = false; pinPos = pinTarget = p;
            if (curPin != null) curPin.root.transform.position = PW(p, FilmY);
            if (measureOn) UpdateMeasure(false);
        }

        /// <summary>정답 경로: 1단계 = 정답 격자점에 박기 · 다리 단계 = 거리가 같은 새 격자점에 발자국(3개 뒤에는 「다음」) ·
        /// 2단계 = 알맞은 두 선을 긋고(TryAddLine — 탭과 같은 함수) 그 교점에 박기.</summary>
        public void TestAnswerCorrect()
        {
            EnsurePlaying();
            if (step == Step.Bridge)
            {
                if (!bridgeDone)
                {
                    IP best = default; int bd = int.MaxValue; bool got = false;
                    var here = pinPos.IsLattice ? pinPos.Lattice : Rules.Start;
                    foreach (var q in cur.BridgePoints())
                    {
                        bool used = false; foreach (var d in dots) if (d.Same(q)) used = true;
                        if (used) continue;
                        int dd = Mathf.Max(Mathf.Abs(q.x - here.x), Mathf.Abs(q.y - here.y));
                        if (dd < bd) { bd = dd; best = q; got = true; }
                    }
                    if (got) TeleportPin(RP.Of(best));
                }
                Plant();
                return;
            }
            if (step == Step.Build)
            {
                ClearBuild();
                bool perp = cur.m == Mission.Circum;
                TryAddLine(perp, 0); TryAddLine(perp, 1);
                if (markers.Count > 0) TeleportPin(markers[0]);
                Plant();
                return;
            }
            TeleportPin(RP.Of(cur.ans));
            Plant();
        }

        /// <summary>오답 경로: 1단계 = 출발점(정답이 될 수 없다)에 박기 · 다리 단계 = 거리가 다른 점에 찍기(목숨 차감 없음) ·
        /// 2단계 = 반대 종류 두 선을 긋고 그 교점에 박기(교점이 판 밖이면 출발점).</summary>
        public void TestAnswerWrong()
        {
            EnsurePlaying();
            if (step == Step.Bridge)
            {
                if (bridgeDone) return;
                var q = Rules.Start;
                for (int x = -Rules.GX; x <= Rules.GX && cur.BridgeOk(RP.Of(q)); x++) q = new IP(x, -Rules.GY);
                TeleportPin(RP.Of(q));
                Plant();
                return;
            }
            cur.tutorial = false;                 // 훅은 안내 명판의 무감점 재시도를 건너뛰고 실제 오답 경로를 탄다
            if (step == Step.Build)
            {
                ClearBuild();
                bool perp = cur.m != Mission.Circum;
                TryAddLine(perp, 0); TryAddLine(perp, 1);
                if (markers.Count > 0 && !markers[0].Is(cur.ans)) { TeleportPin(markers[0]); Plant(); }
                else { TeleportPin(RP.Of(Rules.Start)); Plant(true); }
                return;
            }
            TeleportPin(RP.Of(Rules.Start));      // 출발점은 정답이 될 수 없다(생성기 조건)
            Plant();
        }

        public string StateJson() => JsonUtility.ToJson(st);

        public string ProblemBankJson() => MgfJson.Bank(bank);
    }
}
