// 구슬 새총 — 실제 쓸기·잎 주머니 당김·첫 제출 숙련 게이트 상태 머신.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.GuseulSaechong
{
    public partial class GuseulSaechongGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int N;
            public int[] colors = new int[GuseulRules.MaxMarbles]; // 1=보라, 0=상아
            public string eventType = "one";
            public string targetColor = "보라";
            public string targetFraction = "1/3";
            public string submittedFraction = "";
            public int firstAttemptCorrect;
            public int firstAttempts;
            public int[] attemptsByProblem = new int[GuseulRules.ProblemCount];
            public int trialCount;
            public int eventCount;
            public bool onboarding;
            public bool frozen;
            public bool submissionLatched;
            public int pointerVersion;
            public string currentProblem = "";
            public string prompt = "";
            public string misconceptionId = "";
            public string lastAction = "boot";
            public int[] marblePx = new int[GuseulRules.MaxMarbles * 2]; // CSS 좌표, 왼쪽 위 원점
            public int[] pouchPx = new int[4]; // 시작 x,y / 아래 당김 목표 x,y
            public int[] restartPx = new int[2];
            public int screenW;
            public int screenH;
        }

        enum GamePhase { Title, Practice, Playing, Reveal, End }
        enum Gesture { None, Paint, Pull }

        readonly State st = new State();
        readonly bool[] purple = new bool[GuseulRules.MaxMarbles];
        readonly bool[] paintedThisStroke = new bool[GuseulRules.MaxMarbles];
        readonly List<MgfProblem> bank = new List<MgfProblem>();
        readonly List<MarbleProblem> deck = new List<MarbleProblem>();

        GamePhase phase = GamePhase.Title;
        Gesture gesture;
        MarbleProblem current;
        System.Random targetRng;
        System.Random initialRng;
        System.Random experimentRng;
        int orderIndex;
        int runSerial;
        int previousN;
        int lastWrongPurple = -1;
        int selectedMarble;
        int scoreBeforeAnimation;
        int lastEvent8;
        int lastEvent24;
        int lastEvent72;
        float remainingTime;
        float revealClock;
        float hitStopClock;
        float idleClock;
        float pullPixels;
        float lastStatePull;
        Vector2 pullStart;
        bool strokeColor;
        bool revealCorrect;
        bool revealPractice;
        bool fastTestMode;
        string endReason = "";

        void Awake()
        {
            MgfLook.Quality(34f);
            // CreatePrimitive의 콜라이더 타입이 IL2CPP 스트리핑되지 않게 명시적으로 참조한다.
            var keep = new GameObject("Primitive collider type keeper");
            keep.AddComponent<BoxCollider>();
            keep.AddComponent<SphereCollider>();
            keep.AddComponent<CapsuleCollider>();
            keep.AddComponent<MeshCollider>();
            Destroy(keep);

            GuseulRules.ValidateAll();
            bank.AddRange(GuseulRules.BuildBank());
            BuildWorld();
            BuildUi();
            Prewarm();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb = new StringBuilder("구슬새총구슬을섞어쏴라연못깨우기중학교2학년확률최고기록");
            sb.Append("구슬을배합해확률을맞추면잎길이열린다보라상아전체안전잎남은시간첫제출완료");
            sb.Append("한개를임의로꺼낼때나오지않을두번모두적어도한번확인한후다시넣고모양과크기는모두같다");
            sb.Append("조금더당긴뒤놓으시오구슬을바꾼뒤다시놓으시오배합완료연못길완성학습목표미달다시도전");
            for (int i = 0; i < bank.Count; i++)
            {
                sb.Append(bank[i].prompt);
                sb.Append(bank[i].answer);
                sb.Append(bank[i].unitConcept);
            }
            MgfText.Prewarm(sb.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(.05f, Time.deltaTime);
            HandleInput();
            HandleKeyboard();

            if (hitStopClock > 0f) hitStopClock -= dt;
            else
            {
                if (phase == GamePhase.Practice)
                {
                    idleClock += dt;
                    if (idleClock >= 8f) { idleClock = 0f; ReplayGuide(true); }
                }
                else if (phase == GamePhase.Playing)
                {
                    remainingTime = Mathf.Max(0f, remainingTime - dt);
                    idleClock += dt;
                    if (idleClock >= 9f) { idleClock = 0f; ReplayGuide(true); }
                    if (remainingTime <= 0f) EndRun("time");
                }
                else if (phase == GamePhase.Reveal)
                {
                    revealClock += dt;
                    float duration = fastTestMode ? .035f : (revealCorrect ? 2.2f : 1.15f);
                    if (revealClock >= duration) FinishReveal();
                }
            }

            AnimateWorld(dt);
            UpdateUi(dt);
            ApplyResponsiveLayout();
        }

        void ShowTitle()
        {
            phase = GamePhase.Title;
            gesture = Gesture.None;
            current = null;
            deck.Clear();
            orderIndex = 0;
            previousN = 0;
            lastWrongPurple = -1;
            endReason = "";
            fastTestMode = false;
            st.score = 0;
            st.lives = GuseulRules.StartLives;
            st.level = 1;
            st.solved = 0;
            st.N = 0;
            st.firstAttemptCorrect = 0;
            st.firstAttempts = 0;
            Array.Clear(st.attemptsByProblem, 0, st.attemptsByProblem.Length);
            st.trialCount = 0;
            st.eventCount = 0;
            st.onboarding = false;
            st.frozen = true;
            st.submissionLatched = false;
            st.currentProblem = "";
            st.prompt = "";
            st.misconceptionId = "";
            st.lastAction = "title";
            remainingTime = 120f;
            scoreBeforeAnimation = 0;
            SetCount(3, 6, false);
            SetPhaseUi();
            MgfBridge.NotifyChanged();
        }

        void StartPractice()
        {
            InitialiseRunRandoms();
            phase = GamePhase.Practice;
            current = GuseulRules.Practice();
            orderIndex = 0;
            previousN = current.N;
            remainingTime = 120f;
            idleClock = 0f;
            lastWrongPurple = -1;
            endReason = "";
            st.score = 0;
            st.lives = GuseulRules.StartLives;
            st.level = 0;
            st.solved = 0;
            st.firstAttemptCorrect = 0;
            st.firstAttempts = 0;
            Array.Clear(st.attemptsByProblem, 0, st.attemptsByProblem.Length);
            st.trialCount = 0;
            st.eventCount = 0;
            st.onboarding = true;
            st.frozen = true;
            st.submissionLatched = false;
            st.misconceptionId = "";
            st.lastAction = "practice-start";
            SetCount(0, current.N, false);
            BindCurrent();
            ResetWorldForProblem(true);
            SetPhaseUi();
            ReplayGuide(false);
            MgfSfx.Play("whoosh", .35f);
            MgfBridge.NotifyChanged();
        }

        void InitialiseRunRandoms()
        {
            runSerial++;
            int seed = unchecked(Environment.TickCount ^ runSerial * 104729);
            targetRng = new System.Random(seed);
            initialRng = new System.Random(seed ^ unchecked((int)0x6d2b79f5));
            experimentRng = new System.Random(seed ^ unchecked((int)0x9e3779b9));
        }

        void StartRunDirect(bool preservePractice, bool fast)
        {
            if (!preservePractice) InitialiseRunRandoms();
            fastTestMode = fast;
            phase = GamePhase.Playing;
            gesture = Gesture.None;
            orderIndex = 0;
            remainingTime = 120f;
            idleClock = 0f;
            lastWrongPurple = -1;
            endReason = "";
            st.score = 0;
            st.lives = GuseulRules.StartLives;
            st.level = 1;
            st.solved = 0;
            st.firstAttemptCorrect = 0;
            st.firstAttempts = 0;
            Array.Clear(st.attemptsByProblem, 0, st.attemptsByProblem.Length);
            st.trialCount = 0;
            st.eventCount = 0;
            st.onboarding = false;
            st.frozen = false;
            st.submissionLatched = false;
            st.misconceptionId = "";
            st.lastAction = "run-start";
            if (!preservePractice)
            {
                previousN = 6;
                SetRandomColors(previousN);
            }
            deck.Clear();
            deck.AddRange(GuseulRules.BuildRunDeck(targetRng, initialRng, purple, previousN));
            LoadProblem();
            SetPhaseUi();
            MgfBridge.NotifyChanged();
        }

        void LoadProblem()
        {
            if (orderIndex >= deck.Count)
            {
                EndRun(st.firstAttemptCorrect >= GuseulRules.RequiredFirstCorrect ? "clear" : "mastery");
                return;
            }
            current = deck[orderIndex];
            ResizePreservingColors(current.N);
            lastWrongPurple = -1;
            st.submissionLatched = false;
            st.misconceptionId = "";
            st.submittedFraction = "";
            st.level = current.band;
            st.lastAction = "problem-" + (orderIndex + 1);
            BindCurrent();
            ResetWorldForProblem(false);
            RefreshProblemUi();
            MgfBridge.NotifyChanged();
        }

        void BindCurrent()
        {
            st.N = current.N;
            st.eventType = GuseulRules.EventCode(current.eventType);
            st.targetColor = current.TargetColor;
            st.targetFraction = new IntFraction(current.targetN, current.targetD).Display;
            st.currentProblem = current.id;
            st.prompt = current.prompt;
            SyncColorsToState();
        }

        void ResizePreservingColors(int total)
        {
            int old = previousN;
            for (int i = old; i < total; i++) purple[i] = initialRng.Next(2) == 0;
            for (int i = total; i < purple.Length; i++) purple[i] = false;
            previousN = total;
            SyncColorsToState();
        }

        void SetRandomColors(int total)
        {
            for (int i = 0; i < purple.Length; i++) purple[i] = i < total && initialRng.Next(2) == 0;
            SyncColorsToState();
        }

        void SetCount(int wantedPurple, int total, bool animate)
        {
            wantedPurple = Mathf.Clamp(wantedPurple, 0, total);
            for (int i = 0; i < purple.Length; i++) purple[i] = i < total && i < wantedPurple;
            previousN = total;
            SyncColorsToState();
            RefreshMarbles(animate);
        }

        void SyncColorsToState()
        {
            for (int i = 0; i < st.colors.Length; i++) st.colors[i] = purple[i] ? 1 : 0;
        }

        int CountPurple()
        {
            int count = 0;
            int total = current != null ? current.N : Math.Min(previousN, purple.Length);
            for (int i = 0; i < total; i++) if (purple[i]) count++;
            return count;
        }

        void HandleInput()
        {
            if (MgfPointer.Down)
            {
                st.pointerVersion++;
                idleClock = 0f;
                Vector2 p = MgfPointer.Position;
                if (phase == GamePhase.Title)
                {
                    TriggerRipple(p, true);
                    StartPractice();
                    return;
                }
                if (phase == GamePhase.End)
                {
                    TriggerRipple(p, true);
                    StartPractice();
                    return;
                }
                if (phase == GamePhase.Reveal)
                {
                    TriggerRipple(p, false);
                    Refuse("구슬을 돌려놓는 중");
                    MgfBridge.NotifyChanged();
                    return;
                }
                if (phase != GamePhase.Practice && phase != GamePhase.Playing) return;

                int hit = HitMarble(p);
                if (hit >= 0)
                {
                    gesture = Gesture.Paint;
                    Array.Clear(paintedThisStroke, 0, paintedThisStroke.Length);
                    strokeColor = !purple[hit];
                    PaintMarble(hit);
                    st.lastAction = "paint-start";
                    MgfSfx.Play("tap", .24f);
                    MgfBridge.NotifyChanged();
                }
                else if (InPouch(p))
                {
                    gesture = Gesture.Pull;
                    pullStart = p;
                    pullPixels = 0f;
                    lastStatePull = 0f;
                    st.lastAction = "pull-start";
                    SetPullVisual(0f);
                    MgfBridge.NotifyChanged();
                }
                else
                {
                    gesture = Gesture.None;
                    TriggerRipple(p, false);
                    Refuse("구슬을 칠하거나 잎 주머니를 당기시오");
                    st.lastAction = "empty-ripple";
                    MgfBridge.NotifyChanged();
                }
            }

            if (MgfPointer.Held)
            {
                if (gesture == Gesture.Paint)
                {
                    int hit = HitMarble(MgfPointer.Position);
                    if (hit >= 0) PaintMarble(hit);
                }
                else if (gesture == Gesture.Pull)
                {
                    pullPixels = Mathf.Max(0f, pullStart.y - MgfPointer.Position.y);
                    float t = Mathf.Clamp01(pullPixels / Mathf.Max(52f, Screen.height * .105f));
                    SetPullVisual(t);
                    if (Mathf.Abs(t - lastStatePull) > .18f)
                    {
                        lastStatePull = t;
                        st.pointerVersion++;
                        st.lastAction = "pulling";
                        MgfBridge.NotifyChanged();
                    }
                }
            }

            if (MgfPointer.Up)
            {
                if (gesture == Gesture.Pull)
                {
                    float required = Mathf.Max(44f, Screen.height * .052f);
                    if (pullPixels >= required) SubmitMix();
                    else
                    {
                        Refuse("조금 더 당긴 뒤 놓으시오");
                        st.lastAction = "short-pull";
                        MgfSfx.Play("wrong", .20f);
                        SetPullVisual(0f);
                        MgfBridge.NotifyChanged();
                    }
                }
                else if (gesture == Gesture.Paint)
                {
                    st.lastAction = "paint-end";
                    MgfBridge.NotifyChanged();
                }
                gesture = Gesture.None;
            }
        }

        void PaintMarble(int index)
        {
            if (index < 0 || current == null || index >= current.N || paintedThisStroke[index]) return;
            paintedThisStroke[index] = true;
            purple[index] = strokeColor;
            if (lastWrongPurple >= 0 && CountPurple() != lastWrongPurple) st.submissionLatched = false;
            SyncColorsToState();
            PulseMarble(index);
            RefreshMarbles(false);
            st.pointerVersion++;
        }

        void HandleKeyboard()
        {
            if (phase != GamePhase.Practice && phase != GamePhase.Playing) return;
            if (Input.GetKeyDown(KeyCode.LeftArrow)) { selectedMarble = (selectedMarble - 1 + current.N) % current.N; FocusMarble(selectedMarble); }
            if (Input.GetKeyDown(KeyCode.RightArrow)) { selectedMarble = (selectedMarble + 1) % current.N; FocusMarble(selectedMarble); }
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Array.Clear(paintedThisStroke, 0, paintedThisStroke.Length);
                strokeColor = !purple[selectedMarble];
                PaintMarble(selectedMarble);
                MgfSfx.Play("tap", .22f);
            }
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                st.lastAction = "keyboard-pull";
                SetPullVisual(1f);
            }
            if (Input.GetKeyUp(KeyCode.DownArrow)) SubmitMix();
        }

        void SubmitMix()
        {
            if (current == null || (phase != GamePhase.Practice && phase != GamePhase.Playing)) return;
            int madePurple = CountPurple();
            if (lastWrongPurple >= 0 && madePurple == lastWrongPurple)
            {
                Refuse("구슬을 바꾼 뒤 다시 놓으시오");
                RingFailureShell();
                st.lastAction = "same-mixture-refused";
                SetPullVisual(0f);
                MgfBridge.NotifyChanged();
                return;
            }

            bool correct = GuseulRules.IsCorrect(current, madePurple);
            IntFraction submitted = GuseulRules.MadeProbability(current, madePurple);
            st.submittedFraction = submitted.Display;
            st.submissionLatched = true;
            revealCorrect = correct;
            revealPractice = phase == GamePhase.Practice;
            revealClock = 0f;
            phase = GamePhase.Reveal;
            SetPullVisual(0f);
            LaunchPlant(correct);

            if (revealPractice)
            {
                st.lastAction = correct ? "practice-correct" : "practice-wrong";
                st.misconceptionId = correct ? "" : GuseulRules.ClassifyWrong(current, madePurple);
                ShowEvidence(correct, madePurple, submitted, 0, 0);
                if (correct)
                {
                    hitStopClock = .065f;
                    MgfSfx.Play("correct", .55f);
                }
                else MgfSfx.Play("wrong", .30f);
            }
            else
            {
                bool first = st.attemptsByProblem[orderIndex] == 0;
                st.attemptsByProblem[orderIndex]++;
                if (first)
                {
                    st.firstAttempts++;
                    if (correct) st.firstAttemptCorrect++;
                }

                if (correct)
                {
                    RunExperiment(out lastEvent8, out lastEvent24, out lastEvent72);
                    st.trialCount = 72;
                    st.eventCount = lastEvent72;
                    int changeBonus = Mathf.Max(0, current.N - Mathf.Abs(madePurple - current.initialPurple) * 8);
                    int add = 120 + (first ? 80 : 0) + changeBonus + st.level * 20;
                    scoreBeforeAnimation = st.score;
                    st.score += add;
                    st.solved++;
                    st.lastAction = "correct-" + (orderIndex + 1);
                    st.misconceptionId = "";
                    lastWrongPurple = -1;
                    hitStopClock = .075f;
                    ShowEvidence(true, madePurple, submitted, 72, lastEvent72);
                    MgfSfx.Play("correct", .58f);
                }
                else
                {
                    lastWrongPurple = madePurple;
                    st.lives = Mathf.Max(0, st.lives - 1);
                    st.misconceptionId = GuseulRules.ClassifyWrong(current, madePurple);
                    st.lastAction = "wrong-" + st.misconceptionId;
                    ShowEvidence(false, madePurple, submitted, 0, 0);
                    DropSafetyLeaf(st.lives);
                    WrongCameraNudge();
                    MgfSfx.Play("wrong", .34f);
                }
            }
            SetPhaseUi();
            MgfBridge.NotifyChanged();
        }

        void FinishReveal()
        {
            if (revealPractice)
            {
                if (revealCorrect)
                {
                    st.onboarding = false;
                    st.frozen = false;
                    StartRunDirect(true, fastTestMode);
                    ShowToast("실전 시작 · 6개 배합 중 첫 제출 4개를 맞히시오", 3f);
                }
                else
                {
                    phase = GamePhase.Practice;
                    st.submissionLatched = false;
                    st.lastAction = "practice-repair";
                    ReplayGuide(false);
                    SetPhaseUi();
                    MgfBridge.NotifyChanged();
                }
                return;
            }

            if (!revealCorrect)
            {
                if (st.lives <= 0) { EndRun("lives"); return; }
                phase = GamePhase.Playing;
                st.submissionLatched = false;
                st.lastAction = "repair-same-problem";
                SetPhaseUi();
                ShowToast("검은 조개의 배합을 보고 구슬을 고치시오", 2.4f);
                MgfBridge.NotifyChanged();
                return;
            }

            orderIndex++;
            if (orderIndex >= GuseulRules.ProblemCount)
            {
                EndRun(st.firstAttemptCorrect >= GuseulRules.RequiredFirstCorrect ? "clear" : "mastery");
                return;
            }
            phase = GamePhase.Playing;
            LoadProblem();
            SetPhaseUi();
        }

        void EndRun(string reason)
        {
            if (phase == GamePhase.End) return;
            phase = GamePhase.End;
            gesture = Gesture.None;
            endReason = reason;
            bool clear = reason == "clear";
            st.phase = clear ? "clear" : "gameover";
            st.frozen = true;
            st.submissionLatched = true;
            st.lastAction = "end-" + reason;
            if (clear)
            {
                int best = PlayerPrefs.GetInt("guseul-saechong-best-first", 0);
                if (st.firstAttemptCorrect > best)
                {
                    PlayerPrefs.SetInt("guseul-saechong-best-first", st.firstAttemptCorrect);
                    PlayerPrefs.Save();
                }
                MgfSfx.Play("win", .60f);
            }
            else MgfSfx.Play("lose", .45f);
            SetPhaseUi();
            ShowEndResult(clear, reason);
            MgfBridge.NotifyChanged();
        }

        void RunExperiment(out int event8, out int event24, out int event72)
        {
            event8 = 0;
            event24 = 0;
            event72 = 0;
            for (int i = 0; i < 72; i++)
            {
                bool first = IsTargetColor(purple[experimentRng.Next(current.N)]);
                bool second = IsTargetColor(purple[experimentRng.Next(current.N)]); // 매번 같은 구슬집에서 복원 추출.
                bool happened;
                switch (current.eventType)
                {
                    case EventType.Not: happened = !first; break;
                    case EventType.Both: happened = first && second; break;
                    case EventType.AtLeastOne: happened = first || second; break;
                    default: happened = first; break;
                }
                if (happened)
                {
                    event72++;
                    if (i < 24) event24++;
                    if (i < 8) event8++;
                }
            }
        }

        bool IsTargetColor(bool isPurple) => current.targetPurple ? isPurple : !isPurple;

        public void TestStart()
        {
            StartRunDirect(false, true);
        }

        public void TestAnswerCorrect()
        {
            if (phase == GamePhase.Title || phase == GamePhase.End || current == null) StartRunDirect(false, true);
            if (phase == GamePhase.Reveal) FinishReveal();
            if (current == null) return;
            SetCount(current.CorrectPurple, current.N, false);
            SubmitMix();
        }

        public void TestAnswerWrong()
        {
            if (phase == GamePhase.Title || phase == GamePhase.End || current == null) StartRunDirect(false, true);
            if (phase == GamePhase.Reveal) FinishReveal();
            if (current == null) return;
            List<DiagnosticCandidate> d = GuseulRules.DiagnosticCandidates(current);
            int wrong = d.Count > 0 ? d[0].purpleCount : (current.CorrectPurple + 1) % (current.N + 1);
            SetCount(wrong, current.N, false);
            SubmitMix();
        }

        public string StateJson()
        {
            if (phase != GamePhase.End)
                st.phase = phase == GamePhase.Title ? "title" : phase == GamePhase.Practice ? "practice"
                    : phase == GamePhase.Reveal ? "reveal" : "playing";
            return JsonUtility.ToJson(st);
        }

        public string ProblemBankJson() => MgfJson.Bank(bank);
    }
}
