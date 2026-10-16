// 자두 집게 — 타이틀, 무손실 연습, 10송이 본판, 실패/숙련 상태와 실제 포인터 입력.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.JaduJipge
{
    public partial class JaduJipgeGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int order;
            public int combo;
            public int firstAttemptTotal;
            public int firstAttemptCorrect;
            public int firstAttemptWrong;
            public int band3FirstCorrect;
            public int band3FirstWrong;
            public int attemptIndex;
            public int currentU;
            public int currentV;
            public int currentAngleIndex;
            public string touched = "000";
            public string problemId = "";
            public string prompt = "";
            public string misconceptionId = "";
            public string endReason = "";
            public int pointerVersion;
            public bool onboarding;
            public bool frozen;
            public float[] controlPx = new float[8];
        }

        enum GamePhase { Title, Practice, Playing, Reveal, End }

        readonly State st = new State();
        readonly List<MgfProblem> bank = new List<MgfProblem>();
        List<JaduProblem> deck;
        JaduProblem current;
        GamePhase gamePhase;
        System.Random rng;
        int runSerial;
        float timeLeft;
        float revealClock;
        float idleClock;
        float hitStopLeft;
        int dragControl;
        Vector2 dragStart;
        bool dragMoved;
        bool touchedU;
        bool touchedV;
        bool touchedAngle;
        bool firstAttempt;
        bool revealCorrect;
        bool revealWasPractice;
        string pendingEndReason = "";

        void Awake()
        {
            MgfLook.Quality(34f);
            int audit = JaduRules.AuditBank();
            if (audit != 0) Debug.LogError("[자두 집게] 오답 우연 일치 " + audit + "건");
            bank.AddRange(JaduRules.BuildBank());
            BuildWorld();
            BuildUi();
            Prewarm();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb = new StringBuilder(4096);
            sb.Append("자두집게집게로자두를따라중학교2학년도형의닮음가운데자두를눌러수확을시작하시오");
            sb.Append("견본과닮은집게가되도록두팔의길이를구하시오두변과그끼인각을맞추시오견본과집게의닮음비");
            sb.Append("수확안전패드남은시간첫시도후반숙련대응변기호순서SAS닮음조절완료다시수확");
            sb.Append("길이의차가아니라비가일정해야한다두변의비는맞지만끼인각이다르다처리중이다고정눈금레일");
            sb.Append("연습을다시해보시오ABDEACDF가각각같은비인지확인하시오올바른집게길이");
            for (int i = 0; i < Math.Min(36, bank.Count); i++)
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

            if (hitStopLeft > 0f) hitStopLeft = Mathf.Max(0f, hitStopLeft - dt);
            else if (gamePhase == GamePhase.Practice)
            {
                idleClock += dt;
                if (idleClock >= 8f)
                {
                    idleClock = 0f;
                    ReplayGuide(true);
                }
            }
            else if (gamePhase == GamePhase.Playing)
            {
                timeLeft = Mathf.Max(0f, timeLeft - dt);
                if (timeLeft <= 0f) EndRun("time");
                idleClock += dt;
                if (idleClock >= 9f)
                {
                    idleClock = 0f;
                    ReplayGuide(true);
                }
            }
            else if (gamePhase == GamePhase.Reveal)
            {
                revealClock += dt;
                float duration = revealWasPractice ? 1.65f : revealCorrect ? 1.45f : 1.18f;
                if (revealClock >= duration) FinishReveal();
            }

            AnimateWorld(dt);
            UpdateUi(dt);
            ApplyResponsiveLayout();
        }

        void ShowTitle()
        {
            gamePhase = GamePhase.Title;
            deck = null;
            current = null;
            rng = null;
            dragControl = 0;
            st.score = 0;
            st.lives = JaduRules.StartLives;
            st.level = 1;
            st.solved = 0;
            st.order = 0;
            st.combo = 0;
            st.firstAttemptTotal = 0;
            st.firstAttemptCorrect = 0;
            st.firstAttemptWrong = 0;
            st.band3FirstCorrect = 0;
            st.band3FirstWrong = 0;
            st.attemptIndex = 0;
            st.currentU = 2;
            st.currentV = 9;
            st.currentAngleIndex = 2;
            st.touched = "000";
            st.problemId = "";
            st.prompt = "";
            st.misconceptionId = "";
            st.endReason = "";
            st.pointerVersion = 0;
            st.onboarding = false;
            st.frozen = false;
            pendingEndReason = "";
            timeLeft = JaduRules.RunSeconds;
            ResetWorldForRun();
            ShowTitleVisual();
            SetScreen();
            MgfBridge.NotifyChanged();
        }

        void ResetRunState()
        {
            runSerial++;
            int seed = unchecked(Environment.TickCount ^ (runSerial * 104729));
            rng = new System.Random(seed);
            deck = JaduRules.RunDeck(seed);
            st.score = 0;
            st.lives = JaduRules.StartLives;
            st.level = 1;
            st.solved = 0;
            st.order = 0;
            st.combo = 0;
            st.firstAttemptTotal = 0;
            st.firstAttemptCorrect = 0;
            st.firstAttemptWrong = 0;
            st.band3FirstCorrect = 0;
            st.band3FirstWrong = 0;
            st.attemptIndex = 0;
            st.misconceptionId = "";
            st.endReason = "";
            st.onboarding = false;
            st.frozen = false;
            pendingEndReason = "";
            timeLeft = JaduRules.RunSeconds;
            revealClock = 0f;
            idleClock = 0f;
            dragControl = 0;
            ResetWorldForRun();
        }

        void StartPractice()
        {
            ResetRunState();
            gamePhase = GamePhase.Practice;
            current = JaduRules.Practice();
            st.onboarding = true;
            st.frozen = true;
            st.problemId = current.id;
            st.prompt = current.prompt;
            st.currentU = 2;
            st.currentV = 9;
            st.currentAngleIndex = current.angleIndex;
            firstAttempt = true;
            ResetTouches();
            ResetWorldForProblem(current);
            RefreshProblemUi();
            SetScreen();
            ReplayGuide(false);
            MgfSfx.Play("whoosh", .34f);
            MgfBridge.NotifyChanged();
        }

        void StartRunDirect()
        {
            ResetRunState();
            gamePhase = GamePhase.Playing;
            st.onboarding = false;
            st.frozen = false;
            LoadNextProblem();
            SetScreen();
            MgfBridge.NotifyChanged();
        }

        void BeginMainAfterPractice()
        {
            st.score = 0;
            st.solved = 0;
            st.order = 0;
            st.combo = 0;
            st.firstAttemptTotal = 0;
            st.firstAttemptCorrect = 0;
            st.firstAttemptWrong = 0;
            st.band3FirstCorrect = 0;
            st.band3FirstWrong = 0;
            st.lives = JaduRules.StartLives;
            st.onboarding = false;
            st.frozen = false;
            gamePhase = GamePhase.Playing;
            timeLeft = JaduRules.RunSeconds;
            LoadNextProblem();
            SetScreen();
            MgfBridge.NotifyChanged();
        }

        void LoadNextProblem()
        {
            if (st.solved >= JaduRules.TargetFruit)
            {
                EndRun(st.firstAttemptCorrect >= 8 && st.band3FirstCorrect >= 3 ? "clear" : "mastery_fail");
                return;
            }

            current = deck[st.solved];
            st.order = st.solved + 1;
            st.level = current.band;
            st.problemId = current.id;
            st.prompt = current.prompt;
            st.attemptIndex = 0;
            st.misconceptionId = "";
            firstAttempt = true;
            st.currentU = rng.Next(1, 13);
            st.currentV = rng.Next(1, 13);
            // 1·2단계는 각 손잡이를 숨기는 대신 견본의 끼인각으로 기계적으로 잠근다.
            // 3단계만 임의 각에서 시작해 학생이 직접 조절한다.
            st.currentAngleIndex = current.angleRequired ? rng.Next(0, JaduRules.AngleCount) : current.angleIndex;
            ResetTouches();
            ResetWorldForProblem(current);
            RefreshProblemUi();
            ReplayGuide(false);
            MgfBridge.NotifyChanged();
        }

        void ResetTouches()
        {
            touchedU = false;
            touchedV = false;
            touchedAngle = current != null && !current.angleRequired;
            SyncTouchedState();
        }

        void SyncTouchedState()
        {
            st.touched = (touchedU ? "1" : "0") + (touchedV ? "1" : "0") + (touchedAngle ? "1" : "0");
        }

        bool AllRequiredTouched()
        {
            return touchedU && touchedV && (current == null || !current.angleRequired || touchedAngle);
        }

        void HandleInput()
        {
            if (MgfPointer.Down)
            {
                st.pointerVersion++;
                idleClock = 0f;
                dragStart = MgfPointer.Position;
                dragMoved = false;
                SpawnPointerRipple(MgfPointer.Position);

                if (gamePhase == GamePhase.Title)
                {
                    dragControl = -1;
                    PressTitlePlum();
                    MgfBridge.NotifyChanged();
                    return;
                }
                if (gamePhase == GamePhase.End)
                {
                    StartRunDirect();
                    return;
                }
                if (gamePhase == GamePhase.Reveal)
                {
                    RefuseInput(MgfPointer.Position, "자두가 이동하는 동안 잠깐 기다리시오.");
                    MgfBridge.NotifyChanged();
                    return;
                }
                if (gamePhase != GamePhase.Practice && gamePhase != GamePhase.Playing) return;

                dragControl = PickControl(MgfPointer.Position, current != null && current.angleRequired);
                // 연습에서는 큰 집게 작업 영역 안의 드래그도 아직 만지지 않은 다음
                // 손잡이에 연결한다. 값은 포인터 투영으로 그대로 정해지며 정답 위치로
                // 스냅하지 않는다. 본판은 실제 손잡이 히트만 허용한다.
                if (gamePhase == GamePhase.Practice)
                {
                    int activePracticeControl = !touchedU ? 1 : !touchedV ? 2 : 0;
                    // 연습은 한 번에 한 손잡이만 활성화한다. 밝은 손잡이와 고스트 경로가
                    // 차지하는 아래 작업 영역 전체를 그 손잡이의 넉넉한 히트박스로 쓴다.
                    // 실제 포인터 입력과 눈금 투영은 유지하되 겹친 다른 손잡이가 입력을
                    // 가로채는 상황은 만들지 않는다.
                    bool inPracticeWorkArea = MgfPointer.Position.y < Screen.height * .68f;
                    dragControl = activePracticeControl > 0 && (dragControl == activePracticeControl || inPracticeWorkArea)
                        ? activePracticeControl : 0;
                }
                if (dragControl == 0)
                {
                    RefuseInput(MgfPointer.Position, "빛나는 집게 손잡이를 끌어 눈금에 놓으시오.");
                    ReplayGuide(true);
                    MgfBridge.NotifyChanged();
                }
                else
                {
                    BeginControlDrag(dragControl);
                    MgfSfx.Play("tap", .24f);
                }
            }

            if (MgfPointer.Held && dragControl > 0 && (gamePhase == GamePhase.Practice || gamePhase == GamePhase.Playing))
            {
                float moved = Vector2.Distance(dragStart, MgfPointer.Position);
                if (moved >= 10f) dragMoved = true;
                int before = dragControl == 1 ? st.currentU : dragControl == 2 ? st.currentV : st.currentAngleIndex;
                int after = DragControlValue(dragControl, MgfPointer.Position);
                if (after != before)
                {
                    if (dragControl == 1) st.currentU = after;
                    else if (dragControl == 2) st.currentV = after;
                    else st.currentAngleIndex = after;
                    UpdateClawVisual(true);
                    MgfSfx.Play("tap", .10f);
                    MgfBridge.NotifyChanged();
                }
                UpdateDragTrail(dragControl, MgfPointer.Position);
            }

            if (MgfPointer.Up && gamePhase == GamePhase.Title && dragControl == -1)
            {
                dragControl = 0;
                ReleaseTitlePlum();
                StartPractice();
                return;
            }

            if (MgfPointer.Up && dragControl > 0 && (gamePhase == GamePhase.Practice || gamePhase == GamePhase.Playing))
            {
                int released = dragControl;
                dragControl = 0;
                EndDragTrail();
                bool releasedOnTarget = released == 1 ? st.currentU == current.targetU
                    : released == 2 ? st.currentV == current.targetV
                    : st.currentAngleIndex == current.angleIndex;
                if (!dragMoved && gamePhase == GamePhase.Practice && !releasedOnTarget)
                {
                    // 고스트 목표를 누른 학생에게 10 px 우회 드래그를 요구하지 않는다.
                    // 활성 손잡이 탭은 보조 입력으로 목표 눈금에 놓고, 다음 손잡이로 넘긴다.
                    if (released == 1) st.currentU = current.targetU;
                    else if (released == 2) st.currentV = current.targetV;
                    else st.currentAngleIndex = current.angleIndex;
                    releasedOnTarget = true;
                    UpdateClawVisual(true);
                }
                if (!dragMoved && !releasedOnTarget)
                {
                    BounceControl(released);
                    RefuseInput(MgfPointer.Position, "밝은 손잡이를 목표 눈금까지 끌어 놓으시오.");
                    ReplayGuide(true);
                    MgfBridge.NotifyChanged();
                    return;
                }

                if (released == 1) touchedU = true;
                else if (released == 2) touchedV = true;
                else touchedAngle = true;
                SyncTouchedState();
                MarkAdjusted(released);
                UpdateClawVisual(false);
                MgfSfx.Play("pop", .24f);
                if (AllRequiredTouched()) SubmitClaw();
                else ReplayGuide(false);
                MgfBridge.NotifyChanged();
            }
        }

        void SubmitClaw()
        {
            if ((gamePhase != GamePhase.Practice && gamePhase != GamePhase.Playing) || current == null) return;
            revealWasPractice = gamePhase == GamePhase.Practice;
            revealCorrect = JaduRules.IsCorrect(current, st.currentU, st.currentV, st.currentAngleIndex);
            pendingEndReason = "";

            if (revealWasPractice)
            {
                if (revealCorrect)
                {
                    st.score = 80;
                    st.solved = 1;
                    st.misconceptionId = "";
                }
                else
                {
                    st.misconceptionId = JaduRules.MisconceptionId(current, st.currentU, st.currentV, st.currentAngleIndex);
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
                        if (current.band == 3) st.band3FirstCorrect++;
                    }
                    else
                    {
                        st.firstAttemptWrong++;
                        if (current.band == 3) st.band3FirstWrong++;
                    }
                }

                if (revealCorrect)
                {
                    st.combo = firstAttempt ? st.combo + 1 : 0;
                    st.score += firstAttempt ? 150 + st.combo * 12 : 70;
                    st.solved++;
                    st.misconceptionId = "";
                }
                else
                {
                    st.combo = 0;
                    st.lives = Math.Max(0, st.lives - 1);
                    st.attemptIndex++;
                    st.misconceptionId = JaduRules.MisconceptionId(current, st.currentU, st.currentV, st.currentAngleIndex);
                    firstAttempt = false;
                    if (st.lives <= 0) pendingEndReason = "pads";
                    else if (st.firstAttemptWrong >= 3) pendingEndReason = "mastery_lost";
                    else if (st.band3FirstWrong >= 2) pendingEndReason = "band3_lost";
                }
            }

            gamePhase = GamePhase.Reveal;
            revealClock = 0f;
            hitStopLeft = revealCorrect ? .075f : .105f;
            dragControl = 0;
            string evidence = revealCorrect ? CorrectEquation(current)
                : revealWasPractice ? PracticeCorrection(current)
                : JaduRules.Feedback(current, st.currentU, st.currentV, st.currentAngleIndex);
            BeginRevealVisual(revealCorrect, firstAttempt, revealWasPractice, evidence);
            SetScreen();
            MgfBridge.NotifyChanged();
        }

        string CorrectEquation(JaduProblem p)
        {
            string eq = p.sourceTriangle + "∽" + p.clawTriangle + "  ·  "
                + p.referenceU + ":" + p.targetU + "=" + p.referenceV + ":" + p.targetV + "=" + p.ratioQ + ":1";
            eq += "  ·  ∠A=∠" + p.jointLabel + "=" + p.angleDeg + "°";
            return eq;
        }

        string PracticeCorrection(JaduProblem p)
        {
            // 고정 연습의 오답 화면은 정답값만 던지지 않고 대응하는 두 비를
            // 같은 데이터로 연결한다. 이후 같은 문제로 돌아가 학생이 직접 고친다.
            return "AB:" + p.armULabel + "=AC:" + p.armVLabel + "=" + p.ratioQ + ":1\n"
                + p.armULabel + "=" + p.targetU + " cm  ·  " + p.armVLabel + "=" + p.targetV + " cm";
        }

        void FinishReveal()
        {
            if (gamePhase != GamePhase.Reveal) return;
            CompleteRevealVisual(revealCorrect);

            if (revealWasPractice)
            {
                if (revealCorrect)
                {
                    BeginMainAfterPractice();
                    return;
                }

                // 오답 연습은 통과권이 아니다. 같은 문제와 오답 형상을 유지한 채
                // 두 손잡이를 다시 만지게 하며 점수·진도·패드는 전혀 소비하지 않는다.
                gamePhase = GamePhase.Practice;
                st.score = 0;
                st.solved = 0;
                st.onboarding = true;
                st.frozen = true;
                ResetTouches();
                ResetWorldForProblem(current);
                RefreshProblemUi();
                SetScreen();
                ReplayGuide(true);
                MgfBridge.NotifyChanged();
                return;
            }

            if (!string.IsNullOrEmpty(pendingEndReason))
            {
                EndRun(pendingEndReason);
                return;
            }

            if (revealCorrect)
            {
                if (st.solved >= JaduRules.TargetFruit)
                    EndRun(st.firstAttemptCorrect >= 8 && st.band3FirstCorrect >= 3 ? "clear" : "mastery_fail");
                else
                {
                    gamePhase = GamePhase.Playing;
                    LoadNextProblem();
                    SetScreen();
                }
            }
            else
            {
                gamePhase = GamePhase.Playing;
                ResetTouches();
                RefreshProblemUi();
                SetScreen();
                ReplayGuide(true);
            }
            MgfBridge.NotifyChanged();
        }

        void EndRun(string reason)
        {
            if (gamePhase == GamePhase.End) return;
            gamePhase = GamePhase.End;
            dragControl = 0;
            st.endReason = reason;
            int best = PlayerPrefs.GetInt("jadu-jipge.best", 0);
            if (st.score > best)
            {
                PlayerPrefs.SetInt("jadu-jipge.best", st.score);
                PlayerPrefs.Save();
            }
            ShowEnd(reason);
            SetScreen();
            MgfSfx.Play(reason == "clear" ? "win" : "lose", .58f);
            MgfBridge.NotifyChanged();
        }

        public void TestStart()
        {
            if (gamePhase == GamePhase.Reveal)
            {
                revealClock = 99f;
                FinishReveal();
            }
            StartRunDirect();
        }

        public void TestAnswerCorrect()
        {
            if (gamePhase == GamePhase.Reveal)
            {
                revealClock = 99f;
                FinishReveal();
            }
            if (gamePhase != GamePhase.Playing || current == null) StartRunDirect();
            st.currentU = current.targetU;
            st.currentV = current.targetV;
            st.currentAngleIndex = current.angleIndex;
            touchedU = touchedV = touchedAngle = true;
            SyncTouchedState();
            UpdateClawVisual(false);
            SubmitClaw();
        }

        public void TestAnswerWrong()
        {
            if (gamePhase == GamePhase.Reveal)
            {
                revealClock = 99f;
                FinishReveal();
            }
            if (gamePhase != GamePhase.Playing || current == null) StartRunDirect();
            st.currentU = current.targetU == 12 ? 11 : current.targetU + 1;
            st.currentV = current.targetV;
            st.currentAngleIndex = (current.angleIndex + 1) % JaduRules.AngleCount;
            touchedU = touchedV = touchedAngle = true;
            SyncTouchedState();
            UpdateClawVisual(false);
            SubmitClaw();
        }

        public string StateJson()
        {
            if (gamePhase == GamePhase.Title) st.phase = "title";
            else if (gamePhase == GamePhase.End)
                st.phase = st.endReason == "clear" ? "clear" : st.endReason == "mastery_fail" ? "mastery_fail" : "gameover";
            else st.phase = "playing";

            st.onboarding = gamePhase == GamePhase.Practice;
            st.frozen = gamePhase == GamePhase.Practice;
            st.lives = Math.Max(0, st.lives);
            st.level = current == null ? 1 : Math.Max(1, current.band);
            st.problemId = current == null ? "" : current.id;
            st.prompt = current == null ? "" : current.prompt;
            FillControlPixels(st.controlPx);
            return JsonUtility.ToJson(st);
        }

        public string ProblemBankJson() => MgfJson.Bank(bank);
    }
}
