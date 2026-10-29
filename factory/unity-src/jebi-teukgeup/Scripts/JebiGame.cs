// 제비 특급 — 상태 기계·입력·판정. 연출은 JebiView.cs(같은 partial 클래스).
//
// 답 입력 경로는 단 하나: 오른쪽 지급 눈금의 고리를 잡아 끌고(최대 이동 12px 이상) 1..24 눈금에서 놓는다.
// 놓는 순간 그 정수 길이로 판정한다. 확인 버튼·객관식·키보드 입력은 없다.
// 고정 연습만 첫 실패 뒤 유령 손을 따라 한 충분한 드래그를 7 cm에 보조 스냅한다(실전에는 없음).
// 시험 훅(answerCorrect/answerWrong)도 같은 Submit(value) 정수 판정 경로를 탄다.
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Mgf.JebiTeukgeup
{
    public partial class JebiGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int round;                 // 0 = 연습, 1..8 = 실전 역
            public int selectedLength;        // 지금 고리가 가리키는 눈금(0 = 눈금 아래, 25 = 눈금 위)
            public int firstAttempts;
            public int firstCorrect;
            public bool firstAttemptResolved;
            public string currentSegment = "PB";
            public int stationCount;
            public int lastSubmission;
            public string lastFailureReason = "";
            public string misconceptionId = "";
            public string problemId = "";
            public string kind = "";
            public bool onboarding;
            public bool dragging;
            public bool inspect;
            public int submissions;
            public int cancels;
            public int pointerVersion;
            public bool inputOpen;            // 지급 고리를 받을 수 있는 상태(연습·플레이). 단계 전환 때만 바뀐다
            public string result = "";
        }

        enum RunPhase { Title, Reveal, Practice, Playing, Feedback, Transition, End }

        readonly State st = new State();
        readonly List<JebiProblem> catalog = new List<JebiProblem>();
        readonly List<MgfProblem> bank = new List<MgfProblem>();
        readonly JebiProblem[] deck = new JebiProblem[JebiRules.Stations];
        RunPhase phase = RunPhase.Title;
        JebiProblem current;
        JebiRng runRng, layoutRng;
        int runSerial;
        int inspectQ;
        bool firstTry;
        float activeLeft;
        float phaseT;
        bool feedbackCorrect, feedbackPractice;
        string pendingEnd = "";
        int combo;
        // 지급 고리
        bool grabbing;
        Vector2 grabStart;
        float maxDisp;
        int handleValue, startValue;
        float idleT;
        int practiceMisses;
        bool assistedPracticeGrab;
        int transitionTo;
        int bridgeKind;
        bool transitionLoaded;

        void Awake()
        {
            catalog.AddRange(JebiRules.BuildCatalog());
            for (int i = 0; i < catalog.Count; i++)
            {
                var p = catalog[i];
                bank.Add(new MgfProblem
                {
                    id = p.id,
                    prompt = p.prompt,
                    choices = null,
                    answer = p.t + " cm",
                    answerNumeric = p.t,
                    unitConcept = p.ConceptName,
                });
            }
            string verify = JebiRules.Verify(catalog);
            if (!verify.EndsWith("bad=0")) Debug.LogWarning("[Jebi] 은행 검사 실패: " + verify);
            Prewarm();
            BuildView();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb = new StringBuilder();
            sb.Append(ViewStrings);
            for (int i = 0; i < catalog.Count; i++) sb.Append(catalog[i].prompt).Append(catalog[i].ConceptName);
            MgfText.Prewarm(sb.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(0.05f, Time.deltaTime);
            Relayout(false);
            HandleInput();
            phaseT += dt;
            switch (phase)
            {
                case RunPhase.Reveal:
                    if (phaseT >= 0.42f) BeginSession(true);
                    break;
                case RunPhase.Practice:
                    idleT += dt;
                    if (idleT >= 8f) { idleT = 0f; BoostGuide(); }
                    break;
                case RunPhase.Playing:
                    if (!grabbing) idleT += dt;
                    activeLeft -= dt;
                    if (activeLeft <= 0f) { activeLeft = 0f; EndSession("time"); }
                    break;
                case RunPhase.Feedback:
                    if (phaseT >= FeedbackDuration(feedbackCorrect, feedbackPractice)) FinishFeedback();
                    break;
                case RunPhase.Transition:
                    if (!transitionLoaded && phaseT >= TransitionOut) { transitionLoaded = true; LoadRound(transitionTo, true); }
                    if (phaseT >= TransitionOut + BridgeHold(bridgeKind) + TransitionIn) FinishTransition();
                    break;
            }
            bool open = phase == RunPhase.Practice || phase == RunPhase.Playing;
            if (st.inputOpen != open) { st.inputOpen = open; MgfBridge.NotifyChanged(); }
            UpdateView(dt);
        }

        const float TransitionOut = 0.5f, TransitionIn = 0.55f;
        static float BridgeHold(int kind) => kind == 0 ? 0.05f : 2.1f;
        static float FeedbackDuration(bool correct, bool practice) => correct ? (practice ? 2.6f : 2.05f) : 1.55f;

        // ───────────────────────── 흐름 ─────────────────────────
        void ShowTitle()
        {
            phase = RunPhase.Title; phaseT = 0f;
            current = JebiRules.Practice();
            ResetStats();
            st.phase = "title";
            ShowTitleVisual();
            MgfBridge.NotifyChanged();
        }

        void ResetStats()
        {
            st.score = 0; st.lives = JebiRules.StartRings; st.level = 1; st.solved = 0;
            st.round = 0; st.selectedLength = 0; st.firstAttempts = 0; st.firstCorrect = 0;
            st.firstAttemptResolved = false; st.stationCount = 0; st.lastSubmission = 0;
            st.lastFailureReason = ""; st.misconceptionId = ""; st.onboarding = false; st.dragging = false;
            st.inspect = false; st.submissions = 0; st.cancels = 0; st.result = "";
            activeLeft = JebiRules.ActiveSeconds; combo = 0; pendingEnd = ""; grabbing = false; idleT = 0f;
            practiceMisses = 0; assistedPracticeGrab = false;
        }

        void StartReveal()
        {
            phase = RunPhase.Reveal; phaseT = 0f;
            RevealVisual();
            JebiAudio.Play("bell", 0.45f);
        }

        void BeginSession(bool practice)
        {
            runSerial++;
            // 판마다 시드를 SplitMix64 로 섞고, 정답 t 는 기각 표본 균등 추출(JebiRng)로 뽑는다.
            ulong mix = Mix64((ulong)runSerial * 0x9E3779B97F4A7C15UL ^ (ulong)Environment.TickCount ^ ((ulong)DateTime.UtcNow.Ticks << 7));
            runRng = new JebiRng(mix);
            layoutRng = new JebiRng(Mix64(mix ^ 0x2f6b13UL));   // 그림 배치는 정답과 독립인 별도 난수
            BuildDeck();
            ResetStats();
            st.phase = "playing";
            ResetRunVisual();
            if (practice)
            {
                phase = RunPhase.Practice; phaseT = 0f;
                current = JebiRules.Practice();
                firstTry = true;
                st.onboarding = true; st.round = 0; st.level = 1;
                st.problemId = current.id; st.kind = current.kind.ToString(); st.currentSegment = current.Target;
                handleValue = startValue = 0; st.selectedLength = 0;
                LoadBoard(current, false, false);
                StartGuide();
            }
            else
            {
                phase = RunPhase.Playing; phaseT = 0f;
                LoadRound(1, false);
            }
            SetScreen();
            MgfBridge.NotifyChanged();
        }

        static ulong Mix64(ulong z)
        {
            z += 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        void BuildDeck()
        {
            // 정답 t 를 역마다 1..24 에서 독립·균등하게 먼저 뽑고, 같은 종류·같은 t 의 5변형 중 하나를 고른다.
            for (int r = 1; r <= JebiRules.Stations; r++)
            {
                var kind = JebiRules.KindForRound(r);
                int t = runRng.Next(1, JebiRules.MaxLen + 1);
                int v = runRng.Next(0, 5);
                JebiProblem pick = null;
                for (int i = 0; i < catalog.Count; i++)
                    if (catalog[i].kind == kind && catalog[i].t == t && catalog[i].variant == v) { pick = catalog[i]; break; }
                deck[r - 1] = pick;
            }
            // 점검역(5역)의 견습 줄 q 는 정답과 독립·균등. 맞는 줄일 수도 있다(1/24) — 틀린 값만 고르면 q 배제 전략이 생긴다.
            inspectQ = runRng.Next(1, JebiRules.MaxLen + 1);
        }

        void LoadRound(int round, bool offscreen)
        {
            current = deck[round - 1];
            firstTry = true;
            st.round = round;
            st.level = round <= 3 ? 1 : round <= 5 ? 2 : 3;
            st.firstAttemptResolved = false;
            st.problemId = current.id; st.kind = current.kind.ToString(); st.currentSegment = current.Target;
            st.inspect = JebiRules.IsInspectRound(round);
            st.misconceptionId = ""; st.lastFailureReason = "";
            handleValue = startValue = st.inspect ? inspectQ : 0;
            st.selectedLength = handleValue;
            idleT = 0f;
            LoadBoard(current, st.inspect, offscreen);
            MgfBridge.NotifyChanged();
        }

        void Submit(int value)
        {
            if (phase != RunPhase.Practice && phase != RunPhase.Playing) return;
            bool practice = phase == RunPhase.Practice;
            bool correct = current.IsCorrect(value);
            st.submissions++;
            st.lastSubmission = value; st.selectedLength = value; handleValue = value;
            if (!practice && firstTry)
            {
                st.firstAttempts++;
                if (correct) st.firstCorrect++;
                st.firstAttemptResolved = true;
            }
            if (correct)
            {
                st.misconceptionId = ""; st.lastFailureReason = "";
                if (!practice)
                {
                    st.solved++; st.stationCount++;
                    combo = firstTry ? combo + 1 : 0;
                    st.score += firstTry ? 100 : 40;
                }
            }
            else
            {
                if (practice) practiceMisses++;
                st.misconceptionId = JebiRules.MisconceptionFor(current, value);
                st.lastFailureReason = value < current.t ? "short" : "long";
                if (!practice)
                {
                    st.lives--; combo = 0;
                    activeLeft = Mathf.Max(0f, activeLeft - JebiRules.WrongPenalty);
                    int remaining = JebiRules.Stations - st.firstAttempts;
                    if (st.lives <= 0) pendingEnd = "rings";
                    else if (st.firstCorrect + remaining < JebiRules.PassFirst) pendingEnd = "impossible";
                    else if (activeLeft <= 0f) pendingEnd = "time";
                    if (pendingEnd != "") { st.phase = "gameover"; st.result = pendingEnd; }
                }
            }
            firstTry = false;
            feedbackCorrect = correct; feedbackPractice = practice;
            phase = RunPhase.Feedback; phaseT = 0f;
            OnSubmitVisual(correct, value, practice);
            SetScreen();
            MgfBridge.NotifyChanged();
        }

        void FinishFeedback()
        {
            if (phase != RunPhase.Feedback) return;
            if (feedbackPractice)
            {
                if (feedbackCorrect)
                {
                    st.onboarding = false;
                    StartTransition(1, 0);
                }
                else
                {
                    phase = RunPhase.Practice; phaseT = 0f;
                    ResumeRepairVisual();
                }
                SetScreen();
                return;
            }
            if (!feedbackCorrect)
            {
                if (pendingEnd != "") { EndSession(pendingEnd); return; }
                phase = RunPhase.Playing; phaseT = 0f; idleT = 0f;
                ResumeRepairVisual();
                SetScreen();
                MgfBridge.NotifyChanged();
                return;
            }
            if (st.round >= JebiRules.Stations) { EndSession("clear"); return; }
            int next = st.round + 1;
            StartTransition(next, st.round == 3 ? 1 : st.round == 5 ? 2 : 0);
        }

        void StartTransition(int next, int bridge)
        {
            phase = RunPhase.Transition; phaseT = 0f;
            transitionTo = next; bridgeKind = bridge; transitionLoaded = false;
            TransitionVisual(bridge);
            SetScreen();
        }

        void FinishTransition()
        {
            if (!transitionLoaded) { transitionLoaded = true; LoadRound(transitionTo, false); }
            phase = RunPhase.Playing; phaseT = 0f; idleT = 0f;
            ArriveVisual();
            SetScreen();
            MgfBridge.NotifyChanged();
        }

        void EndSession(string reason)
        {
            if (phase == RunPhase.End) return;
            phase = RunPhase.End; phaseT = 0f;
            grabbing = false; st.dragging = false;
            st.result = reason;
            st.phase = reason == "clear" ? "clear" : "gameover";
            int best = PlayerPrefs.GetInt("jebi-teukgeup.bestFirst", 0);
            if (st.firstCorrect > best) { PlayerPrefs.SetInt("jebi-teukgeup.bestFirst", st.firstCorrect); PlayerPrefs.Save(); }
            EndVisual(reason);
            SetScreen();
            MgfBridge.NotifyChanged();
        }

        // ───────────────────────── 입력 ─────────────────────────
        void HandleInput()
        {
            bool down = MgfPointer.Down;
            Vector2 lp = PointerLogical();
            if (down)
            {
                st.pointerVersion++;
                Ripple(lp);
                MgfBridge.NotifyChanged();
            }

            switch (phase)
            {
                case RunPhase.Title:
                    if (down) StartReveal();
                    return;
                case RunPhase.Reveal:
                    return;
                case RunPhase.End:
                    if (down)
                    {
                        if (phaseT < 0.9f) return;
                        if (HitRestartBell(lp)) { JebiAudio.Play("bell", 0.4f); BeginSession(false); }
                        else Refuse("종을 누르면 다시 출발한다.", RefuseTarget.Bell, lp);
                    }
                    return;
                case RunPhase.Feedback:
                case RunPhase.Transition:
                    if (down) Refuse("도착하면 다음 줄을 뽑는다.", RefuseTarget.Cart, lp);
                    return;
            }

            // Practice / Playing — 지급 고리
            if (down)
            {
                // 첫 실패 뒤에는 화면의 유령 손을 따라 한 긴 드래그도 연습 고리 입력으로 받는다.
                // 탭은 maxDisp 게이트를 그대로 통과하지 못하고, 실전 입력 범위는 넓히지 않는다.
                bool guidedCatch = phase == RunPhase.Practice && practiceMisses > 0 && NearPracticeCapture(lp);
                if (NearHandle(lp) || guidedCatch)
                {
                    grabbing = true; st.dragging = true;
                    grabStart = lp; maxDisp = 0f; startValue = handleValue; idleT = 0f;
                    assistedPracticeGrab = guidedCatch;
                    HideGuide();
                    OnGrabVisual();
                    JebiAudio.Play("tick", 0.3f, 0.8f);
                    MgfBridge.NotifyChanged();
                }
                else if (NearRail(lp))
                {
                    if (phase == RunPhase.Practice) practiceMisses++;
                    Refuse("눈금은 누르는 곳이 아니다. 고리를 잡아 끌어 올려라.", RefuseTarget.RailPath, lp);
                }
                else
                {
                    if (phase == RunPhase.Practice) practiceMisses++;
                    Refuse("오른쪽 고리를 잡아 끌어 줄을 뽑아라.", RefuseTarget.Handle, lp);
                }
            }

            if (!grabbing) return;

            if (MgfPointer.Held)
            {
                maxDisp = Mathf.Max(maxDisp, (lp - grabStart).magnitude);
                int v = ValueFromY(lp.y);
                if (v != handleValue)
                {
                    int dir = v > handleValue ? 1 : -1;
                    handleValue = v; st.selectedLength = v;
                    OnHandleStep(dir);
                    MgfBridge.NotifyChanged();
                }
            }

            if (MgfPointer.Up || !MgfPointer.Held)
            {
                grabbing = false; st.dragging = false;
                OnReleaseVisual();
                if (maxDisp < 12f)
                {
                    assistedPracticeGrab = false;
                    handleValue = startValue; st.selectedLength = startValue;
                    Refuse("고리를 끌었다가 놓아야 줄이 나간다.", RefuseTarget.Handle, lp);
                    MgfBridge.NotifyChanged();
                    return;
                }
                // 첫 실패 뒤 유령 손을 따라 충분히 길게 끌었다면 연습 목표 고리에 보조 스냅한다.
                // 실전에는 적용하지 않고, 탭/짧은 끌기는 위 12 px 게이트에서 계속 거부된다.
                if (phase == RunPhase.Practice && assistedPracticeGrab && maxDisp >= 54f)
                {
                    handleValue = JebiRules.Practice().t;
                    st.selectedLength = handleValue;
                }
                if (handleValue < 1 || handleValue > JebiRules.MaxLen)
                {
                    assistedPracticeGrab = false;
                    handleValue = startValue; st.selectedLength = startValue; st.cancels++;
                    Refuse("눈금 1~24 cm 안에서 놓아야 출발한다.", RefuseTarget.RailPath, lp);
                    MgfBridge.NotifyChanged();
                    return;
                }
                assistedPracticeGrab = false;
                Submit(handleValue);
            }
        }

        // ───────────────────────── 시험 훅 ─────────────────────────
        void EnsurePlayable()
        {
            if (phase == RunPhase.Title || phase == RunPhase.Reveal || phase == RunPhase.End) { BeginSession(false); return; }
            if (phase == RunPhase.Practice)
            {
                st.onboarding = false; HideGuide();
                phase = RunPhase.Playing; phaseT = 0f;
                LoadRound(1, false); ArriveVisual(); SetScreen();
                return;
            }
            if (phase == RunPhase.Feedback) FinishFeedback();
            if (phase == RunPhase.Transition) FinishTransition();
            if (phase == RunPhase.End) BeginSession(false);
        }

        public void TestStart() { BeginSession(false); }

        public void TestAnswerCorrect()
        {
            EnsurePlayable();
            Submit(current.t);
        }

        public void TestAnswerWrong()
        {
            EnsurePlayable();
            Submit(JebiRules.WrongValueFor(current));
        }

        public string StateJson() => JsonUtility.ToJson(st);
        public string ProblemBankJson() => MgfJson.Bank(bank);
    }
}
