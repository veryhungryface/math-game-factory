using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.RouteRecord
{
    // 루트 레코드 — 회전하는 음반의 노치에 브리지를 끼워 서로 다른 소리 경로의 수를 만들고,
    // 톤암을 내려 학생이 만든 그래프의 모든 경로를 펄스로 시험한다(합의 법칙·곱의 법칙).
    // 학생이 놓고, 코드는 판정만 한다: 판정은 링별 브리지 개수로 정수 계산(RouteProblem.PathCount).
    public partial class RouteRecordGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int needles = RouteRecordRules.StartNeedles;
            public int firstCorrect;
            public int firstAttempted;
            public int carryBridges;
            public int bridgesUsed;
            public int budget;
            public int target;
            public int pathCountAfterSubmit;
            public int attempts;
            public string currentProblemId = "";
            public string structure = "";
            public string placedBridgeIds = "";
            public string misconceptionId = "";
            public string maxima = "";
            public string inputTargets = "";   // 뷰포트 좌표(0~1, 위가 0) — 포인터 봇·QA 용. 입력·화면 크기 변화 때만 바뀐다
            public bool tonearmLocked;
            public bool onboarding;
        }

        enum RunPhase { Title, Intro, Practice, Playing, Testing, End }

        readonly State st = new State();
        readonly List<RouteProblem> catalog = new List<RouteProblem>();
        readonly bool[,] placed = new bool[3, RouteRecordRules.MaxSlots];
        readonly int[] counts = new int[3];
        readonly RecordLayout layout = new RecordLayout();
        RunPhase phase = RunPhase.Title;
        RouteProblem current;
        System.Random rng = new System.Random(20261015);
        int runSerial;
        int currentBudget;
        bool firstAttempt;
        bool practiceDone;
        bool lastCorrect;
        bool lastPractice;
        bool pendingEnd;
        bool pendingClear;
        float activeClock;
        float idleGuide;
        float introClock;
        string endReason = "";

        // 입력 상태
        enum Drag { None, NewBridge, MoveBridge, Tonearm, TapSlot }
        Drag drag = Drag.None;
        int dragGroup = -1, dragSlot = -1;
        Vector2 downPos;

        void Awake()
        {
            MgfLook.Quality(50f);
            catalog.AddRange(RouteRecordRules.BuildCatalog());
            Prewarm();
            BuildWorld();
            BuildUi();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var s = new StringBuilder("루트레코드홈을이어트랙수를맞춰라중학교2학년경우의수최고최초정확톤암을내려재생끌어서노치에끼우기남은브리지이월바늘재킷목표가지실제경로없다빛나는음반쪽으로시험하시오본판시작연습완성다시닳았다끝났다활성시간부족넘침면링하나씩곱한다더한다만나지않는다둘중한면새벽전파");
            s.Append(RouteRecordRules.Practice().prompt);
            for (int i = 0; i < catalog.Count; i++) { s.Append(catalog[i].prompt); s.Append(catalog[i].reveal); }
            s.Append(RouteRecordRules.MisconceptionLine("or-sides-multiplied")).Append(RouteRecordRules.MisconceptionLine("serial-rings-added"))
             .Append(RouteRecordRules.MisconceptionLine("bridge-count-equals-path-count")).Append(RouteRecordRules.MisconceptionLine(""));
            MgfText.Prewarm(s.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(.05f, Time.deltaTime);
            HandleInput();

            if (phase == RunPhase.Intro)
            {
                introClock += dt;
                if (introClock >= 1.15f) { if (practiceDone) BeginMain(); else BeginPractice(); }
            }
            else if (phase == RunPhase.Practice)
            {
                idleGuide += dt;
            }
            else if (phase == RunPhase.Playing)
            {
                activeClock += dt;
                idleGuide += dt;
                if (activeClock >= RouteRecordRules.SessionSeconds) EndRun(false, "활성 시간 110초가 끝났다");
            }

            UpdateWorld(dt);
            UpdateUi(dt);
        }

        // ───────────────────────── 흐름 ─────────────────────────

        void ShowTitle()
        {
            phase = RunPhase.Title;
            current = RouteRecordRules.Practice();
            layout.Build(current);
            ResetStats();
            st.phase = "title"; st.onboarding = false;
            ClearBridges();
            ApplyProblemVisuals(true);
            SetScreen();
            MgfBridge.NotifyChanged();
        }

        void ResetStats()
        {
            st.score = 0; st.lives = st.needles = RouteRecordRules.StartNeedles; st.level = 1; st.solved = 0;
            st.firstCorrect = 0; st.firstAttempted = 0; st.carryBridges = 0; st.bridgesUsed = 0; st.attempts = 0;
            st.pathCountAfterSubmit = 0; st.misconceptionId = ""; st.tonearmLocked = false;
            activeClock = 0; idleGuide = 0; endReason = ""; pendingEnd = false; pendingClear = false; firstAttempt = true;
        }

        void StartFromTitle()
        {
            phase = RunPhase.Intro; introClock = 0;
            st.phase = "playing";
            BeginIntroVisuals();
            SetScreen();
            MgfBridge.NotifyChanged();
        }

        void NewRun()
        {
            runSerial++;
            rng = new System.Random(unchecked(Environment.TickCount * 31 + runSerial * 7919));
            ResetStats();
            EndTestVisuals();
            ResetWorldForRun();
            drag = Drag.None; armHeld = false; ShowDragBridge(false);
        }

        void BeginPractice()
        {
            NewRun();
            phase = RunPhase.Practice;
            st.phase = "playing"; st.onboarding = true;
            LoadProblem(RouteRecordRules.Practice(), 0);
            SetScreen();
            MgfBridge.NotifyChanged();
        }

        void BeginMain()
        {
            NewRun();
            practiceDone = true;
            phase = RunPhase.Playing;
            st.phase = "playing"; st.onboarding = false;
            LoadNextProblem();
            SetScreen();
            MgfBridge.NotifyChanged();
        }

        void LoadNextProblem()
        {
            var p = RouteRecordRules.Pick(catalog, st.solved, rng, current != null ? current.id : "");
            LoadProblem(p, st.carryBridges);
        }

        void LoadProblem(RouteProblem p, int carry)
        {
            current = p;
            layout.Build(p);
            currentBudget = p.budget + carry;
            st.level = Mathf.Min(4, st.solved < 2 ? 1 : st.solved == 2 ? 2 : st.solved == 3 ? 3 : 4);
            st.currentProblemId = p.id; st.target = p.target; st.budget = currentBudget; st.structure = p.kind.ToString();
            st.maxima = p.groupCount == 3 ? p.maxima[0] + "-" + p.maxima[1] + "-" + p.maxima[2] : p.maxima[0] + "-" + p.maxima[1];
            targetsDirty = true;
            st.pathCountAfterSubmit = 0; st.misconceptionId = ""; st.tonearmLocked = false;
            firstAttempt = true; idleGuide = 0;
            ClearBridges();
            ApplyProblemVisuals(false);
            MgfBridge.NotifyChanged();
        }

        void EndRun(bool clear, string reason)
        {
            if (phase == RunPhase.End) return;
            phase = RunPhase.End; endReason = reason; drag = Drag.None;
            st.phase = clear ? "clear" : "gameover"; st.tonearmLocked = true;
            ShowDragBridge(false);
            SetScreen(); EndSequence(clear);
            MgfBridge.NotifyChanged();
        }

        bool Editable => phase == RunPhase.Practice || phase == RunPhase.Playing;

        // ───────────────────────── 입력 ─────────────────────────

        void HandleInput()
        {
            Vector2 pos = MgfPointer.Position;
            if (MgfPointer.Down)
            {
                SpawnRipple(pos);
                downPos = pos;
                OnPointerDown(pos);
            }
            else if (MgfPointer.Held && drag != Drag.None)
            {
                OnPointerHeld(pos);
            }
            if (MgfPointer.Up && drag != Drag.None) OnPointerUp(pos);
        }

        void OnPointerDown(Vector2 pos)
        {
            if (phase == RunPhase.Title)
            {
                if (HitTitleStart(pos)) StartFromTitle();
                else { Refuse("빛나는 톤암을 눌러 음반 위로 내리시오."); PointAtTonearm(); }
                return;
            }
            if (phase == RunPhase.Intro) return;
            if (phase == RunPhase.End)
            {
                if (HitRestart(pos)) StartFromTitle();
                else { Refuse("'다시 재생' 라벨을 누르시오."); PulseRestart(); }
                return;
            }
            if (phase == RunPhase.Testing) { Refuse("펄스가 실제 경로를 세는 중이다."); return; }
            if (!Editable) return;

            if (HitTonearm(pos))
            {
                drag = Drag.Tonearm; GrabTonearm(); return;
            }
            if (HitBin(pos))
            {
                if (st.bridgesUsed >= currentBudget)
                {
                    Refuse("남은 브리지가 없다. 놓인 브리지를 음반 밖으로 끌어 회수하시오."); RefuseBin(); return;
                }
                drag = Drag.NewBridge; dragGroup = -1; dragSlot = -1;
                ShowDragBridge(true); MoveDragBridge(pos); LiftFromBin();
                MgfSfx.Play("tap", .14f);
                return;
            }
            int g, j;
            if (SlotAt(pos, out g, out j))
            {
                dragGroup = g; dragSlot = j;
                if (placed[g, j]) { drag = Drag.MoveBridge; PickBridge(g, j); }
                else drag = Drag.TapSlot;
                return;
            }
            if (OnRecord(pos)) { Refuse("작은 홈(노치)을 누르면 브리지가 끼워진다."); FlashNearestSlot(pos); }
            else { Refuse("브리지는 음반의 노치에 끼운다."); PointAtBin(); }
        }

        void OnPointerHeld(Vector2 pos)
        {
            switch (drag)
            {
                case Drag.NewBridge: MoveDragBridge(pos); break;
                case Drag.MoveBridge:
                    if ((pos - downPos).magnitude > 10f * UiScale) { ShowDragBridge(true); HideBridge(dragGroup, dragSlot); MoveDragBridge(pos); }
                    break;
                case Drag.Tonearm: PullTonearm(pos); break;
            }
        }

        void OnPointerUp(Vector2 pos)
        {
            var d = drag; drag = Drag.None;
            int g, j;
            switch (d)
            {
                case Drag.TapSlot:
                    if (!Editable) return;
                    if (SlotAt(pos, out g, out j) && g == dragGroup && j == dragSlot) PlaceBridge(g, j, true);
                    break;
                case Drag.NewBridge:
                    ShowDragBridge(false);
                    if (SlotAt(pos, out g, out j))
                    {
                        if (!placed[g, j]) PlaceBridge(g, j, false);
                        else { Refuse("그 노치에는 이미 브리지가 있다."); WiggleBridge(g, j); ReturnToBin(); }
                    }
                    else { Refuse("노치가 아닌 곳이다. 브리지가 통으로 돌아간다."); FlashNearestSlot(pos); ReturnToBin(); }
                    break;
                case Drag.MoveBridge:
                    ShowDragBridge(false);
                    if ((pos - downPos).magnitude <= 10f * UiScale)
                    {
                        // 제자리 탭 = 빼기
                        RemoveBridge(dragGroup, dragSlot);
                    }
                    else if (SlotAt(pos, out g, out j))
                    {
                        if (g == dragGroup && j == dragSlot) { ShowBridgeAgain(g, j); }
                        else if (!placed[g, j]) { placed[dragGroup, dragSlot] = false; PlaceBridge(g, j, false); }
                        else { Refuse("그 노치에는 이미 브리지가 있다."); ShowBridgeAgain(dragGroup, dragSlot); WiggleBridge(g, j); }
                    }
                    else RemoveBridge(dragGroup, dragSlot);
                    break;
                case Drag.Tonearm:
                    if (ReleaseTonearm()) Submit();
                    else { Refuse("톤암을 음반 쪽으로 더 끌어 내려야 시험한다."); }
                    break;
            }
        }

        void PlaceBridge(int g, int j, bool viaTap)
        {
            if (placed[g, j]) return;
            if (st.bridgesUsed >= currentBudget)
            {
                Refuse("남은 브리지가 없다. 놓인 브리지를 눌러 빼시오."); RefuseBin(); WiggleSocket(g, j); return;
            }
            placed[g, j] = true;
            Recount();
            BridgeSnapped(g, j, viaTap);
            idleGuide = 0;
            MgfBridge.NotifyChanged();
        }

        void RemoveBridge(int g, int j)
        {
            if (!placed[g, j]) return;
            placed[g, j] = false;
            Recount();
            BridgeReturned(g, j);
            MgfBridge.NotifyChanged();
        }

        void Recount()
        {
            Array.Clear(counts, 0, counts.Length);
            var sb = new StringBuilder(32);
            int used = 0;
            for (int g = 0; g < current.groupCount; g++)
            for (int j = 0; j < current.maxima[g]; j++)
            {
                if (!placed[g, j]) continue;
                counts[g]++; used++;
                if (sb.Length > 0) sb.Append(',');
                sb.Append((char)('A' + g)).Append(j + 1);
            }
            st.bridgesUsed = used; st.placedBridgeIds = sb.ToString();
            RefreshBridges();
        }

        void ClearBridges()
        {
            for (int g = 0; g < 3; g++) for (int j = 0; j < RouteRecordRules.MaxSlots; j++) placed[g, j] = false;
            Array.Clear(counts, 0, counts.Length);
            st.bridgesUsed = 0; st.placedBridgeIds = "";
            RefreshBridges();
        }

        // ───────────────────────── 제출·판정 ─────────────────────────

        void Submit()
        {
            if (!Editable) return;
            if (st.bridgesUsed == 0) { Refuse("경로가 없다. 브리지를 먼저 노치에 끼우시오."); EmptyRecord(); return; }
            int paths = current.PathCount(counts);
            bool correct = paths == current.target;
            Resolve(correct, paths, correct ? "" : RouteRecordRules.Diagnose(current, counts));
        }

        void Resolve(bool correct, int paths, string misconception)
        {
            bool practice = phase == RunPhase.Practice;
            lastCorrect = correct; lastPractice = practice;
            st.pathCountAfterSubmit = paths; st.tonearmLocked = true; st.attempts++;
            st.misconceptionId = misconception;
            if (!practice && firstAttempt)
            {
                st.firstAttempted++;
                if (correct) st.firstCorrect++;
            }
            if (correct)
            {
                if (!practice)
                {
                    int spare = Mathf.Max(0, currentBudget - st.bridgesUsed);
                    st.score += 100 + (firstAttempt ? 50 : 0) + spare * 20;
                    st.carryBridges = Mathf.Min(2, spare);
                    st.solved++;
                    if (st.solved >= RouteRecordRules.Goal)
                    {
                        pendingEnd = true; pendingClear = st.firstCorrect >= 4;
                        st.phase = pendingClear ? "clear" : "gameover";
                    }
                }
            }
            else if (!practice)
            {
                st.lives = --st.needles;
                if (st.needles <= 0) { pendingEnd = true; pendingClear = false; st.phase = "gameover"; }
            }
            firstAttempt = false;
            phase = RunPhase.Testing;
            BeginTest(correct, paths, misconception, practice);
            MgfBridge.NotifyChanged();
        }

        // 펄스 시험 연출이 끝나면 View 가 부른다.
        void FinishTest()
        {
            if (phase != RunPhase.Testing) return;
            st.tonearmLocked = false;
            EndTestVisuals();
            if (pendingEnd)
            {
                if (st.needles <= 0) EndRun(false, "바늘 끝 3개가 모두 닳았다");
                else if (pendingClear) EndRun(true, "6장의 코드가 모두 완성됐다");
                else EndRun(false, "6장을 완성했지만 최초 정확이 4/6에 못 미쳤다");
                return;
            }
            if (lastCorrect)
            {
                if (lastPractice) { practiceDone = true; BeginMain(); }
                else { phase = RunPhase.Playing; LoadNextProblem(); }
            }
            else
            {
                phase = lastPractice ? RunPhase.Practice : RunPhase.Playing;
                st.phase = "playing";
                CorrectionState();
            }
            SetScreen();
            MgfBridge.NotifyChanged();
        }

        // ───────────────────────── QA 훅 ─────────────────────────

        public void TestStart()
        {
            practiceDone = true;
            BeginMain();
        }

        public void TestAnswerCorrect()
        {
            EnsureTestPlayable();
            if (!Editable) return;
            SetLayoutCounts(current.validLayouts[0]);
            Submit();
        }

        public void TestAnswerWrong()
        {
            EnsureTestPlayable();
            if (!Editable) return;
            // 첫 링에 브리지 1개만: 어느 구조든 목표(≥4)와 다르다(직렬 1×0=0, 합 1, 혼합 0 또는 1).
            ClearBridges();
            placed[0, 0] = true; Recount();
            Submit();
        }

        void EnsureTestPlayable()
        {
            if (phase == RunPhase.Testing) FinishTest();
            if (phase == RunPhase.Title || phase == RunPhase.End || phase == RunPhase.Intro) { practiceDone = true; BeginMain(); }
        }

        void SetLayoutCounts(string layoutId)
        {
            ClearBridges();
            string[] parts = layoutId.Split('-');
            for (int g = 0; g < parts.Length && g < current.groupCount; g++)
            {
                int n;
                if (!int.TryParse(parts[g], out n)) continue;
                for (int j = 0; j < n && j < current.maxima[g]; j++) placed[g, j] = true;
            }
            Recount();
        }

        public string StateJson() { return JsonUtility.ToJson(st); }
        public string ProblemBankJson() { return RouteRecordRules.BankJson(catalog); }
    }
}
