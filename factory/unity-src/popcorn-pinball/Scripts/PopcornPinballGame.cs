// 팝콘 핀볼 — 포신 드래그, 첫 시도 장부, 수리 루프, 학습 진도 실패 게이트.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.PopcornPinball
{
    public partial class PopcornPinballGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int firstAttempted;
            public int firstAttemptCorrect;
            public int shots;
            public int selectedTick;
            public int remainingSeconds;
            public int currentProblemIndex;
            public bool practice;
            public bool dragging;
            public bool frozen;
            public int pointerVersion;
            public int layoutVersion;
            public int pX;
            public int pY;
            public int refX;
            public int refY;
            public int screenW;
            public int screenH;
            public string currentProblemId = "";
            public string prompt = "";
            public string misconceptionId = "";
            public string lastAction = "boot";
        }

        internal enum Phase { Title, Practice, Playing, Feedback, SameArcBridge, DiameterBridge, Clear, GameOver }

        readonly State st = new State();
        readonly List<MgfProblem> bank = new List<MgfProblem>();
        readonly List<PinballProblem> deck = new List<PinballProblem>();
        readonly int[] attempts = new int[PopcornRules.Goal];

        internal Phase phase = Phase.Title;
        internal PinballProblem current;
        internal float remainingTime = 90f;
        internal float feedbackClock;
        internal float hitStopClock;
        internal float phaseClock;
        internal float idleClock;
        internal bool feedbackCorrect;
        internal bool feedbackPractice;
        internal bool fastTestMode;
        internal int currentIndex;
        internal int runSerial;
        internal int wrongTick;
        internal string endReason = "";
        System.Random rng;

        void Awake()
        {
            MgfLook.Quality(32f);
            // IL2CPP 스트리핑 방지를 위해 사용하는 프리미티브 콜라이더 타입을 명시한다.
            var keep = new GameObject("Primitive collider type keeper");
            keep.AddComponent<BoxCollider>();
            keep.AddComponent<SphereCollider>();
            keep.AddComponent<CapsuleCollider>();
            Destroy(keep);

            PopcornRules.ValidateAll();
            bank.AddRange(PopcornRules.BuildBank());
            BuildWorld();
            BuildUi();
            PrewarmText();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void PrewarmText()
        {
            var sb = new StringBuilder("팝콘핀볼포신을돌려쏴라중학교3학년원주각상자를눌러시작");
            sb.Append("포신각을맞춰쏘면상자가찬다목표중심각호길이첫발여분남은시간수리완성");
            sb.Append("중심각원주각같은호반원지름현비례구하고포신으로나타내시오눈금안에서놓으시오");
            sb.Append("중심각은원주각의두배같은호의원주각은같다반원에대한원주각은90도");
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
            HandlePointer();
            HandleKeyboard();

            if (hitStopClock > 0f) hitStopClock -= dt;
            else
            {
                phaseClock += dt;
                if (phase == Phase.Playing || phase == Phase.Feedback)
                {
                    remainingTime = Mathf.Max(0f, remainingTime - dt);
                    if (remainingTime <= 0f) EndRun("time");
                }
                if (phase == Phase.Practice || phase == Phase.Playing)
                {
                    idleClock += dt;
                    if (idleClock >= 8f)
                    {
                        idleClock = 0f;
                        ReplayGuide(true);
                    }
                }
                if (phase == Phase.Feedback)
                {
                    feedbackClock += dt;
                    float duration = fastTestMode ? .025f : (feedbackCorrect ? 2.05f : 1.15f);
                    if (feedbackClock >= duration) FinishFeedback();
                }
                else if (phase == Phase.DiameterBridge && phaseClock >= (fastTestMode ? .02f : 1.45f))
                    CompleteBridge();
            }

            AnimateView(dt);
            RefreshUi(dt);
            ApplyResponsiveLayout();
        }

        void ShowTitle()
        {
            phase = Phase.Title;
            current = null;
            deck.Clear();
            currentIndex = 0;
            Array.Clear(attempts, 0, attempts.Length);
            st.score = 0;
            st.lives = PopcornRules.StartLives;
            st.level = 1;
            st.solved = 0;
            st.firstAttempted = 0;
            st.firstAttemptCorrect = 0;
            st.shots = 0;
            st.selectedTick = 0;
            st.remainingSeconds = 90;
            st.currentProblemIndex = 0;
            st.practice = false;
            st.dragging = false;
            st.frozen = true;
            st.currentProblemId = "";
            st.prompt = "";
            st.misconceptionId = "";
            st.lastAction = "title";
            endReason = "";
            remainingTime = 90f;
            fastTestMode = false;
            phaseClock = 0f;
            SetPhaseView();
            MgfBridge.NotifyChanged();
        }

        internal void StartPractice()
        {
            phase = Phase.Practice;
            current = PopcornRules.Practice();
            remainingTime = 90f;
            feedbackClock = 0f;
            phaseClock = 0f;
            idleClock = 0f;
            wrongTick = 0;
            st.score = 0;
            st.lives = PopcornRules.StartLives;
            st.level = 0;
            st.solved = 0;
            st.firstAttempted = 0;
            st.firstAttemptCorrect = 0;
            st.shots = 0;
            st.selectedTick = 0;
            st.remainingSeconds = 90;
            st.currentProblemIndex = 0;
            st.practice = true;
            st.dragging = false;
            st.frozen = false;
            st.currentProblemId = current.id;
            st.prompt = current.prompt;
            st.misconceptionId = "";
            st.lastAction = "practice-start";
            ResetAimForProblem(true);
            SetPhaseView();
            ReplayGuide(false);
            MgfSfx.Play("whoosh", .32f);
            MgfBridge.NotifyChanged();
        }

        void StartRunDirect(bool fast)
        {
            runSerial++;
            rng = new System.Random(unchecked(Environment.TickCount ^ (runSerial * 104729)));
            fastTestMode = fast;
            phase = Phase.Playing;
            deck.Clear();
            for (int i = 0; i < PopcornRules.Goal; i++) deck.Add(PopcornRules.MakeForRound(i + 1, rng, runSerial * 100 + i));
            currentIndex = 0;
            Array.Clear(attempts, 0, attempts.Length);
            remainingTime = 90f;
            feedbackClock = 0f;
            phaseClock = 0f;
            idleClock = 0f;
            wrongTick = 0;
            endReason = "";
            st.score = 0;
            st.lives = PopcornRules.StartLives;
            st.level = 1;
            st.solved = 0;
            st.firstAttempted = 0;
            st.firstAttemptCorrect = 0;
            st.shots = 0;
            st.selectedTick = 0;
            st.remainingSeconds = 90;
            st.currentProblemIndex = 1;
            st.practice = false;
            st.dragging = false;
            st.frozen = false;
            st.misconceptionId = "";
            st.lastAction = fast ? "test-start" : "run-start";
            BindCurrent();
            ResetAllCartons();
            ResetAimForProblem(false);
            SetPhaseView();
            MgfBridge.NotifyChanged();
        }

        void BindCurrent()
        {
            current = deck[currentIndex];
            st.level = current.band;
            st.currentProblemIndex = currentIndex + 1;
            st.currentProblemId = current.id;
            st.prompt = current.prompt;
            st.selectedTick = 0;
            st.misconceptionId = "";
            UpdateProblemView();
        }

        internal void SubmitTick(int tick, bool fromTest = false)
        {
            if (phase != Phase.Practice && phase != Phase.Playing) return;
            if (tick < 1 || tick > PopcornRules.TickCount)
            {
                RefuseAim("눈금 안에서 놓으시오");
                return;
            }

            bool practice = phase == Phase.Practice;
            bool correct = PopcornRules.IsCorrect(current, tick);
            st.selectedTick = tick;
            st.shots++;
            st.lastAction = correct ? "shot-correct" : "shot-wrong";
            st.misconceptionId = correct ? "" : PopcornRules.Diagnose(current, tick);
            wrongTick = correct ? 0 : tick;

            bool first = true;
            if (!practice)
            {
                attempts[currentIndex]++;
                first = attempts[currentIndex] == 1;
                if (first)
                {
                    st.firstAttempted++;
                    if (correct) st.firstAttemptCorrect++;
                }
            }

            if (correct)
            {
                if (!practice)
                {
                    st.solved++;
                    st.score += first ? 100 : 20;
                }
                hitStopClock = fromTest ? 0f : .08f;
                MgfSfx.Play("correct", .58f);
            }
            else
            {
                if (!practice) st.lives = Math.Max(0, st.lives - 1);
                MgfSfx.Play("wrong", .43f);
            }

            feedbackCorrect = correct;
            feedbackPractice = practice;
            feedbackClock = 0f;
            phaseClock = 0f;
            phase = Phase.Feedback;
            st.phase = "feedback";
            st.dragging = false;
            st.frozen = true;
            st.remainingSeconds = Mathf.CeilToInt(remainingTime);
            BeginShotFeedback(tick, correct, practice, first);
            MgfBridge.NotifyChanged();

            if (!practice && !correct && (st.lives <= 0 || st.firstAttemptCorrect + (PopcornRules.Goal - st.firstAttempted) < PopcornRules.RequiredFirstCorrect))
                EndRun(st.lives <= 0 ? "lives" : "mastery");
        }

        void FinishFeedback()
        {
            if (phase != Phase.Feedback) return;
            if (feedbackPractice)
            {
                if (feedbackCorrect) StartRunDirect(false);
                else
                {
                    phase = Phase.Practice;
                    phaseClock = 0f;
                    st.phase = "practice";
                    st.frozen = false;
                    st.selectedTick = 0;
                    st.lastAction = "practice-retry";
                    ResetAimForProblem(true);
                    SetPhaseView();
                    ReplayGuide(false);
                    MgfBridge.NotifyChanged();
                }
                return;
            }

            if (!feedbackCorrect)
            {
                phase = Phase.Playing;
                phaseClock = 0f;
                st.phase = "playing";
                st.frozen = false;
                st.selectedTick = 0;
                st.lastAction = "repair-ready";
                ResetAimForProblem(false);
                SetPhaseView();
                MgfBridge.NotifyChanged();
                return;
            }

            if (st.solved >= PopcornRules.Goal)
            {
                if (st.firstAttemptCorrect >= PopcornRules.RequiredFirstCorrect) EndRun("clear");
                else EndRun("mastery");
                return;
            }
            if (st.solved == 3)
            {
                BeginSameArcBridge();
                return;
            }
            if (st.solved == 6)
            {
                BeginDiameterBridge();
                return;
            }
            NextProblem();
        }

        void NextProblem()
        {
            currentIndex++;
            if (currentIndex >= deck.Count) { EndRun("mastery"); return; }
            phase = Phase.Playing;
            phaseClock = 0f;
            idleClock = 0f;
            st.phase = "playing";
            st.practice = false;
            st.frozen = false;
            st.lastAction = "next-problem";
            BindCurrent();
            ResetAimForProblem(false);
            SetPhaseView();
            MgfBridge.NotifyChanged();
        }

        void BeginSameArcBridge()
        {
            phase = Phase.SameArcBridge;
            phaseClock = 0f;
            st.phase = "bridge";
            st.frozen = false;
            st.lastAction = "same-arc-bridge";
            SetPhaseView();
            ShowSameArcBridge();
            MgfBridge.NotifyChanged();
            if (fastTestMode) CompleteBridge();
        }

        void BeginDiameterBridge()
        {
            phase = Phase.DiameterBridge;
            phaseClock = 0f;
            st.phase = "bridge";
            st.frozen = true;
            st.lastAction = "diameter-bridge";
            SetPhaseView();
            ShowDiameterBridge();
            MgfSfx.Play("tap", .46f);
            MgfBridge.NotifyChanged();
        }

        internal void CompleteBridge()
        {
            if (phase != Phase.SameArcBridge && phase != Phase.DiameterBridge) return;
            st.lastAction = phase == Phase.SameArcBridge ? "same-arc-confirmed" : "diameter-confirmed";
            HideBridge();
            NextProblem();
        }

        void EndRun(string reason)
        {
            if (phase == Phase.Clear || phase == Phase.GameOver) return;
            endReason = reason;
            bool clear = reason == "clear" && st.solved >= PopcornRules.Goal && st.firstAttemptCorrect >= PopcornRules.RequiredFirstCorrect;
            phase = clear ? Phase.Clear : Phase.GameOver;
            st.phase = clear ? "clear" : "gameover";
            st.frozen = true;
            st.practice = false;
            st.dragging = false;
            st.remainingSeconds = Mathf.CeilToInt(remainingTime);
            st.lastAction = clear ? "clear" : "gameover-" + reason;
            SetPhaseView();
            ShowResult(clear, reason);
            MgfSfx.Play(clear ? "win" : "lose", .55f);
            MgfBridge.NotifyChanged();
        }

        void HandleKeyboard()
        {
            if (phase != Phase.Practice && phase != Phase.Playing) return;
            if (Input.GetKeyDown(KeyCode.LeftArrow)) SetAimTick(Mathf.Max(1, st.selectedTick - 1), true);
            if (Input.GetKeyDown(KeyCode.RightArrow)) SetAimTick(Mathf.Min(PopcornRules.TickCount, Mathf.Max(1, st.selectedTick + 1)), true);
            if (Input.GetKeyDown(KeyCode.Space) && st.selectedTick > 0) SubmitTick(st.selectedTick);
        }

        internal void SetAimTick(int tick, bool userAction)
        {
            tick = Mathf.Clamp(tick, 1, PopcornRules.TickCount);
            if (st.selectedTick == tick) return;
            st.selectedTick = tick;
            st.lastAction = "aim-" + tick;
            if (userAction) idleClock = 0f;
            UpdateAimVisual(tick);
            MgfSfx.Play("tap", .14f);
            MgfBridge.NotifyChanged();
        }

        internal void NotePointer(string action)
        {
            st.pointerVersion++;
            st.lastAction = action;
            idleClock = 0f;
            MgfBridge.NotifyChanged();
        }

        public void TestStart()
        {
            StartRunDirect(true);
        }

        void EnsureTestPlaying()
        {
            if (phase == Phase.Title || phase == Phase.Practice || phase == Phase.Clear || phase == Phase.GameOver)
            {
                StartRunDirect(true);
                return;
            }
            if (phase == Phase.Feedback)
            {
                feedbackClock = 99f;
                FinishFeedback();
            }
            if (phase == Phase.SameArcBridge || phase == Phase.DiameterBridge) CompleteBridge();
        }

        public void TestAnswerCorrect()
        {
            EnsureTestPlaying();
            if (phase == Phase.Playing) SubmitTick(current.answerTick, true);
        }

        public void TestAnswerWrong()
        {
            EnsureTestPlaying();
            if (phase == Phase.Playing)
            {
                int wrong = current.answerTick == 1 ? 2 : current.answerTick - 1;
                SubmitTick(wrong, true);
            }
        }

        public string StateJson() => JsonUtility.ToJson(st);
        public string ProblemBankJson() => MgfJson.Bank(bank);
    }
}
