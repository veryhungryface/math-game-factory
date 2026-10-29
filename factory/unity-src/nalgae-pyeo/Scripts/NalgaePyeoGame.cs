using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.NalgaePyeo
{
    public partial class NalgaePyeoGame : MonoBehaviour, IMgfGame
    {
        static readonly Type KeepBox = typeof(BoxCollider);
        static readonly Type KeepSphere = typeof(SphereCollider);
        static readonly Type KeepCapsule = typeof(CapsuleCollider);

        [Serializable]
        public sealed class GameState : MgfState
        {
            public int round;
            public int firstAttempts;
            public int firstCorrect;
            public int firstMisses;
            public int selectedDeg = 30;
            public int targetBand;
            public bool firstAttemptLocked;
            public bool onboarding;
            public bool repairMode;
            public string activeProblemId = "";
            public string lastResult = "";
            public string misconceptionId = "";
            public string lastInputKind = "none";
            public int inputRevision;
            public int submissionSerial;
            public bool layoutLand;
            public int handleXPx;
            public int handleYPx;
        }

        enum Flow { Title, Opening, Practice, Playing, Feedback, Bridge, End }

        readonly GameState st = new GameState();
        readonly List<MgfProblem> bank = new List<MgfProblem>(432);
        List<WingProblem> pool;
        List<WingProblem> deck;
        WingProblem current;
        Flow flow = Flow.Title;
        bool firstAttempt;
        bool lastCorrect;
        bool feedbackPractice;
        bool pendingFailure;
        bool dragging;
        float flowClock;
        float timeLeft;
        float idleClock;
        float dragTravel;
        float lastPointerX;
        int angleAtDown;
        Vector2 downPos;
        int runSerial;
        int shownSecond = -1;
        string endReason = "";

        public GameState State => st;
        public WingProblem Current => current;
        public bool IsTitle => flow == Flow.Title;
        public bool IsPractice => flow == Flow.Practice;
        public bool IsFeedback => flow == Flow.Feedback;
        public float TimeLeft => timeLeft;

        void Awake()
        {
            GC.KeepAlive(KeepBox); GC.KeepAlive(KeepSphere); GC.KeepAlive(KeepCapsule);
            MgfLook.Quality(34f);
            pool = NalgaeRules.BuildBank();
            for (int i = 0; i < pool.Count; i++) bank.Add(pool[i].ToMgf());
            Prewarm();
            BuildWorld();
            BuildUi();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb = new StringBuilder(24000);
            sb.Append("날개펴날개를끌어원주각을만들어라중학교3학년원주각중심각같은호호의길이비흑칠관절인형황금비단주름도르래장력끈첫시도연습실전남은시간초등불ABCDOPQ∠°÷=:0123456789. ");
            for (int i = 0; i < bank.Count; i++) sb.Append(bank[i].prompt).Append(bank[i].answer).Append(bank[i].unitConcept);
            MgfText.Prewarm(sb.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(.05f, Time.unscaledDeltaTime);
            Relayout(false);
            HandlePointer();
            flowClock += dt;
            if (flow == Flow.Opening && flowClock >= .42f) BeginPractice();
            else if (flow == Flow.Practice)
            {
                idleClock += dt;
                if (idleClock >= 8f) { idleClock = 0f; BoostGuide(true); }
            }
            else if (flow == Flow.Playing)
            {
                timeLeft -= dt;
                if (timeLeft <= 0f) { timeLeft = 0f; EndRun("time"); }
                else
                {
                    idleClock += dt;
                    if (idleClock >= 8f) { idleClock = 0f; BoostGuide(false); }
                    int sec = Mathf.CeilToInt(timeLeft);
                    if (sec != shownSecond) { shownSecond = sec; UpdateHudText(); }
                }
            }
            else if (flow == Flow.Feedback && flowClock >= (lastCorrect ? 1.16f : 1.30f)) FinishFeedback();
            else if (flow == Flow.Bridge && flowClock >= 1.35f) LoadRound(st.solved + 1);
            TickView(dt);
        }

        void ResetRunState()
        {
            st.score = 0; st.lives = NalgaeRules.Lives; st.level = 1; st.solved = 0; st.round = 0;
            st.firstAttempts = 0; st.firstCorrect = 0; st.firstMisses = 0; st.selectedDeg = 30;
            st.targetBand = 1; st.firstAttemptLocked = false; st.onboarding = false; st.repairMode = false;
            st.activeProblemId = ""; st.lastResult = ""; st.misconceptionId = ""; st.lastInputKind = "none";
            st.inputRevision = 0; st.submissionSerial = 0;
            firstAttempt = true; dragging = false; pendingFailure = false; feedbackPractice = false;
            flowClock = 0f; idleClock = 0f; dragTravel = 0f; timeLeft = NalgaeRules.SessionSeconds; shownSecond = -1; endReason = "";
        }

        void ShowTitle()
        {
            ResetRunState(); flow = Flow.Title; st.phase = "title"; current = null; deck = null;
            ResetVisuals(); ShowTitleVisual(); Notify();
        }

        void StartOpening()
        {
            if (flow != Flow.Title && flow != Flow.End) return;
            ResetRunState(); flow = Flow.Opening; st.phase = "playing"; flowClock = 0f;
            BeginOpeningVisual(); MgfSfx.Play("whoosh", .36f); Notify();
        }

        void BeginPractice()
        {
            flow = Flow.Practice; st.phase = "playing"; st.onboarding = true; st.level = 1;
            LoadProblem(NalgaeRules.Practice(), 30, true); BoostGuide(true);
        }

        void BeginRun(bool deterministic)
        {
            runSerial++;
            if (!deterministic && runSerial < 2) runSerial = (int)(DateTime.UtcNow.Ticks % 900000L) + 37;
            ResetRunState();
            deck = NalgaeRules.BuildDeck(pool, deterministic ? 731 : runSerial * 7919 + 41);
            flow = Flow.Playing; st.phase = "playing"; st.onboarding = false;
            ResetCurtainsForRun();
            LoadRound(1);
        }

        void LoadRound(int round)
        {
            if (round > NalgaeRules.Goal)
            {
                EndRun(st.firstCorrect >= NalgaeRules.Mastery ? "clear" : "mastery");
                return;
            }
            WingProblem p = deck[round - 1];
            if (round > 1 && p.band != st.targetBand)
            {
                st.targetBand = p.band; st.level = p.band; flow = Flow.Bridge; st.phase = "playing"; flowClock = 0f;
                ShowBridgeVisual(p.band); Notify(); return;
            }
            int start = IndependentStart(runSerial, round, p.targetDeg);
            LoadProblem(p, start, false);
        }

        static int IndependentStart(int seed, int round, int target)
        {
            uint x = unchecked((uint)(seed * 0x45D9F3B + round * 0x119DE1F3));
            x ^= x >> 16; x *= 0x7FEB352D; x ^= x >> 15;
            int v = 5 + (int)(x % 18u) * 5;
            if (v == target) v = v == 90 ? 85 : v + 5;
            return v;
        }

        void LoadProblem(WingProblem p, int start, bool practice)
        {
            current = p; firstAttempt = true; pendingFailure = false;
            st.activeProblemId = p.id; st.selectedDeg = start; st.targetBand = Math.Max(1, p.band); st.level = Math.Max(1, p.band);
            st.round = practice ? 0 : st.solved + 1; st.firstAttemptLocked = false; st.lastResult = ""; st.misconceptionId = ""; st.repairMode = false;
            dragging = false; idleClock = 0f; flowClock = 0f;
            flow = practice ? Flow.Practice : Flow.Playing; st.phase = "playing"; st.onboarding = practice;
            LoadProblemVisual(p, practice); UpdateAngleVisual(start, false); UpdateHudText(); Notify();
        }

        void Submit(int selected)
        {
            if ((flow != Flow.Practice && flow != Flow.Playing) || current == null) return;
            selected = Mathf.Clamp(Mathf.RoundToInt(selected / 5f) * 5, 5, 90);
            st.selectedDeg = selected; st.submissionSerial++;
            feedbackPractice = flow == Flow.Practice;
            lastCorrect = NalgaeRules.IsCorrect(current, selected);
            if (!feedbackPractice && firstAttempt)
            {
                st.firstAttempts++; st.firstAttemptLocked = true;
                if (lastCorrect) st.firstCorrect++; else st.firstMisses++;
            }
            st.lastResult = lastCorrect ? "correct" : "wrong";
            st.misconceptionId = lastCorrect ? "" : NalgaeRules.MisconceptionId(current, selected);
            if (lastCorrect)
            {
                if (!feedbackPractice)
                {
                    st.solved++;
                    st.score += firstAttempt ? 100 : 20;
                }
                st.repairMode = false;
            }
            else
            {
                if (!feedbackPractice)
                {
                    st.lives = Math.Max(0, st.lives - 1);
                    st.repairMode = true;
                    pendingFailure = st.lives <= 0 || st.firstMisses >= 3;
                }
                firstAttempt = false;
            }
            dragging = false; st.lastInputKind = "wing_release"; st.inputRevision++;
            flow = Flow.Feedback; st.phase = "playing"; flowClock = 0f;
            BeginFeedbackVisual(lastCorrect, feedbackPractice, selected, st.misconceptionId);
            UpdateHudText(); Notify();
        }

        void FinishFeedback()
        {
            if (flow != Flow.Feedback) return;
            FinishFeedbackVisual(lastCorrect);
            if (feedbackPractice)
            {
                if (lastCorrect) BeginRun(false);
                else
                {
                    flow = Flow.Practice; st.phase = "playing"; st.lastResult = ""; flowClock = 0f;
                    LoadProblemVisual(current, true); UpdateAngleVisual(st.selectedDeg, false); BoostGuide(true); Notify();
                }
                return;
            }
            if (pendingFailure) { EndRun(st.lives <= 0 ? "cords" : "mastery"); return; }
            if (lastCorrect) LoadRound(st.solved + 1);
            else
            {
                flow = Flow.Playing; st.phase = "playing"; st.lastResult = ""; flowClock = 0f;
                LoadProblemVisual(current, false); UpdateAngleVisual(st.selectedDeg, false); UpdateHudText(); BoostGuide(false); Notify();
            }
        }

        void EndRun(string reason)
        {
            dragging = false; endReason = reason; flow = Flow.End; flowClock = 0f;
            bool win = reason == "clear" && st.solved == NalgaeRules.Goal && st.firstCorrect >= NalgaeRules.Mastery && st.lives > 0 && timeLeft > 0f;
            st.phase = win ? "clear" : "gameover"; st.onboarding = false;
            ShowEndVisual(win, reason); MgfSfx.Play(win ? "win" : "lose", .52f); Notify();
        }

        void HandlePointer()
        {
            if (MgfPointer.Down)
            {
                downPos = MgfPointer.Position;
                if (flow == Flow.Title || flow == Flow.End) { StartOpening(); return; }
                if (flow == Flow.Opening || flow == Flow.Feedback || flow == Flow.Bridge)
                {
                    st.lastInputKind = "busy_tap"; st.inputRevision++; Refuse(MgfPointer.Position, "도르래가 멈출 때까지 기다리시오"); Notify(); return;
                }
                if (flow != Flow.Practice && flow != Flow.Playing) return;
                float y01 = MgfPointer.Position.y / Mathf.Max(1f, Screen.height);
                bool controlZone = MgfBridge.IsLandLayout
                    ? y01 <= .76f && MgfPointer.Position.x >= Screen.width * .25f && MgfPointer.Position.x <= Screen.width * .78f
                    : y01 <= .57f;
                if (controlZone)
                {
                    dragging = true; angleAtDown = st.selectedDeg; lastPointerX = MgfPointer.Position.x; dragTravel = 0f;
                    st.lastInputKind = "wing_drag"; st.inputRevision++; BeginDragVisual(); MgfSfx.Play("tap", .18f); Notify();
                }
                else
                {
                    dragging = false; st.lastInputKind = "refused"; st.inputRevision++;
                    Refuse(MgfPointer.Position, "아래 날개 끝을 가로로 끌어 각을 만드시오"); BoostGuide(flow == Flow.Practice); Notify();
                }
            }
            if (dragging && MgfPointer.Held)
            {
                float dx = MgfPointer.Position.x - downPos.x;
                dragTravel += Mathf.Abs(MgfPointer.Position.x - lastPointerX); lastPointerX = MgfPointer.Position.x;
                int next = Mathf.Clamp(angleAtDown + Mathf.RoundToInt(dx / 9f) * 5, 5, 90);
                if (next != st.selectedDeg)
                {
                    st.selectedDeg = next; st.inputRevision++; st.lastInputKind = "wing_drag";
                    UpdateAngleVisual(next, true); UpdateHudText(); Notify();
                }
            }
            if (dragging && MgfPointer.Up)
            {
                dragging = false; EndDragVisual();
                float moved = Mathf.Max(dragTravel, Mathf.Abs(MgfPointer.Position.x - downPos.x));
                if (moved >= 18f) Submit(st.selectedDeg);
                else
                {
                    st.lastInputKind = "short_tap"; st.inputRevision++;
                    Refuse(MgfPointer.Position, "날개 끝을 18px 이상 가로로 끌어 놓으시오"); BoostGuide(flow == Flow.Practice); Notify();
                }
            }
        }

        void Notify() => MgfBridge.NotifyChanged();

        public void TestStart() => BeginRun(true);

        void EnsureTestPlaying()
        {
            if (flow == Flow.Feedback) FinishFeedback();
            if (flow == Flow.Bridge) LoadRound(st.solved + 1);
            if (flow != Flow.Playing) BeginRun(true);
        }

        public void TestAnswerCorrect() { EnsureTestPlaying(); Submit(current.targetDeg); }
        public void TestAnswerWrong() { EnsureTestPlaying(); Submit(current.targetDeg == 90 ? 85 : current.targetDeg + 5); }

        public string StateJson()
        {
            st.layoutLand = MgfBridge.IsLandLayout;
            CaptureHandleScreen(out st.handleXPx, out st.handleYPx);
            return JsonUtility.ToJson(st);
        }

        public string ProblemBankJson() => MgfJson.Bank(bank);
    }
}
