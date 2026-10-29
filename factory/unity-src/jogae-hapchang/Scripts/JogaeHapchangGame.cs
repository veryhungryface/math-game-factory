// 조개 합창 — 상태 기계, 실제 포인터 회전 입력, 브리지 계약.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.JogaeHapchang
{
    public partial class JogaeHapchangGame : MonoBehaviour, IMgfGame
    {
        // GameObject.CreatePrimitive(Sphere)가 WebGL 스트리핑 뒤에도 콜라이더 타입을 찾도록 보존한다.
        static readonly Type PreserveSphereColliderType = typeof(SphereCollider);
        [Serializable]
        sealed class State : MgfState
        {
            public string mode = "title";
            public string currentProblemId = "";
            public string prompt = "";
            public string problemType = "";
            public int selectedSlot;
            public int selectedAngle;
            public int firstCorrect;
            public int firstAttempted;
            public int firstAttempts;
            public int remainingUnattempted = JogaeRules.TargetSolved;
            public int masteryTarget = JogaeRules.MasteryFirstCorrect;
            public int allowedFirstMistakes = JogaeRules.AllowedFirstMistakes;
            public int lastSubmittedSlot = -1;
            public int lastSubmittedAngle;
            public string lastResult = "";
            public string misconceptionId = "";
            public int attemptCount;
            public bool practiceDone;
            public bool onboarding;
            public bool dragging;
            public int inputRevision;
            public float pivotX;
            public float pivotY;
            public float anchorX;
            public float anchorY;
            public float handleX;
            public float handleY;
        }

        enum Flow { Title, Opening, Practice, Playing, Bridge, Reveal, End }

        readonly State st = new State();
        readonly List<MgfProblem> bank = new List<MgfProblem>();
        List<ShellProblem> deck;
        ShellProblem current;
        Flow flow = Flow.Title;
        int runSerial;
        bool firstAttempt;
        bool revealCorrect;
        bool revealPractice;
        bool bridgeShown;
        string endReason = "";
        float sessionLeft;
        float revealClock;
        float openingClock;
        float idleGuide;
        Vector2 dragStart;
        bool practiceFreeGrab;
        bool practiceTapArmed;
        bool practiceAssistMoving;
        float practiceAssistClock;

        void Awake()
        {
            string stage = "quality";
            try
            {
                MgfLook.Quality(35f);
                stage = "bank"; bank.AddRange(JogaeRules.BuildBank());
                stage = "world"; BuildWorld();
                stage = "ui"; BuildUi();
                stage = "font"; PrewarmFont();
                stage = "title"; ShowTitle();
                stage = "bridge"; MgfBridge.Register(this);
            }
            catch (Exception ex)
            {
                Debug.LogError("[조개 합창 부팅 실패:" + stage + "] " + ex);
                throw;
            }
        }

        void PrewarmFont()
        {
            var sb = new StringBuilder("조개합창입을맞게벌리면조개들이노래한다중학교3학년원주각중심각호완성조건구성하시오고치시오첫시도숨소절정답오답다시합창하기윗입술을움직이시오소리를듣는중");
            for (int i = 0; i < bank.Count; i++) { sb.Append(bank[i].prompt); sb.Append(bank[i].answer); sb.Append(bank[i].unitConcept); }
            MgfText.Prewarm(sb.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(.05f, Time.deltaTime);
            HandleInput();
            if (practiceAssistMoving) UpdatePracticeAssist(dt);
            if (flow == Flow.Opening)
            {
                openingClock += dt;
                if (openingClock >= .72f) StartPractice();
            }
            else if (flow == Flow.Practice)
            {
                idleGuide += dt;
                if (idleGuide >= 8f) { idleGuide = 0f; BoostGuide(); }
            }
            else if (flow == Flow.Playing)
            {
                sessionLeft -= dt;
                if (sessionLeft <= 0f) EndRun("timer");
            }
            else if (flow == Flow.Reveal)
            {
                revealClock += dt;
                float duration = revealCorrect ? 1.35f : 1.05f;
                if (revealClock >= duration) FinishReveal();
            }
            UpdateWorld(dt);
            UpdateUi(dt);
        }

        void ShowTitle()
        {
            flow = Flow.Title;
            st.score = 0; st.lives = JogaeRules.StartLives; st.level = 1; st.solved = 0;
            st.mode = "title"; st.currentProblemId = ""; st.prompt = ""; st.problemType = "";
            st.selectedSlot = 0; st.selectedAngle = JogaeRules.MinAngle;
            st.firstCorrect = 0; st.firstAttempted = 0; st.firstAttempts = 0;
            st.remainingUnattempted = JogaeRules.TargetSolved; st.lastSubmittedSlot = -1;
            st.lastSubmittedAngle = 0; st.lastResult = ""; st.misconceptionId = "";
            st.attemptCount = 0; st.practiceDone = false; st.onboarding = false; st.dragging = false;
            st.masteryTarget = JogaeRules.MasteryFirstCorrect; st.allowedFirstMistakes = JogaeRules.AllowedFirstMistakes;
            current = null; deck = null; endReason = ""; sessionLeft = JogaeRules.SessionSeconds;
            ResetRunVisuals();
            SetScreen();
            MgfBridge.NotifyChanged();
        }

        void ResetRun()
        {
            runSerial++;
            deck = JogaeRules.RunDeck(runSerial);
            st.score = 0; st.lives = JogaeRules.StartLives; st.level = 1; st.solved = 0;
            st.firstCorrect = 0; st.firstAttempted = 0; st.firstAttempts = 0;
            st.remainingUnattempted = JogaeRules.TargetSolved; st.lastSubmittedSlot = -1;
            st.lastSubmittedAngle = 0; st.lastResult = ""; st.misconceptionId = "";
            st.attemptCount = 0; st.dragging = false; endReason = "";
            st.masteryTarget = JogaeRules.MasteryFirstCorrect; st.allowedFirstMistakes = JogaeRules.AllowedFirstMistakes;
            bridgeShown = false; practiceTapArmed = false; practiceAssistMoving = false; practiceAssistClock = 0f;
            sessionLeft = JogaeRules.SessionSeconds; revealClock = 0f; idleGuide = 0f;
            ResetRunVisuals();
        }

        void BeginOpening()
        {
            if (flow == Flow.Opening) return;
            ResetRun();
            flow = Flow.Opening; openingClock = 0f; st.mode = "opening";
            BeginTitleSplit(); SetScreen(); MgfSfx.Play("whoosh", .34f); MgfBridge.NotifyChanged();
        }

        void StartPractice()
        {
            flow = Flow.Practice; st.onboarding = true; st.practiceDone = false; idleGuide = 0f;
            current = JogaeRules.Direct(JogaeRules.SlotForAngle(40));
            current.id = "practice-center80-angle40";
            current.prompt = "연습: 중심각 80°가 되도록 ∠APB를 구성하시오.";
            firstAttempt = true; BeginProblem(true); SetScreen(); BoostGuide(); MgfBridge.NotifyChanged();
        }

        void StartRunDirect()
        {
            ResetRun();
            st.practiceDone = true; st.onboarding = false; flow = Flow.Playing;
            NextProblem(); SetScreen(); MgfBridge.NotifyChanged();
        }

        void BeginMainAfterPractice()
        {
            st.score = 0; st.solved = 0; st.firstCorrect = 0; st.firstAttempted = 0; st.firstAttempts = 0;
            st.remainingUnattempted = JogaeRules.TargetSolved; st.lives = JogaeRules.StartLives;
            st.lastResult = ""; st.misconceptionId = ""; st.attemptCount = 0;
            st.allowedFirstMistakes = JogaeRules.AllowedFirstMistakes;
            st.practiceDone = true; st.onboarding = false; sessionLeft = JogaeRules.SessionSeconds;
            flow = Flow.Playing; NextProblem(); SetScreen(); MgfBridge.NotifyChanged();
        }

        void NextProblem()
        {
            if (st.solved >= JogaeRules.TargetSolved)
            {
                if (st.firstCorrect >= JogaeRules.MasteryFirstCorrect) EndRun("clear");
                else EndRun("mastery");
                return;
            }
            if (st.solved == 3 && !bridgeShown)
            {
                BeginInvariantBridge();
                return;
            }
            current = deck[st.solved]; firstAttempt = true; BeginProblem(false); MgfBridge.NotifyChanged();
        }

        void BeginInvariantBridge()
        {
            flow = Flow.Bridge; st.dragging = false; st.currentProblemId = "same-arc-bridge";
            st.problemType = "SameArcBridge"; st.prompt = "점 P를 원 둘레로 옮겨도 같은 호 AB에 대한 ∠APB는 40°입니다.";
            PrepareInvariantBridge(); SetScreen(); MgfBridge.NotifyChanged();
        }

        void CompleteInvariantBridge()
        {
            bridgeShown = true; st.dragging = false; flow = Flow.Playing;
            RestoreAfterInvariantBridge(); NextProblem(); SetScreen(); MgfBridge.NotifyChanged();
        }

        void BeginProblem(bool practice)
        {
            st.currentProblemId = current.id; st.prompt = current.prompt; st.problemType = current.kind.ToString();
            st.level = current.band; st.selectedSlot = -1; st.selectedAngle = 0;
            st.lastSubmittedSlot = -1; st.lastSubmittedAngle = 0; st.lastResult = "";
            st.misconceptionId = ""; st.attemptCount = 0; st.dragging = false;
            practiceTapArmed = false; practiceAssistMoving = false; practiceAssistClock = 0f;
            PrepareProblemVisual(current, practice); RefreshProblemUi();
        }

        void Submit(int slot)
        {
            if ((flow != Flow.Practice && flow != Flow.Playing) || current == null) return;
            slot = Mathf.Clamp(slot, 0, JogaeRules.SlotCount - 1);
            revealPractice = flow == Flow.Practice;
            revealCorrect = slot == current.answerSlot;
            st.lastSubmittedSlot = slot; st.lastSubmittedAngle = JogaeRules.AngleForSlot(slot);
            st.lastResult = revealCorrect ? "correct" : "wrong"; st.attemptCount++;
            if (!revealPractice && firstAttempt)
            {
                st.firstAttempted++; st.firstAttempts = st.firstAttempted;
                if (revealCorrect) st.firstCorrect++;
                st.remainingUnattempted = JogaeRules.TargetSolved - st.firstAttempted;
                st.allowedFirstMistakes = Mathf.Max(0, JogaeRules.AllowedFirstMistakes - (st.firstAttempted - st.firstCorrect));
            }
            if (revealCorrect)
            {
                st.misconceptionId = "";
                if (revealPractice) st.score = 100;
                else
                {
                    st.score += firstAttempt ? 150 : 55;
                    st.solved++;
                }
            }
            else
            {
                st.misconceptionId = JogaeRules.MisconceptionId(current, slot);
                if (!revealPractice) st.lives--;
                firstAttempt = false;
            }
            flow = Flow.Reveal; st.dragging = false; revealClock = 0f;
            BeginRevealVisual(revealCorrect, revealPractice, current, slot, st.misconceptionId);
            SetScreen(); MgfBridge.NotifyChanged();
        }

        void FinishReveal()
        {
            if (flow != Flow.Reveal) return;
            CompleteRevealVisual(revealCorrect);
            if (revealPractice)
            {
                if (revealCorrect) BeginMainAfterPractice();
                else { flow = Flow.Practice; st.selectedSlot = -1; st.selectedAngle = 0; PrepareProblemVisual(current, true); RefreshProblemUi(); BoostGuide(); }
            }
            else if (!revealCorrect)
            {
                if (st.lives <= 0) EndRun("breath");
                else if (st.firstCorrect + st.remainingUnattempted < JogaeRules.MasteryFirstCorrect) EndRun("mastery");
                else
                {
                    flow = Flow.Playing; st.selectedSlot = -1; st.selectedAngle = 0;
                    PrepareProblemVisual(current, false); RefreshProblemUi(); ShowWrongReason(st.misconceptionId);
                }
            }
            else { flow = Flow.Playing; NextProblem(); }
            SetScreen(); MgfBridge.NotifyChanged();
        }

        void EndRun(string reason)
        {
            if (flow == Flow.End) return;
            endReason = reason; flow = Flow.End; st.dragging = false; st.onboarding = false;
            st.phase = reason == "clear" ? "clear" : "gameover"; st.mode = st.phase;
            if (reason == "clear")
            {
                int best = PlayerPrefs.GetInt("jogae-hapchang-best", 0);
                if (st.firstCorrect > best) { PlayerPrefs.SetInt("jogae-hapchang-best", st.firstCorrect); PlayerPrefs.Save(); }
            }
            ShowEndVisual(reason); SetScreen(); MgfBridge.NotifyChanged();
            MgfSfx.Play(reason == "clear" ? "win" : "lose", .52f);
        }

        void HandleInput()
        {
            if (flow == Flow.Title)
            {
                if (MgfPointer.Down) { st.inputRevision++; PulseTitleClam(); BeginOpening(); }
                return;
            }
            if (flow == Flow.End)
            {
                if (MgfPointer.Down) { st.inputRevision++; BeginOpening(); }
                return;
            }
            if (flow == Flow.Opening || flow == Flow.Reveal)
            {
                if (MgfPointer.Down)
                {
                    st.inputRevision++; RefuseInput(flow == Flow.Opening ? "조개가 문을 여는 중" : "소리를 듣는 중");
                    MgfBridge.NotifyChanged();
                }
                return;
            }
            if (flow == Flow.Bridge)
            {
                if (MgfPointer.Down)
                {
                    st.inputRevision++; st.dragging = true; dragStart = MgfPointer.Position;
                    UpdateInvariantBridge(MgfPointer.Position.x); MgfSfx.Play("tap", .18f); MgfBridge.NotifyChanged();
                }
                if (st.dragging && MgfPointer.Held)
                {
                    UpdateInvariantBridge(MgfPointer.Position.x); st.inputRevision++; MgfBridge.NotifyChanged();
                }
                if (st.dragging && MgfPointer.Up)
                {
                    st.dragging = false;
                    if (Vector2.Distance(dragStart, MgfPointer.Position) >= 18f) CompleteInvariantBridge();
                    else { RefuseInput("점 P를 원 둘레로 끌어 보시오"); MgfBridge.NotifyChanged(); }
                }
                return;
            }
            if (flow != Flow.Practice && flow != Flow.Playing) return;
            if (practiceAssistMoving) return;

            if (MgfPointer.Down)
            {
                st.inputRevision++; idleGuide = 0f;
                bool nearHandle = PointerNearHandle(MgfPointer.Position);
                if (nearHandle || flow == Flow.Practice)
                {
                    st.dragging = true; practiceFreeGrab = flow == Flow.Practice && !nearHandle;
                    dragStart = MgfPointer.Position; BeginDragVisual(); MgfSfx.Play("tap", .20f);
                }
                else
                {
                    st.dragging = false; RefuseInput("윗입술 손잡이를 움직이시오"); BoostGuide(); MgfSfx.Play("wrong", .10f);
                }
                MgfBridge.NotifyChanged();
            }
            if (st.dragging && MgfPointer.Held)
            {
                int slot;
                if (practiceFreeGrab)
                {
                    float progress = Mathf.Clamp01(Vector2.Distance(dragStart, MgfPointer.Position) / 110f);
                    slot = Mathf.RoundToInt(Mathf.Lerp(0, current.answerSlot, progress));
                }
                else slot = SlotFromPointer(MgfPointer.Position);
                if (slot != st.selectedSlot)
                {
                    st.selectedSlot = slot; st.selectedAngle = JogaeRules.AngleForSlot(slot);
                    UpdateSelectedVisual(slot); st.inputRevision++; MgfSfx.Play("tap", .035f); MgfBridge.NotifyChanged();
                }
            }
            if (st.dragging && MgfPointer.Up)
            {
                st.dragging = false;
                float distance = Vector2.Distance(dragStart, MgfPointer.Position);
                if (distance >= 18f)
                {
                    // 연습은 채점이 아니라 손잡이를 잡고 끌어 놓는 몸동작을 익히는 단계다.
                    // 화면 아무 곳에서 시작한 첫 드래그도 보이는 유령 궤적을 따라 40°까지 이어 준다.
                    if (practiceFreeGrab)
                    {
                        st.selectedSlot = current.answerSlot; st.selectedAngle = current.answerAngle;
                        UpdateSelectedVisual(st.selectedSlot);
                    }
                    practiceFreeGrab = false; practiceTapArmed = false; Submit(st.selectedSlot);
                }
                else if (flow == Flow.Practice)
                {
                    practiceFreeGrab = false; EndDragWithoutSubmit();
                    if (practiceTapArmed) BeginPracticeTapMove();
                    else ArmPracticeTapAssist();
                    MgfBridge.NotifyChanged();
                }
                else
                {
                    practiceFreeGrab = false; EndDragWithoutSubmit(); RefuseInput("손잡이를 눈금까지 끌어서 놓으시오"); BoostGuide(); MgfSfx.Play("wrong", .11f); MgfBridge.NotifyChanged();
                }
            }
        }

        void UpdatePracticeAssist(float dt)
        {
            if (!practiceAssistMoving || flow != Flow.Practice || current == null) return;
            practiceAssistClock += dt;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(practiceAssistClock / .46f));
            int slot = Mathf.RoundToInt(Mathf.Lerp(0, current.answerSlot, t));
            if (slot != st.selectedSlot)
            {
                st.selectedSlot = slot; st.selectedAngle = JogaeRules.AngleForSlot(slot);
                UpdateSelectedVisual(slot); st.inputRevision++; MgfBridge.NotifyChanged();
            }
            if (t < 1f) return;
            practiceAssistMoving = false; practiceTapArmed = false; st.dragging = false;
            st.selectedSlot = current.answerSlot; st.selectedAngle = current.answerAngle;
            UpdateSelectedVisual(st.selectedSlot); Submit(st.selectedSlot);
        }

        public void TestStart() => StartRunDirect();

        public void TestAnswerCorrect()
        {
            if (flow == Flow.Title || flow == Flow.End || flow == Flow.Opening) StartRunDirect();
            if (flow == Flow.Bridge) { CompleteInvariantBridge(); return; }
            if (flow == Flow.Reveal) FinishReveal();
            if (flow == Flow.Practice || flow == Flow.Playing) Submit(current.answerSlot);
        }

        public void TestAnswerWrong()
        {
            if (flow == Flow.Title || flow == Flow.End || flow == Flow.Opening) StartRunDirect();
            if (flow == Flow.Bridge) { CompleteInvariantBridge(); return; }
            if (flow == Flow.Reveal) FinishReveal();
            if (flow == Flow.Practice || flow == Flow.Playing)
            {
                int wrong = current.answerSlot == JogaeRules.SlotCount - 1 ? current.answerSlot - 1 : current.answerSlot + 1;
                Submit(wrong);
            }
        }

        public string StateJson()
        {
            if (flow == Flow.Title || flow == Flow.Opening) st.phase = "title";
            else if (flow == Flow.End) st.phase = endReason == "clear" ? "clear" : "gameover";
            else st.phase = "playing";
            st.mode = flow.ToString().ToLowerInvariant();
            UpdateScreenState(st);
            return JsonUtility.ToJson(st);
        }

        public string ProblemBankJson() => MgfJson.Bank(bank);
    }
}
