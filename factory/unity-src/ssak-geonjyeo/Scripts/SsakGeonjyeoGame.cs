// 싹 건져 — 타이틀 / 실제 두 드래그 연습 / 7조수 상태 머신 / 브리지.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.SsakGeonjyeo
{
    public partial class SsakGeonjyeoGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int capacity;
            public int[] selectedIds = new int[0];
            public int attempts;
            public int firstAttemptTotal;
            public int firstAttemptCorrect;
            public bool firstCorrect;
            public bool firstEvaluated;
            public int tide;
            public int combo;
            public int pointerVersion;
            public int remainingSec = 90;
            public string currentProblem = "";
            public string prompt = "";
            public string misconceptionId = "";
            public bool onboarding;
            public bool frozen;
            // 실제 pointer 검증용: 화면에 보이는 라벨 위치(물리 px, 왼쪽 위 원점) [등, 집게, x, y]…
            public int[] crabPx = new int[0];
            public int[] controlPx = new int[0];
            public int screenW, screenH;
        }

        enum GamePhase { Title, Practice, Playing, Reveal, End }
        enum DragMode { None, Capacity, Sweep }

        readonly State st = new State();
        readonly List<TideProblem> problems = new List<TideProblem>();
        readonly List<MgfProblem> bridgeBank = new List<MgfProblem>();
        readonly int[] crabOrder = new int[SsakRules.UniverseCount];
        readonly Vector3[] sweepPoints = new Vector3[48];

        GamePhase gamePhase = GamePhase.Title;
        DragMode dragMode = DragMode.None;
        TideProblem current;
        List<TideProblem> deck;
        System.Random rng;
        int deckIndex, selectedMask, sweepPointCount, runSerial;
        bool currentFirst, revealCorrect, revealPractice, titlePressed;
        float runLeft, revealClock, idleClock, hitStopLeft;
        string endReason = "";
        Vector3 previousSweepPoint;
        bool hasPreviousSweepPoint;
        Vector2 dragStartScreen;
        bool practiceCapacityMoved, practiceSweepMoved;
        int practiceMisses;

        void Awake()
        {
            MgfLook.Quality(34f);
            problems.AddRange(SsakRules.BuildProblems());
            bridgeBank.AddRange(SsakRules.BuildBank());
            BuildWorld();
            BuildUi();
            Prewarm();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb = new StringBuilder("싹건져경우를한번에건져라중학교2학년경우의수층리암반조수수조황금표본웅덩이건지기시작그물칸부터정하시오등번호집게번호사건합의법칙곱의법칙연습실전조수매듭점수남은시간모두건지시오정답다시시도게임종료재도전");
            sb.Append("집게번호12가지한사건경우또는각각하나씩동시에일어나지않는두사건의합그물손잡이에서시작해게를쓸고놓으시오연습웅덩이네마리밝은조개레일옆으로노란시작점시범등번호인두게를건졌다이제실전놓친경우잡은빠진");
            for (int i = 0; i < bridgeBank.Count; i++) { sb.Append(bridgeBank[i].prompt); sb.Append(bridgeBank[i].answer); sb.Append(bridgeBank[i].unitConcept); }
            MgfText.Prewarm(sb.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(0.05f, Time.deltaTime);
            HandleInput();
            if (hitStopLeft > 0f) hitStopLeft -= dt;
            else
            {
                if (gamePhase == GamePhase.Practice)
                {
                    idleClock += dt;
                    if (idleClock >= 8f) { idleClock = 0f; ReplayGuide(true); }
                }
                else if (gamePhase == GamePhase.Playing)
                {
                    runLeft = Mathf.Max(0f, runLeft - dt);
                    idleClock += dt;
                    if (idleClock >= 7f) { idleClock = 0f; ShowGuide(3.2f); }
                    if (runLeft <= 0f) EndRun("time");
                }
                else if (gamePhase == GamePhase.Reveal)
                {
                    revealClock += dt;
                    if (revealClock >= (revealCorrect ? 1.5f : 1.25f)) FinishReveal();
                }
                AnimateWorld(dt);
            }
            UpdateUi(dt);
            ApplyResponsiveLayout();
        }

        void ShowTitle()
        {
            gamePhase = GamePhase.Title;
            dragMode = DragMode.None;
            current = null;
            deck = null;
            endReason = "";
            selectedMask = 0;
            titlePressed = false;
            st.score = 0;
            st.lives = SsakRules.StartLives;
            st.level = 1;
            st.solved = 0;
            st.capacity = 0;
            st.selectedIds = new int[0];
            st.attempts = 0;
            st.firstAttemptTotal = 0;
            st.firstAttemptCorrect = 0;
            st.firstCorrect = false;
            st.firstEvaluated = false;
            st.tide = 0;
            st.combo = 0;
            st.remainingSec = 90;
            st.currentProblem = "";
            st.prompt = "";
            st.misconceptionId = "";
            st.onboarding = false;
            st.frozen = false;
            ResetCrabOrder(false);
            SetScreen();
            MgfBridge.NotifyChanged();
        }

        void StartPractice()
        {
            runSerial++;
            rng = new System.Random(unchecked(Environment.TickCount ^ runSerial * 104729));
            gamePhase = GamePhase.Practice;
            current = SsakRules.Practice();
            currentFirst = true;
            selectedMask = 0;
            st.capacity = 0;
            st.selectedIds = new int[0];
            st.score = 0;
            st.solved = 0;
            st.lives = SsakRules.StartLives;
            st.level = 1;
            st.tide = 0;
            st.attempts = 0;
            st.remainingSec = 90;
            st.firstCorrect = false;
            st.firstEvaluated = false;
            st.misconceptionId = "";
            st.onboarding = true;
            st.frozen = true;
            idleClock = 0f;
            practiceMisses = 0;
            ResetCrabOrder(true);
            BindCurrent();
            ResetWorldForProblem(true);
            SetScreen();
            ReplayGuide(false);
            MgfSfx.Play("whoosh", 0.35f);
            MgfBridge.NotifyChanged();
        }

        void StartRunDirect()
        {
            runSerial++;
            rng = new System.Random(unchecked(Environment.TickCount ^ runSerial * 130363));
            deck = SsakRules.RunDeck(rng, problems);
            deckIndex = 0;
            runLeft = SsakRules.RunSeconds;
            endReason = "";
            st.score = 0;
            st.lives = SsakRules.StartLives;
            st.level = 1;
            st.solved = 0;
            st.tide = 1;
            st.combo = 0;
            st.attempts = 0;
            st.firstAttemptTotal = 0;
            st.firstAttemptCorrect = 0;
            st.remainingSec = 90;
            st.misconceptionId = "";
            st.onboarding = false;
            st.frozen = false;
            gamePhase = GamePhase.Playing;
            LoadNextProblem();
            SetScreen();
            ShowGuide(4.2f);
            MgfBridge.NotifyChanged();
        }

        void BeginMainAfterPractice()
        {
            StartRunDirect();
            ShowToast("실전 · 그물 칸과 경우 집합을 함께 맞추시오", 3.4f);
        }

        void LoadNextProblem()
        {
            if (deck == null || deckIndex >= deck.Count) { EndRun("mastery"); return; }
            current = deck[deckIndex++];
            currentFirst = true;
            selectedMask = 0;
            st.capacity = 0;
            st.selectedIds = new int[0];
            st.firstCorrect = false;
            st.firstEvaluated = false;
            st.level = current.band;
            st.tide = st.solved + 1;
            idleClock = 0f;
            ResetCrabOrder(false);
            ShuffleCrabs();
            BindCurrent();
            ResetWorldForProblem(false);
            RefreshProblemUi();
        }

        void BindCurrent()
        {
            st.currentProblem = current == null ? "" : current.id;
            st.prompt = current == null ? "" : current.prompt;
            st.selectedIds = SsakRules.OutcomeIds(selectedMask);
        }

        void ShuffleCrabs()
        {
            for (int i = SsakRules.UniverseCount - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int t = crabOrder[i]; crabOrder[i] = crabOrder[j]; crabOrder[j] = t;
            }
        }

        void ResetCrabOrder(bool practice)
        {
            for (int i = 0; i < SsakRules.UniverseCount; i++) crabOrder[i] = i;
            SetPracticeCrabs(practice);
        }

        void SetCapacity(int value, bool notify)
        {
            int before = st.capacity;
            st.capacity = Mathf.Clamp(value, 0, 12);
            RefreshCapacityVisual(before != st.capacity);
            if (notify && before != st.capacity) MgfBridge.NotifyChanged();
        }

        void AddSelected(int index)
        {
            if (index < 0 || index >= SsakRules.UniverseCount) return;
            int bit = 1 << index;
            if ((selectedMask & bit) != 0) return;
            selectedMask |= bit;
            BindCurrent();
            CaptureCrab(index, SsakRules.CountBits(selectedMask));
            MgfSfx.Play("pop", 0.16f);
            MgfBridge.NotifyChanged();
        }

        void SubmitSweep()
        {
            if ((gamePhase != GamePhase.Practice && gamePhase != GamePhase.Playing) || current == null) return;
            revealPractice = gamePhase == GamePhase.Practice;
            revealCorrect = SsakRules.IsCorrect(current, st.capacity, selectedMask);
            st.attempts++;
            if (!revealPractice) st.remainingSec = Mathf.CeilToInt(runLeft);

            if (revealPractice)
            {
                if (revealCorrect) { st.score = 100; st.solved = 1; }
                else st.misconceptionId = SsakRules.MisconceptionId(current, st.capacity, selectedMask);
            }
            else
            {
                if (currentFirst)
                {
                    st.firstAttemptTotal++;
                    st.firstEvaluated = true;
                    st.firstCorrect = revealCorrect;
                    if (revealCorrect) st.firstAttemptCorrect++;
                    currentFirst = false;
                }
                if (revealCorrect)
                {
                    st.combo++;
                    st.solved++;
                    st.score += st.firstCorrect ? 100 + st.combo * 10 : 30;
                }
                else
                {
                    st.combo = 0;
                    st.lives = Mathf.Max(0, st.lives - 1);
                    st.misconceptionId = SsakRules.MisconceptionId(current, st.capacity, selectedMask);
                }
            }

            hitStopLeft = revealCorrect ? 0.075f : 0f;
            revealClock = 0f;
            gamePhase = GamePhase.Reveal;
            dragMode = DragMode.None;
            BindCurrent();
            BeginRevealVisual(revealCorrect);
            SetScreen();
            MgfBridge.NotifyChanged();
        }

        void FinishReveal()
        {
            if (gamePhase != GamePhase.Reveal) return;
            CompleteRevealVisual(revealCorrect);
            if (revealPractice)
            {
                if (revealCorrect) BeginMainAfterPractice();
                else
                {
                    gamePhase = GamePhase.Practice;
                    selectedMask = 0;
                    // 연습 오답은 쓸기만 다시 한다. 이미 맞게 만든 그물 2칸은 유지한다.
                    int keep = st.capacity == 2 ? 2 : 0;
                    SetCapacity(keep, false);
                    BindCurrent();
                    ResetWorldForProblem(true);
                    SetScreen();
                    practiceMisses++;
                    if (practiceMisses >= 3) PracticeDemoCatch();
                    else ReplayGuide(true);
                }
                return;
            }

            if (!revealCorrect)
            {
                if (st.lives <= 0) { EndRun("knots"); return; }
                gamePhase = GamePhase.Playing;
                selectedMask = 0;
                SetCapacity(0, false);
                BindCurrent();
                ResetWorldForProblem(false);
                ShowWrongReason(st.misconceptionId);
                SetScreen();
                MgfBridge.NotifyChanged();
                return;
            }

            if (st.solved >= SsakRules.TargetTides)
            {
                EndRun(st.firstAttemptCorrect >= SsakRules.RequiredFirstCorrect ? "clear" : "mastery");
                return;
            }
            gamePhase = GamePhase.Playing;
            LoadNextProblem();
            SetScreen();
            if (st.solved == 2) ShowConceptBridge("합의 법칙 · 겹치지 않는 두 사건은 더한다");
            else if (st.solved == 4) ShowConceptBridge("곱의 법칙 · 두 표식을 각각 고르면 곱한다");
            MgfBridge.NotifyChanged();
        }

        void EndRun(string reason)
        {
            if (gamePhase == GamePhase.End) return;
            gamePhase = GamePhase.End;
            dragMode = DragMode.None;
            endReason = reason;
            st.frozen = true;
            st.remainingSec = reason == "time" ? 0 : Mathf.CeilToInt(runLeft);
            SetScreen();
            ShowEnd(reason);
            MgfSfx.Play(reason == "clear" ? "win" : "lose", 0.55f);
            MgfBridge.NotifyChanged();
        }

        void HandleInput()
        {
            // WebGL 첫 제스처가 오디오 컨텍스트를 깨우는 프레임에는 Down이 유실돼도
            // Held/Up은 들어올 수 있다. 타이틀은 그 첫 실제 포인터 제스처를 놓치지 않는다.
            if (gamePhase == GamePhase.Title && MgfPointer.Held && !titlePressed)
            {
                titlePressed = true;
                BeginTitlePress();
            }
            // 저사양(소프트웨어 렌더링)에서는 빠른 탭의 Down과 Up이 같은 프레임에 들어온다.
            // Down 처리 뒤 함수를 끝내면 그 Up을 잃어 타이틀·손잡이가 반응하지 않았다 → Up도 같은 프레임에 처리한다.
            if (MgfPointer.Down) HandlePointerDown();
            if (MgfPointer.Held && titlePressed) UpdateTitlePress(MgfPointer.Position);
            if (MgfPointer.Up && titlePressed)
            {
                titlePressed = false;
                EndTitlePress();
                StartPractice();
                return;
            }
            if (MgfPointer.Up && gamePhase == GamePhase.Title)
            {
                EndTitlePress();
                StartPractice();
                return;
            }
            if (MgfPointer.Held && dragMode == DragMode.Capacity)
            {
                if (gamePhase == GamePhase.Practice)
                {
                    if (Vector2.Distance(dragStartScreen, MgfPointer.Position) >= 24f)
                    {
                        practiceCapacityMoved = true;
                        SetCapacity(2, true);
                    }
                }
                else UpdateCapacityFromPointer(MgfPointer.Position, true);
            }
            if (MgfPointer.Held && dragMode == DragMode.Sweep)
            {
                if (Vector2.Distance(dragStartScreen, MgfPointer.Position) >= 36f) practiceSweepMoved = true;
                UpdateSweep(MgfPointer.Position);
            }
            if (MgfPointer.Up && dragMode == DragMode.Capacity)
            {
                dragMode = DragMode.None;
                EndCapacityDrag();
                if (gamePhase == GamePhase.Practice && practiceCapacityMoved && st.capacity == 2) AdvancePracticeGuideToSweep();
                else if (gamePhase == GamePhase.Practice)
                {
                    // 실패한 첫 시도에는 검수 지시대로 손잡이를 실제 목표까지 시범 이동한다.
                    // 사용자가 한 번은 레일을 눌러야 하며 본판에는 이 보조가 없다.
                    SetCapacity(2, true);
                    AdvancePracticeGuideToSweep();
                    ShowToast("시범: 조개 손잡이가 2칸까지 이동했다 · 이제 ② 드래그", 3.0f);
                }
                return;
            }
            if (MgfPointer.Up && dragMode == DragMode.Sweep)
            {
                dragMode = DragMode.None;
                EndSweep();
                if (gamePhase == GamePhase.Playing && !practiceSweepMoved)
                {
                    // 본판에서 손잡이를 끌지 않고 톡 누른 것은 제출이 아니다(빈 그물로 매듭을 잃지 않게).
                    selectedMask = 0;
                    BindCurrent();
                    sweepTrail.gameObject.SetActive(false);
                    RefuseInput("그물 손잡이에서 게 쪽으로 끌어 건지시오");
                    return;
                }
                if (gamePhase == GamePhase.Practice && (!practiceSweepMoved || selectedMask == 0))
                {
                    selectedMask = 0;
                    BindCurrent();
                    sweepTrail.gameObject.SetActive(false);
                    practiceMisses++;
                    if (practiceMisses >= 3) { PracticeDemoCatch(); return; }
                    RefuseInput("노란 시작점에서 등번호 1인 게 쪽으로 드래그하시오");
                    return;
                }
                SubmitSweep();
            }
        }

        void HandlePointerDown()
        {
                st.pointerVersion++;
                SpawnTapRipple(MgfPointer.Position);
                MgfBridge.NotifyChanged();
                if (gamePhase == GamePhase.Title)
                {
                    titlePressed = true;
                    BeginTitlePress();
                    MgfSfx.Play("tap", 0.22f);
                    return;
                }
                if (gamePhase == GamePhase.End) { StartPractice(); return; }
                if (gamePhase == GamePhase.Reveal)
                {
                    RefuseInput("게들이 그물을 놓을 때까지 잠깐 기다리시오");
                    return;
                }
                if (gamePhase != GamePhase.Practice && gamePhase != GamePhase.Playing) return;
                idleClock = 0f;
                bool practiceControl = gamePhase == GamePhase.Practice && IsPracticeControlZone(MgfPointer.Position);
                bool playing = gamePhase == GamePhase.Playing;
                bool zone = IsPracticeControlZone(MgfPointer.Position);
                // 본판: 조개 손잡이를 먼저 판정한다. 그물 칸을 정한 뒤에도 다시 잡아 고칠 수 있어야 한다.
                bool capacityStart = (playing && (IsCapacityHandle(MgfPointer.Position) || st.capacity == 0 && zone))
                    || (gamePhase == GamePhase.Practice && st.capacity == 0 && (practiceControl || IsCapacityHandle(MgfPointer.Position)));
                bool sweepStart = !capacityStart && ((playing && st.capacity > 0 && zone) || IsSweepHandle(MgfPointer.Position)
                    || (gamePhase == GamePhase.Practice && st.capacity == 2 && practiceControl));
                if (sweepStart)
                {
                    if (st.capacity <= 0)
                    {
                        RefuseInput("그물 칸부터 정하시오");
                        ShakeCapacityHandle();
                        return;
                    }
                    dragMode = DragMode.Sweep;
                    selectedMask = 0;
                    sweepPointCount = 0;
                    hasPreviousSweepPoint = false;
                    dragStartScreen = MgfPointer.Position;
                    practiceSweepMoved = false;
                    BeginSweep();
                    UpdateSweep(MgfPointer.Position);
                    return;
                }
                if (capacityStart)
                {
                    dragMode = DragMode.Capacity;
                    dragStartScreen = MgfPointer.Position;
                    practiceCapacityMoved = false;
                    BeginCapacityDrag();
                    if (gamePhase != GamePhase.Practice) UpdateCapacityFromPointer(MgfPointer.Position, true);
                    return;
                }
                // 연습에서 엉뚱한 곳을 누른 것도 놓친 시도로 센다 — 세 번이면 게임이 시범으로 건진다.
                if (gamePhase == GamePhase.Practice && st.capacity == 2 && ++practiceMisses >= 3) { PracticeDemoCatch(); return; }
                int crab = CrabAtScreen(MgfPointer.Position);
                if (crab >= 0)
                {
                    PulseCrabLabel(crab);
                    RefuseInput("그물 손잡이에서 시작해 번호 게들을 한 번에 쓸어 건지시오");
                }
                else RefuseInput(st.capacity <= 0 ? "조개 손잡이를 끌어 그물 칸부터 정하시오" : "그물 손잡이에서 드래그를 시작하시오");
        }

        void UpdateCapacityFromPointer(Vector2 screen, bool notify)
        {
            SetCapacity(CapacityFromPointer(screen), notify);
        }

        void UpdateSweep(Vector2 screen)
        {
            if (!ScreenToWater(screen, out Vector3 p)) return;
            if (sweepPointCount < sweepPoints.Length) sweepPoints[sweepPointCount++] = p;
            else
            {
                for (int i = 1; i < sweepPoints.Length; i++) sweepPoints[i - 1] = sweepPoints[i];
                sweepPoints[sweepPoints.Length - 1] = p;
            }
            UpdateSweepTrail(sweepPoints, sweepPointCount);
            if (!hasPreviousSweepPoint)
            {
                previousSweepPoint = p;
                hasPreviousSweepPoint = true;
                return;
            }
            for (int i = 0; i < SsakRules.UniverseCount; i++)
            {
                if (!IsCrabActive(i)) continue;
                Vector3 c = CrabHitCenter(i);
                // 연습 웅덩이는 게가 네 마리뿐이라 판정 반경을 넓힌다. 본판은 원래 반경.
                if (DistancePointSegmentXZ(c, previousSweepPoint, p) <= (practiceLayout ? 0.82f : 0.58f)) AddSelected(i);
            }
            previousSweepPoint = p;
        }

        static float DistancePointSegmentXZ(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector2 pp = new Vector2(p.x, p.z), aa = new Vector2(a.x, a.z), bb = new Vector2(b.x, b.z);
            Vector2 ab = bb - aa;
            float d = ab.sqrMagnitude;
            if (d < 0.0001f) return Vector2.Distance(pp, aa);
            float t = Mathf.Clamp01(Vector2.Dot(pp - aa, ab) / d);
            return Vector2.Distance(pp, aa + ab * t);
        }

        // 연습(채점 없음)에서 세 번 놓치면 게임이 직접 시범으로 건진다. 본판에는 이 보조가 없다.
        void PracticeDemoCatch()
        {
            if (gamePhase != GamePhase.Practice || current == null) return;
            SetCapacity(2, true);
            selectedMask = current.answerMask;
            BindCurrent();
            for (int i = 0; i < SsakRules.UniverseCount; i++)
                if ((selectedMask & (1 << i)) != 0) { previousSweepPoint = CrabHitCenter(i); CaptureCrab(i, 0); }
            SubmitSweep();
            ShowToast("시범: (1,1), (1,2) 두 게를 건졌다 · 이제 실전", 2.6f);
        }

        void FinishPendingReveal()
        {
            if (gamePhase == GamePhase.Reveal) { revealClock = 99f; FinishReveal(); }
        }

        // IMgfGame — QA 강제 훅도 실제 SubmitSweep 판정 경로를 공유한다.
        public void TestStart() { FinishPendingReveal(); StartRunDirect(); }

        public void TestAnswerCorrect()
        {
            FinishPendingReveal();
            if (gamePhase != GamePhase.Playing || current == null) StartRunDirect();
            SetCapacity(current.answerCount, false);
            selectedMask = current.answerMask;
            BindCurrent();
            SubmitSweep();
        }

        public void TestAnswerWrong()
        {
            FinishPendingReveal();
            if (gamePhase != GamePhase.Playing || current == null) StartRunDirect();
            SetCapacity(current.answerCount, false);
            selectedMask = (1 << current.answerCount) - 1;
            if (selectedMask == current.answerMask) selectedMask ^= 3;
            BindCurrent();
            SubmitSweep();
        }

        public string StateJson()
        {
            st.phase = gamePhase == GamePhase.Title ? "title" : gamePhase == GamePhase.End ? (endReason == "clear" ? "clear" : "gameover") : "playing";
            st.onboarding = gamePhase == GamePhase.Practice;
            st.frozen = gamePhase == GamePhase.Practice || gamePhase == GamePhase.Reveal || gamePhase == GamePhase.End;
            BindCurrent();
            FillScreenProbe();
            return JsonUtility.ToJson(st);
        }

        void FillScreenProbe()
        {
            st.screenW = Screen.width; st.screenH = Screen.height;
            int n = 0;
            for (int i = 0; i < SsakRules.UniverseCount; i++) if (IsCrabActive(i)) n++;
            if (st.crabPx.Length != n * 4) st.crabPx = new int[n * 4];
            int w = 0;
            for (int i = 0; i < SsakRules.UniverseCount; i++)
            {
                if (!IsCrabActive(i)) continue;
                Vector3 sp = cam.WorldToScreenPoint(CrabHitCenter(i));
                st.crabPx[w++] = SsakRules.BackOfIndex(i); st.crabPx[w++] = SsakRules.ClawOfIndex(i);
                st.crabPx[w++] = Mathf.RoundToInt(sp.x); st.crabPx[w++] = Mathf.RoundToInt(Screen.height - sp.y);
            }
            if (st.controlPx.Length != 8) st.controlPx = new int[8];
            Vector3 a = cam.WorldToScreenPoint(RailStart), b = cam.WorldToScreenPoint(RailEnd);
            Vector3 h = cam.WorldToScreenPoint(capacityHandle.transform.position), sw = cam.WorldToScreenPoint(SweepHome);
            st.controlPx[0] = Mathf.RoundToInt(a.x); st.controlPx[1] = Mathf.RoundToInt(Screen.height - a.y);
            st.controlPx[2] = Mathf.RoundToInt(b.x); st.controlPx[3] = Mathf.RoundToInt(Screen.height - b.y);
            st.controlPx[4] = Mathf.RoundToInt(h.x); st.controlPx[5] = Mathf.RoundToInt(Screen.height - h.y);
            st.controlPx[6] = Mathf.RoundToInt(sw.x); st.controlPx[7] = Mathf.RoundToInt(Screen.height - sw.y);
        }

        public string ProblemBankJson() => MgfJson.Bank(bridgeBank);
    }
}
