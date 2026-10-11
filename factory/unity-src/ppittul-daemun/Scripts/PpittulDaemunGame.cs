// 삐뚤 대문 — B 모서리 손잡이를 수평으로 밀고 놓는 것이 유일한 답 입력이다.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.PpittulDaemun
{
    public partial class PpittulDaemunGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int selectedDetent = 2;
            public int selectedAngle = 60;
            public int firstAttemptTotal;
            public int firstAttemptCorrect;
            public int lastThreeFirstCorrect;
            public int courseIndex;
            public int presentedCount;
            public int attemptIndex;
            public int brokenNeedles;
            public int pointerVersion;
            public int detentTicks;
            public string problemId = "";
            public string prompt = "";
            public string misconceptionId = "";
            public bool onboarding;
            public int practiceStep;
            public int promptLeftPx;
            public int relationRightPx;
            public bool hudNeedlesVisible;
        }

        enum Flow { Title, Practice, Playing, Reveal, End }

        readonly State st = new State();
        readonly List<MgfProblem> bank = new List<MgfProblem>();
        Flow flow = Flow.Title;
        GateProblem current;
        System.Random rng;
        int[] course;
        int runSerial;
        int practiceStep;
        bool firstAttempt;
        bool dragging;
        bool invalidPointer;
        bool keyboardGrab;
        Vector2 dragStart;
        int dragStartDetent;
        bool revealCorrect;
        bool revealWasFirst;
        int revealSubmittedDetent;
        float revealTime;
        float playTime;
        float idleGuide;
        string endReason = "";
        int bestScore;
        int bestFirst;

        void Awake()
        {
            MgfLook.Quality(28f);
            bank.AddRange(PpittulRules.BuildBank());
            if (!PpittulRules.AuditBank(bank, out string audit))
                throw new InvalidOperationException("문제 은행 감사 실패: " + audit);
            BuildWorld();
            BuildUi();
            Prewarm();
            bestScore = PlayerPrefs.GetInt("ppittul-daemun.bestScore", 0);
            bestFirst = PlayerPrefs.GetInt("ppittul-daemun.bestFirst", 0);
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var s = new StringBuilder("삐뚤대문틀을밀어문을맞춰라중학교2학년사각형의성질평행사변형대각이웃각대각선직사각형마름모정사각형포함관계대문을밀어문표에맞는각으로잠그시오연습실전퍼레이드바늘점수첫시도재도전시간초과공연완료숙련통과문이닫혔다다시계산하시오항상조건네변같음둘다이등분수직비수직최소cm");
            for (int i = 0; i < bank.Count; i++) { s.Append(bank[i].prompt); s.Append(bank[i].answer); }
            MgfText.Prewarm(s.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(0.05f, Time.deltaTime);
            HandleInput();

            if (flow == Flow.Practice)
            {
                idleGuide += dt;
                if (idleGuide >= 8f) { idleGuide = 0f; ReplayGuide(true); }
            }
            else if (flow == Flow.Playing)
            {
                playTime += dt;
                if (playTime >= 90f) EndRun("timeout");
            }
            else if (flow == Flow.Reveal)
            {
                revealTime += dt;
                float hold = revealCorrect ? 1.18f : 1.28f;
                if (revealTime >= hold) FinishReveal();
            }

            UpdateWorld(dt);
            UpdateUi(dt);
        }

        void ShowTitle()
        {
            flow = Flow.Title;
            current = null;
            endReason = "";
            dragging = false; invalidPointer = false; keyboardGrab = false;
            st.score = 0; st.lives = PpittulRules.StartLives; st.level = 1; st.solved = 0;
            st.selectedDetent = 2; st.selectedAngle = 60;
            st.firstAttemptTotal = 0; st.firstAttemptCorrect = 0; st.lastThreeFirstCorrect = 0;
            st.courseIndex = 0; st.presentedCount = 0; st.attemptIndex = 0;
            st.brokenNeedles = 0; st.detentTicks = 0; st.problemId = ""; st.prompt = "";
            st.misconceptionId = ""; st.onboarding = false; st.practiceStep = 0;
            SetDetent(2, false);
            ResetWorldForRun();
            SetScreen();
            MgfBridge.NotifyChanged();
        }

        void ResetRun()
        {
            runSerial++;
            rng = new System.Random(unchecked(Environment.TickCount * 31 + runSerial * 104729 + GetInstanceID()));
            course = PpittulRules.BuildCourse(rng);
            // 1→2→…→11을 판마다 한 칸씩 순환하는 무뇌 입력과 첫 문이 구조적으로
            // 같은 위상에 고정되지 않게 첫 두 원소만 교환한다. 목표 11개를 한 번씩 쓰는
            // 불변과 학생에게 보이는 균등 분포는 유지된다(케이블 록의 cycle derangement와 같은 원리).
            int cycleGuess = (runSerial - 1) % PpittulRules.DetentCount;
            if (course[0] == cycleGuess)
            {
                int swapAt = course[1] == cycleGuess ? 2 : 1;
                int t = course[0]; course[0] = course[swapAt]; course[swapAt] = t;
            }
            st.score = 0; st.lives = PpittulRules.StartLives; st.level = 1; st.solved = 0;
            st.firstAttemptTotal = 0; st.firstAttemptCorrect = 0; st.lastThreeFirstCorrect = 0;
            st.courseIndex = 0; st.presentedCount = 0; st.attemptIndex = 0;
            st.brokenNeedles = 0; st.detentTicks = 0; st.misconceptionId = "";
            playTime = 0; revealTime = 0; dragging = false; invalidPointer = false; keyboardGrab = false;
            endReason = ""; practiceStep = 0; idleGuide = 0;
            ResetWorldForRun();
        }

        void StartPractice()
        {
            ResetRun();
            flow = Flow.Practice;
            practiceStep = 0;
            st.onboarding = true; st.practiceStep = 0;
            current = PpittulRules.Practice(0);
            firstAttempt = true;
            SetDetent(2, false); // 60°에서 80°로 실제로 두 칸 민다.
            LoadProblemUi();
            ResetWorldForProblem();
            SetScreen();
            ReplayGuide(false);
            MgfSfx.Play("whoosh", .25f);
            MgfBridge.NotifyChanged();
        }

        void StartRunDirect()
        {
            ResetRun();
            flow = Flow.Playing;
            st.onboarding = false;
            NextProblem();
            SetScreen();
            MgfBridge.NotifyChanged();
        }

        void BeginMainAfterPractice()
        {
            st.score = 0; st.solved = 0; st.lives = PpittulRules.StartLives;
            st.firstAttemptTotal = 0; st.firstAttemptCorrect = 0; st.lastThreeFirstCorrect = 0;
            st.courseIndex = 0; st.presentedCount = 0; st.attemptIndex = 0; st.brokenNeedles = 0;
            st.misconceptionId = ""; st.onboarding = false; playTime = 0;
            flow = Flow.Playing;
            NextProblem();
            SetScreen();
        }

        void NextProblem()
        {
            if (st.solved >= PpittulRules.TargetCount)
            {
                bool mastered = st.firstAttemptCorrect >= PpittulRules.MasteryFirst && st.lastThreeFirstCorrect >= 2;
                EndRun(mastered ? "clear" : "mastery_failed");
                return;
            }
            int index = st.solved;
            int target = course[index];
            current = PpittulRules.MakeCourseProblem(target, index, rng, runSerial * 1000 + index * 20 + rng.Next(20));
            st.courseIndex = index; st.presentedCount++; st.attemptIndex = 0;
            st.level = current.band; st.problemId = current.id; st.prompt = current.prompt;
            st.misconceptionId = ""; firstAttempt = true; idleGuide = 0;

            // 정답과 독립인 시작 상태. 조건 판별 문항은 네 의미 있는 걸쇠 중
            // 오답 위치에서 시작해 화면의 측정값을 읽고 반드시 실제로 움직이게 한다.
            if (PpittulRules.IsPropertyProblem(current))
            {
                int[] propertyStarts = {
                    PpittulRules.AlwaysDetent, PpittulRules.RightAngleDetent,
                    PpittulRules.EqualSidesDetent, PpittulRules.BothDetent
                };
                int start = propertyStarts[(index + runSerial) % propertyStarts.Length];
                if (start == current.targetDetent)
                    start = propertyStarts[(index + runSerial + 1) % propertyStarts.Length];
                SetDetent(start, false);
            }
            else if (index == 0)
            {
                int start = rng.Next(PpittulRules.DetentCount - 1);
                if (start >= target) start++;
                SetDetent(start, false);
            }
            else if (st.selectedDetent == current.targetDetent)
            {
                // 이전 정답 위치를 그대로 두면 정답이 노출되고 실제 드래그 제출도 막히므로
                // 매 문항을 결정적인 비정답 걸쇠에서 시작한다.
                int start = (current.targetDetent + 2 + index) % PpittulRules.DetentCount;
                if (start == current.targetDetent) start = (start + 1) % PpittulRules.DetentCount;
                SetDetent(start, false);
            }
            LoadProblemUi();
            ResetWorldForProblem();
            MgfBridge.NotifyChanged();
        }

        void SetDetent(int detent, bool notify)
        {
            detent = PpittulRules.ClampDetent(detent);
            int before = st.selectedDetent;
            st.selectedDetent = detent;
            st.selectedAngle = PpittulRules.AngleForSelection(current, detent);
            SetGateDetent(detent, before != detent);
            if (before != detent)
            {
                st.detentTicks++;
                TickDetent();
            }
            if (notify) MgfBridge.NotifyChanged();
        }

        void SubmitGate()
        {
            if ((flow != Flow.Practice && flow != Flow.Playing) || current == null) return;
            revealSubmittedDetent = st.selectedDetent;
            revealCorrect = PpittulRules.IsCorrect(current, revealSubmittedDetent);
            revealWasFirst = firstAttempt;

            if (flow == Flow.Practice)
            {
                if (revealCorrect) st.score += 100;
                else
                {
                    st.misconceptionId = PpittulRules.IdentifyMisconception(current, revealSubmittedDetent);
                    firstAttempt = false;
                }
            }
            else
            {
                if (firstAttempt)
                {
                    st.firstAttemptTotal++;
                    if (revealCorrect)
                    {
                        st.firstAttemptCorrect++;
                        if (st.solved >= 8) st.lastThreeFirstCorrect++;
                    }
                }
                if (revealCorrect)
                {
                    st.score += revealWasFirst ? 100 : 20;
                    st.solved++;
                }
                else
                {
                    st.lives--;
                    st.brokenNeedles = PpittulRules.StartLives - Math.Max(0, st.lives);
                    st.attemptIndex++;
                    st.misconceptionId = PpittulRules.IdentifyMisconception(current, revealSubmittedDetent);
                    firstAttempt = false;
                }
            }

            flow = Flow.Reveal;
            revealTime = 0; dragging = false; keyboardGrab = false;
            BeginRevealVisual(revealCorrect, revealWasFirst, current.targetDetent, revealSubmittedDetent);
            RefreshHudImmediate();
            MgfBridge.NotifyChanged();
        }

        void FinishReveal()
        {
            if (flow != Flow.Reveal) return;
            CompleteRevealVisual(revealCorrect);
            if (current != null && current.band == 0)
            {
                if (!revealCorrect)
                {
                    flow = Flow.Practice;
                    SetDetent(practiceStep == 0 ? 2 : 4, false);
                    LoadProblemUi();
                    ReplayGuide(true);
                }
                else if (practiceStep == 0)
                {
                    practiceStep = 1; st.practiceStep = 1;
                    flow = Flow.Practice;
                    current = PpittulRules.Practice(1);
                    firstAttempt = true; st.misconceptionId = "";
                    SetDetent(4, false); // 80°에서 ∠B=70° → A=110°로 연결.
                    LoadProblemUi(); ResetWorldForProblem(); ReplayGuide(false);
                }
                else BeginMainAfterPractice();
            }
            else if (!revealCorrect)
            {
                if (st.lives <= 0) EndRun("lost");
                else
                {
                    flow = Flow.Playing;
                    ShowWrongReason(st.misconceptionId, current, revealSubmittedDetent);
                    LoadProblemUi();
                }
            }
            else
            {
                flow = Flow.Playing;
                NextProblem();
            }
            SetScreen();
            MgfBridge.NotifyChanged();
        }

        void FinishPendingReveal()
        {
            if (flow == Flow.Reveal) { revealTime = 99f; FinishReveal(); }
        }

        void EndRun(string reason)
        {
            if (flow == Flow.End) return;
            flow = Flow.End; endReason = reason; dragging = false; keyboardGrab = false;
            if (st.score > bestScore) { bestScore = st.score; PlayerPrefs.SetInt("ppittul-daemun.bestScore", bestScore); }
            if (st.firstAttemptCorrect > bestFirst) { bestFirst = st.firstAttemptCorrect; PlayerPrefs.SetInt("ppittul-daemun.bestFirst", bestFirst); }
            PlayerPrefs.Save();
            ShowEnd(reason);
            SetScreen();
            MgfSfx.Play(reason == "clear" ? "win" : "lose", .55f);
            MgfBridge.NotifyChanged();
        }

        void HandleInput()
        {
            HandleKeyboard();
            if (MgfPointer.Down)
            {
                st.pointerVersion++;
                SpawnTapRipple(MgfPointer.Position);
                invalidPointer = false;
                if (flow == Flow.Title)
                {
                    if (IsBellZone(MgfPointer.Position))
                    {
                        dragging = true; dragStart = MgfPointer.Position; PullBell(0);
                        MgfSfx.Play("tap", .22f);
                    }
                    else { invalidPointer = true; RefuseInput("가운데 황동 종끈을 아래로 당기시오"); }
                    MgfBridge.NotifyChanged(); return;
                }
                if (flow == Flow.End)
                {
                    if (IsRestartZone(MgfPointer.Position)) StartPractice();
                    else RefuseInput("무대 아래 종끈을 눌러 다시 시작하시오");
                    MgfBridge.NotifyChanged(); return;
                }
                if (flow == Flow.Reveal)
                {
                    RefuseInput("걸쇠 결과를 확인하는 중"); MgfBridge.NotifyChanged(); return;
                }
                if (flow != Flow.Practice && flow != Flow.Playing) return;
                if (IsHandleZone(MgfPointer.Position))
                {
                    dragging = true; dragStart = MgfPointer.Position; dragStartDetent = st.selectedDetent;
                    HandleGrab(true); idleGuide = 0;
                    MgfSfx.Play("tap", .22f);
                }
                else
                {
                    invalidPointer = true;
                    RefuseInput("B 모서리의 황동 손잡이를 잡아 수평으로 미시오");
                }
                MgfBridge.NotifyChanged();
            }

            if (MgfPointer.Held && dragging)
            {
                if (flow == Flow.Title) PullBell(Mathf.Max(0, dragStart.y - MgfPointer.Position.y));
                else if (flow == Flow.Practice || flow == Flow.Playing)
                    SetDetent(DetentFromScreen(MgfPointer.Position), true);
            }

            if (MgfPointer.Up && dragging)
            {
                dragging = false;
                if (flow == Flow.Title)
                {
                    ReleaseBell();
                    StartPractice();
                    return;
                }
                HandleGrab(false);
                if (Vector2.Distance(dragStart, MgfPointer.Position) < 12f && dragStartDetent == st.selectedDetent)
                {
                    RefuseInput("손잡이를 다른 걸쇠까지 민 뒤 놓으시오");
                    ReplayGuide(true); MgfBridge.NotifyChanged(); return;
                }
                SubmitGate(); return;
            }

            if (MgfPointer.Up && invalidPointer)
            {
                invalidPointer = false;
                if (flow == Flow.Title) ReplayBellGuide();
                else ReplayGuide(true);
                MgfBridge.NotifyChanged();
            }
        }

        void HandleKeyboard()
        {
            if (flow != Flow.Practice && flow != Flow.Playing) return;
            if (Input.GetKeyDown(KeyCode.LeftArrow)) { SetDetent(st.selectedDetent - 1, true); keyboardGrab = true; HandleGrab(true); }
            if (Input.GetKeyDown(KeyCode.RightArrow)) { SetDetent(st.selectedDetent + 1, true); keyboardGrab = true; HandleGrab(true); }
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                if (!keyboardGrab) { keyboardGrab = true; HandleGrab(true); MgfBridge.NotifyChanged(); }
                else { keyboardGrab = false; HandleGrab(false); SubmitGate(); }
            }
        }

        public void TestStart()
        {
            if (flow == Flow.Reveal) FinishPendingReveal();
            StartRunDirect();
        }

        public void TestAnswerCorrect()
        {
            FinishPendingReveal();
            if (flow != Flow.Playing || current == null) return;
            SetDetent(current.targetDetent, false);
            SubmitGate();
        }

        public void TestAnswerWrong()
        {
            FinishPendingReveal();
            if (flow != Flow.Playing || current == null) return;
            int wrong = current.wrongDetents != null && current.wrongDetents.Length > 0 ? current.wrongDetents[0] : (current.targetDetent + 1) % 11;
            SetDetent(wrong, false);
            SubmitGate();
        }

        public string StateJson()
        {
            st.phase = flow == Flow.Title ? "title" : flow == Flow.End ? (endReason == "clear" ? "clear" : "gameover") : "playing";
            st.onboarding = flow == Flow.Practice;
            st.practiceStep = practiceStep;
            st.lives = Math.Max(0, st.lives);
            st.problemId = current == null ? "" : current.id;
            st.prompt = current == null ? "" : current.prompt;
            return JsonUtility.ToJson(st);
        }

        public string ProblemBankJson() => MgfJson.Bank(bank);
    }
}
