// 꿀 도둑 — 상태 머신과 실제 눈금 칼 포인터 입력.
// 손잡이를 눈금으로 옮긴 뒤 직교 방향으로 베는 한 동작만 답 제출이 된다.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.KkulDoduk
{
    public partial class KkulDodukGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int selectedCellCount;
            public string currentProblemId = "";
            public string prompt = "";
            public int firstCorrect;
            public int firstAttempted;
            public int firstAccuracy;
            public int attempts;
            public bool cutLocked;
            public int pointerVersion;
            public string misconceptionId = "";
            public int remainingSec = 120;
            public bool onboarding;
            public bool layoutLand;
        }

        enum GamePhase { Title, Practice, Playing, Reveal, End }

        readonly State st = new State();
        readonly List<MgfProblem> bank = new List<MgfProblem>();
        List<HoneyProblem> deck;
        HoneyProblem current;
        GamePhase phase = GamePhase.Title;
        int runSerial;
        int problemIndex;
        bool firstSubmission;
        bool revealCorrect;
        bool revealPractice;
        bool dragging;
        bool assistedPracticeDrag;
        bool practicePrimed;
        bool titleOpening;
        float titleClock;
        float revealClock;
        float sessionLeft = KkulDodukRules.SessionSeconds;
        float idleGuide;
        float lastAxisChange;
        Vector2 dragStart;
        string endReason = "";

        void Awake()
        {
            MgfLook.Quality(32f);
            string validationError;
            if (!KkulDodukRules.ValidateAll(out validationError))
                Debug.LogError("[꿀 도둑] 문제 풀 검증 실패: " + validationError);
            bank.AddRange(KkulDodukRules.BuildBank());
            Prewarm();
            BuildWorld();
            BuildUi();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb = new StringBuilder("꿀도둑밀랍벌통시식벌중학교2학년경우의수한종류만각각하나씩꽃꿀과일꿀용기선반눈금칼절단봉인첫절단정확도연습의뢰고치시오합의법칙곱의법칙정답오답다시시작");
            for (int i = 0; i < bank.Count; i++)
            {
                sb.Append(bank[i].prompt); sb.Append(bank[i].answer); sb.Append(bank[i].unitConcept);
            }
            MgfText.Prewarm(sb.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(.05f, Time.unscaledDeltaTime);
            bool changedLayout = UpdateLayoutIfNeeded();
            if (changedLayout && dragging)
            {
                dragging = false; st.cutLocked = false;
                RefuseAt(MgfPointer.Position, "화면 방향이 바뀌어 절단을 취소했다. 손잡이를 다시 잡으시오.");
                Snapshot(); MgfBridge.NotifyChanged();
            }
            HandleInput();

            if (titleOpening)
            {
                titleClock += dt;
                if (titleClock >= .38f) { titleOpening = false; StartPractice(); }
            }
            else if (phase == GamePhase.Playing)
            {
                sessionLeft -= dt; idleGuide += dt;
                if (sessionLeft <= 0f) EndRun(false, "120초가 끝나 의뢰를 마치지 못했다");
                else if (idleGuide >= 8f) { idleGuide = 0f; BoostGuide(); }
            }
            else if (phase == GamePhase.Practice)
            {
                idleGuide += dt;
                if (idleGuide >= 8f) { idleGuide = 0f; BoostGuide(); }
            }
            else if (phase == GamePhase.Reveal)
            {
                revealClock += dt;
                if (revealClock >= (revealCorrect ? 2.1f : 1.65f)) FinishReveal();
            }
            UpdateWorldVisual(dt);
            UpdateUiVisual(dt);
        }

        void ShowTitle()
        {
            phase = GamePhase.Title; titleOpening = false; titleClock = 0f; current = null; deck = null;
            st.score = 0; st.lives = KkulDodukRules.StartLives; st.level = 1; st.solved = 0; st.phase = "title";
            st.selectedCellCount = 1; st.currentProblemId = ""; st.prompt = ""; st.firstCorrect = 0;
            st.firstAttempted = 0; st.firstAccuracy = 0; st.attempts = 0; st.cutLocked = false;
            st.pointerVersion = 0; st.misconceptionId = ""; st.remainingSec = (int)KkulDodukRules.SessionSeconds;
            st.onboarding = false; st.layoutLand = IsLand(); endReason = "";
            ResetWorldForRun(); ShowTitleVisual(); SetScreen(); MgfBridge.NotifyChanged();
        }

        void ResetRun()
        {
            runSerial++; deck = KkulDodukRules.RunDeck(runSerial); problemIndex = 0;
            st.score = 0; st.lives = KkulDodukRules.StartLives; st.level = 1; st.solved = 0;
            st.firstCorrect = 0; st.firstAttempted = 0; st.firstAccuracy = 0; st.attempts = 0;
            st.cutLocked = false; st.misconceptionId = ""; st.remainingSec = (int)KkulDodukRules.SessionSeconds;
            sessionLeft = KkulDodukRules.SessionSeconds; dragging = false; idleGuide = 0f; endReason = "";
            ResetWorldForRun();
        }

        void StartPractice()
        {
            ResetRun(); phase = GamePhase.Practice; st.phase = "playing"; st.onboarding = true;
            current = KkulDodukRules.Practice(); problemIndex = -1; firstSubmission = true;
            BeginProblem(1); SetScreen(); BoostGuide(); MgfSfx.Play("whoosh", .26f); MgfBridge.NotifyChanged();
        }

        void StartRunDirect()
        {
            ResetRun(); phase = GamePhase.Playing; st.phase = "playing"; st.onboarding = false;
            problemIndex = 0; LoadProblem(); SetScreen(); MgfBridge.NotifyChanged();
        }

        void StartMainAfterPractice()
        {
            // 연습 기록과 점수는 본판에 섞지 않는다.
            st.score = 0; st.lives = KkulDodukRules.StartLives; st.level = 1; st.solved = 0;
            st.firstCorrect = 0; st.firstAttempted = 0; st.firstAccuracy = 0; st.attempts = 0;
            st.misconceptionId = ""; st.onboarding = false; sessionLeft = KkulDodukRules.SessionSeconds;
            st.remainingSec = (int)sessionLeft; problemIndex = 0; phase = GamePhase.Playing; st.phase = "playing";
            LoadProblem();
        }

        void LoadProblem()
        {
            if (st.solved >= KkulDodukRules.Goal)
            {
                bool mastery = st.firstCorrect >= KkulDodukRules.RequiredFirstCorrect;
                EndRun(mastery, mastery ? "8회 시식을 완성했다" : "첫 절단 정답이 6회에 미치지 못했다");
                return;
            }
            problemIndex = st.solved;
            current = deck[problemIndex]; firstSubmission = true;
            int start = KkulDodukRules.InitialSelection(current, problemIndex);
            BeginProblem(start); MgfBridge.NotifyChanged();
        }

        void BeginProblem(int selected)
        {
            st.currentProblemId = current.id; st.prompt = current.prompt; st.level = current.band;
            st.selectedCellCount = Mathf.Clamp(selected, 1, 24); st.attempts = 0; st.cutLocked = false;
            st.misconceptionId = ""; Snapshot(); dragging = false; idleGuide = 0f;
            assistedPracticeDrag = false; practicePrimed = false;
            StartProblemVisual(current, phase == GamePhase.Practice, st.selectedCellCount);
            RefreshHud();
        }

        void HandleInput()
        {
            if (MgfPointer.Down)
            {
                st.pointerVersion++; MgfBridge.NotifyChanged();
                if (phase == GamePhase.Title)
                {
                    RippleAt(MgfPointer.Position);
                    if (!titleOpening && HitTitleSeal(MgfPointer.Position))
                    {
                        titleOpening = true; titleClock = 0f; BeginSealSplit(); MgfSfx.Play("tap", .28f);
                    }
                    else if (!titleOpening) RefuseAt(MgfPointer.Position, "가운데 밀랍 봉인을 누르시오.");
                    return;
                }
                if (phase == GamePhase.End)
                {
                    RippleAt(MgfPointer.Position);
                    if (HitRestart(MgfPointer.Position)) StartPractice();
                    else RefuseAt(MgfPointer.Position, "밀랍 봉인을 눌러 다시 시작하시오.");
                    return;
                }
                if (phase == GamePhase.Reveal)
                {
                    RippleAt(MgfPointer.Position); RefuseAt(MgfPointer.Position, "시식 벌의 확인이 끝날 때까지 기다리시오.");
                    ReactBees(false); return;
                }
                if (phase != GamePhase.Practice && phase != GamePhase.Playing) return;

                RippleAt(MgfPointer.Position);
                if (phase == GamePhase.Practice && practicePrimed &&
                    Vector2.Distance(MgfPointer.Position, HandleScreenPosition(current.answer)) >= 44f * ReferenceScale())
                {
                    practicePrimed = false; st.selectedCellCount = current.answer;
                    MoveHandleVisual(st.selectedCellCount); SubmitCut(st.selectedCellCount);
                    return;
                }
                bool hitHandle = HitHandle(MgfPointer.Position);
                bool hitPracticeLane = phase == GamePhase.Practice && PracticeTrackHit(MgfPointer.Position);
                bool hitFirstMainLane = phase == GamePhase.Playing && st.solved == 0 && st.firstAttempted == 0 &&
                    FirstMainTrackHit(MgfPointer.Position);
                if (hitHandle || hitPracticeLane || hitFirstMainLane)
                {
                    assistedPracticeDrag = hitPracticeLane && !hitHandle;
                    if (assistedPracticeDrag)
                    {
                        st.selectedCellCount = current.answer;
                        MoveHandleVisual(st.selectedCellCount);
                        RefuseAt(MgfPointer.Position, "좋다. 5칸 눈금에서 띠를 가로질러 베시오.");
                    }
                    else if (hitFirstMainLane && !hitHandle)
                    {
                        // 정답으로 보정하지 않는다. 누른 위치의 실제 눈금부터 조작을 시작한다.
                        st.selectedCellCount = PointerToValue(MgfPointer.Position);
                        MoveHandleVisual(st.selectedCellCount);
                        RefuseAt(MgfPointer.Position, "좋다. 손잡이를 눈금에 맞춘 뒤 띠를 가로질러 베시오.");
                    }
                    dragging = true; st.cutLocked = true; dragStart = MgfPointer.Position;
                    lastAxisChange = Time.unscaledTime; idleGuide = 0f;
                    BeginHandleDrag(); MgfSfx.Play("tap", .18f); Snapshot(); MgfBridge.NotifyChanged();
                }
                else
                {
                    RefuseAt(MgfPointer.Position, "큰 분홍 손잡이를 잡아 눈금으로 옮기시오.");
                    PointToHandle(); ReactBees(false);
                }
            }

            if (dragging && MgfPointer.Held)
            {
                int next = assistedPracticeDrag ? current.answer : PointerToValue(MgfPointer.Position);
                if (next != st.selectedCellCount)
                {
                    st.selectedCellCount = next; lastAxisChange = Time.unscaledTime;
                    MoveHandleVisual(next); MgfSfx.Play("pop", .08f); Snapshot(); MgfBridge.NotifyChanged();
                }
                UpdateCutTrail(dragStart, MgfPointer.Position, false);
            }

            if (dragging && MgfPointer.Up)
            {
                Vector2 now = MgfPointer.Position; dragging = false; st.cutLocked = false;
                float stable = Time.unscaledTime - lastAxisChange;
                bool practiceAssist = phase == GamePhase.Practice && assistedPracticeDrag;
                float cross = IsLand()
                    ? (practiceAssist ? Mathf.Abs(dragStart.y - now.y) : dragStart.y - now.y)
                    : Mathf.Abs(now.x - dragStart.x);
                float threshold = (practiceAssist ? 44f : 64f) * ReferenceScale();
                bool cut = cross >= threshold && stable >= .05f;
                UpdateCutTrail(dragStart, now, true);
                assistedPracticeDrag = false;
                if (cut) SubmitCut(st.selectedCellCount);
                else
                {
                    if (practiceAssist)
                    {
                        practicePrimed = true;
                        st.selectedCellCount = current.answer; MoveHandleVisual(st.selectedCellCount);
                        RefuseAt(now, "5칸에 맞췄다. 분홍 경로 끝을 한 번 더 눌러 베시오.");
                        BoostGuide(); Snapshot(); MgfBridge.NotifyChanged();
                        return;
                    }
                    CancelHandleDrag();
                    string why = stable < .05f ? "눈금에서 잠깐 멈춘 뒤 띠를 가로질러 베시오." :
                        (IsLand() ? "손잡이를 아래로 그어 벌집을 베시오." : "손잡이를 옆으로 그어 벌집을 베시오.");
                    RefuseAt(now, why); BoostGuide(); Snapshot(); MgfBridge.NotifyChanged();
                }
            }
        }

        void SubmitCut(int submitted)
        {
            if ((phase != GamePhase.Practice && phase != GamePhase.Playing) || current == null) return;
            bool practice = phase == GamePhase.Practice;
            bool correct = submitted == current.answer;
            revealPractice = practice; revealCorrect = correct; revealClock = 0f;
            st.attempts++; st.misconceptionId = correct ? "" : KkulDodukRules.Diagnose(current, submitted);

            if (!practice)
            {
                if (firstSubmission)
                {
                    st.firstAttempted++;
                    if (correct) st.firstCorrect++;
                    firstSubmission = false;
                }
                st.firstAccuracy = st.firstAttempted == 0 ? 0 : Mathf.RoundToInt(100f * st.firstCorrect / st.firstAttempted);
            }

            if (correct)
            {
                if (!practice)
                {
                    st.solved++; st.score += 120 + current.band * 30 + Mathf.Min(4, st.firstCorrect) * 10;
                }
                phase = GamePhase.Reveal; Snapshot();
                BeginCorrectReveal(current, submitted, practice); MgfSfx.Play("correct", .42f);
            }
            else
            {
                if (!practice) st.lives = Mathf.Max(0, st.lives - 1);
                phase = GamePhase.Reveal; Snapshot();
                BeginWrongReveal(current, submitted, st.misconceptionId, practice); MgfSfx.Play("wrong", .34f);
                if (!practice && st.lives <= 0) endReason = "세 개의 밀랍 봉인을 모두 잃었다";
            }
            st.phase = "playing"; RefreshHud(); SetScreen(); MgfBridge.NotifyChanged();
        }

        void FinishReveal()
        {
            if (phase != GamePhase.Reveal) return;
            CompleteRevealVisual(revealCorrect);
            if (revealCorrect)
            {
                if (revealPractice) StartMainAfterPractice();
                else { phase = GamePhase.Playing; LoadProblem(); }
            }
            else
            {
                if (!revealPractice && st.lives <= 0) EndRun(false, endReason);
                else
                {
                    phase = revealPractice ? GamePhase.Practice : GamePhase.Playing;
                    st.phase = "playing"; st.cutLocked = false; Snapshot(); idleGuide = 0f;
                    ResumeProblemAfterWrong(); BoostGuide(); RefreshHud(); MgfBridge.NotifyChanged();
                }
            }
            SetScreen();
        }

        void EndRun(bool clear, string reason)
        {
            if (phase == GamePhase.End) return;
            phase = GamePhase.End; st.phase = clear ? "clear" : "gameover"; st.cutLocked = false;
            st.onboarding = false; endReason = reason; Snapshot(); dragging = false;
            ShowEndVisual(clear, reason); SetScreen(); MgfBridge.NotifyChanged();
            MgfSfx.Play(clear ? "win" : "lose", .46f);
        }

        void Snapshot()
        {
            st.remainingSec = Mathf.Max(0, Mathf.CeilToInt(sessionLeft));
            st.layoutLand = IsLand();
        }

        bool IsLand() { return Screen.width >= 1024 || (Screen.width >= Screen.height && Screen.width >= 640); }

        float ReferenceScale()
        {
            return Mathf.Sqrt(Mathf.Max(.25f, (Screen.width * Screen.height) / (390f * 844f)));
        }

        Vector2 HandleScreenPosition(int value)
        {
            float t = (Mathf.Clamp(value, 1, 24) - 1) / 23f;
            return IsLand()
                ? new Vector2(Screen.width * Mathf.Lerp(.22f, .78f, t), Screen.height * .36f)
                : new Vector2(Screen.width * .50f, Screen.height * Mathf.Lerp(.22f, .67f, t));
        }

        int PointerToValue(Vector2 screen)
        {
            float t = IsLand()
                ? Mathf.InverseLerp(Screen.width * .22f, Screen.width * .78f, screen.x)
                : Mathf.InverseLerp(Screen.height * .22f, Screen.height * .67f, screen.y);
            return Mathf.Clamp(Mathf.RoundToInt(t * 23f) + 1, 1, 24);
        }

        bool HitHandle(Vector2 screen)
        {
            return Vector2.Distance(screen, HandleScreenPosition(st.selectedCellCount)) <= 48f * ReferenceScale();
        }

        bool PracticeTrackHit(Vector2 screen)
        {
            if (IsLand())
                return screen.x >= Screen.width * .10f && screen.x <= Screen.width * .90f &&
                    screen.y >= Screen.height * .18f && screen.y <= Screen.height * .62f;
            return screen.y >= Screen.height * .16f && screen.y <= Screen.height * .76f &&
                screen.x >= Screen.width * .10f && screen.x <= Screen.width * .90f;
        }

        bool FirstMainTrackHit(Vector2 screen)
        {
            if (IsLand())
                return screen.x >= Screen.width * .14f && screen.x <= Screen.width * .86f &&
                    screen.y >= Screen.height * .20f && screen.y <= Screen.height * .58f;
            return screen.y >= Screen.height * .18f && screen.y <= Screen.height * .72f &&
                screen.x >= Screen.width * .12f && screen.x <= Screen.width * .88f;
        }

        bool HitTitleSeal(Vector2 screen)
        {
            Vector2 c = new Vector2(Screen.width * .5f, Screen.height * (IsLand() ? .34f : .41f));
            return Vector2.Distance(screen, c) <= 82f * ReferenceScale();
        }

        bool HitRestart(Vector2 screen)
        {
            Vector2 c = new Vector2(Screen.width * .5f, Screen.height * (IsLand() ? .30f : .23f));
            return Vector2.Distance(screen, c) <= 88f * ReferenceScale();
        }

        public void TestStart() { StartRunDirect(); }

        public void TestAnswerCorrect()
        {
            if (phase != GamePhase.Practice && phase != GamePhase.Playing) StartRunDirect();
            st.selectedCellCount = current.answer; MoveHandleVisual(st.selectedCellCount); SubmitCut(st.selectedCellCount);
        }

        public void TestAnswerWrong()
        {
            if (phase != GamePhase.Practice && phase != GamePhase.Playing) StartRunDirect();
            int wrong = current.distractorA;
            if (wrong < 1 || wrong > 24 || wrong == current.answer) wrong = current.distractorB;
            if (wrong < 1 || wrong > 24 || wrong == current.answer) wrong = current.answer == 24 ? 23 : current.answer + 1;
            st.selectedCellCount = wrong; MoveHandleVisual(wrong); SubmitCut(wrong);
        }

        public string StateJson() { return JsonUtility.ToJson(st); }
        public string ProblemBankJson() { return MgfJson.Bank(bank); }
    }
}
