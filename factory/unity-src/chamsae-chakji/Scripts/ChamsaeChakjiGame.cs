// 참새 착지 — 상태 머신, 실제 포인터 드래그, 첫 시도 장부와 실패 상태.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.ChamsaeChakji
{
    public partial class ChamsaeChakjiGame : MonoBehaviour, IMgfGame
    {
        static readonly Type PreserveSphereColliderType = typeof(SphereCollider);
        static readonly Type PreserveCapsuleColliderType = typeof(CapsuleCollider);
        static readonly Type PreserveBoxColliderType = typeof(BoxCollider);

        [Serializable]
        public sealed class GameState : MgfState
        {
            public int round;
            public int attemptedRounds;
            public int firstCorrect;
            public int dispersionFirstCorrect;
            public int[] placements = Array.Empty<int>();
            public bool submitted;
            public bool firstAttemptRecorded;
            public bool repairMode;
            public bool onboarding;
            public string lastFeedback = "";
            public string misconceptionId = "";
            public string problemId = "";
            public int inputRevision;
            public int handlePlacedMask;
            public bool layoutLand;
            public int railLeftPx;
            public int railRightPx;
            public int[] rowYPx = new int[4];
        }

        enum Flow { Title, Opening, Practice, Playing, Reveal, End }

        readonly GameState st = new GameState();
        readonly List<MgfProblem> bank = new List<MgfProblem>(1200);
        List<LandingProblem> pool;
        List<LandingProblem> deck;
        LandingProblem current;
        Flow flow = Flow.Title;
        int runSerial;
        int placedA;
        int placedB;
        int draggingHandle = -1;
        int valueAtDown;
        Vector2 pointerDown;
        bool dragMoved;
        bool firstAttempt;
        bool revealCorrect;
        bool revealPractice;
        bool pendingEnd;
        string pendingEndReason = "";
        float openingClock;
        float revealClock;
        float timeLeft;
        float idleGuide;

        public GameState State => st;
        public LandingProblem Current => current;
        public float TimeLeft => timeLeft;
        public bool IsTitle => flow == Flow.Title;
        public bool IsPractice => flow == Flow.Practice;
        public bool IsReveal => flow == Flow.Reveal;

        void Awake()
        {
            GC.KeepAlive(PreserveSphereColliderType);
            GC.KeepAlive(PreserveCapsuleColliderType);
            GC.KeepAlive(PreserveBoxColliderType);
            MgfLook.Quality(34f);
            pool = ChamsaeRules.BuildProblems();
            for (int i = 0; i < pool.Count; i++) bank.Add(pool[i].ToMgf());
            Prewarm();
            BuildWorld();
            BuildUi();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb = new StringBuilder(90000);
            sb.Append("참새착지횃대를옮겨조건에맞추면참새가착지한다중학교3학년산포도편차변량평균분산표준편차제곱의합고정이동자료첫시도걸쇠남은시간빗물측정창수리다시시작흰손잡이횃대를옮기시오");
            for (int i = 0; i < bank.Count; i++) sb.Append(bank[i].prompt).Append(bank[i].answer).Append(bank[i].unitConcept);
            MgfText.Prewarm(sb.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(.05f, Time.unscaledDeltaTime);
            Relayout(false);
            HandlePointer();
            if (flow == Flow.Opening)
            {
                openingClock += dt;
                if (openingClock >= .42f) BeginPractice();
            }
            else if (flow == Flow.Practice)
            {
                idleGuide += dt;
                if (idleGuide >= 8f) { idleGuide = 0f; BoostGuide(true); }
            }
            else if (flow == Flow.Playing)
            {
                timeLeft -= dt;
                if (timeLeft <= 0f) { timeLeft = 0f; EndRun(false, "time"); }
                else
                {
                    idleGuide += dt;
                    if (idleGuide >= 8f) { idleGuide = 0f; BoostGuide(false); }
                }
            }
            else if (flow == Flow.Reveal)
            {
                revealClock += dt;
                float duration = revealCorrect ? 1.30f : 1.52f;
                if (revealClock >= duration) FinishReveal();
            }
            TickView(dt);
        }

        void ResetState()
        {
            st.score = 0; st.lives = ChamsaeRules.StartLives; st.level = 1; st.phase = "title"; st.solved = 0;
            st.round = 0; st.attemptedRounds = 0; st.firstCorrect = 0; st.dispersionFirstCorrect = 0;
            st.placements = Array.Empty<int>(); st.submitted = false; st.firstAttemptRecorded = false;
            st.repairMode = false; st.onboarding = false; st.lastFeedback = ""; st.misconceptionId = ""; st.problemId = "";
            st.inputRevision = 0; st.handlePlacedMask = 0;
            current = null; deck = null; placedA = 0; placedB = 0; draggingHandle = -1; firstAttempt = true;
            dragMoved = false; openingClock = 0; revealClock = 0; idleGuide = 0; timeLeft = ChamsaeRules.SessionSeconds;
            pendingEnd = false; pendingEndReason = "";
        }

        void ShowTitle()
        {
            ResetState(); flow = Flow.Title; st.phase = "title";
            ResetVisuals(); ShowTitleVisual(); Notify();
        }

        void StartOpening()
        {
            if (flow != Flow.Title && flow != Flow.End) return;
            ResetState(); flow = Flow.Opening; st.phase = "playing"; openingClock = 0;
            BeginOpeningVisual(); Notify(); MgfSfx.Play("whoosh", .34f);
        }

        void BeginPractice()
        {
            flow = Flow.Practice; st.phase = "playing"; st.onboarding = true; st.level = 1;
            LoadProblem(ChamsaeRules.Practice(), true);
            BoostGuide(true);
        }

        void BeginRun(bool deterministic)
        {
            runSerial++;
            ResetState();
            deck = ChamsaeRules.BuildDeck(pool, runSerial, deterministic);
            flow = Flow.Playing; st.phase = "playing"; st.onboarding = false; timeLeft = ChamsaeRules.SessionSeconds;
            LoadRound(0);
        }

        void LoadRound(int index)
        {
            if (index >= ChamsaeRules.RoundCount)
            {
                bool mastered = st.firstCorrect >= ChamsaeRules.MasteryNeeded && st.dispersionFirstCorrect >= ChamsaeRules.DispersionMasteryNeeded;
                EndRun(mastered, mastered ? "clear" : "mastery");
                return;
            }
            flow = Flow.Playing; st.phase = "playing"; st.onboarding = false;
            st.round = index + 1; st.level = deck[index].band;
            LoadProblem(deck[index], false);
        }

        void LoadProblem(LandingProblem p, bool practice)
        {
            current = p;
            st.problemId = p.id; st.firstAttemptRecorded = false; st.submitted = false; st.repairMode = false;
            st.lastFeedback = ""; st.misconceptionId = ""; st.handlePlacedMask = 0;
            firstAttempt = true; draggingHandle = -1; dragMoved = false; idleGuide = 0;
            placedA = p.shift + ((p.answerA + 3 + st.round) % 9);
            if (placedA == p.answerA + p.shift) placedA = p.shift + ((p.answerA + 1) % 9);
            placedB = p.shift + ((p.answerB >= 0 ? p.answerB + 5 + st.round : 7) % 9);
            if (p.band > 1 && placedB == p.answerB + p.shift) placedB = p.shift + ((p.answerB + 2) % 9);
            SyncPlacements();
            LoadProblemVisual(p, practice); ShowPlayVisual(); Notify();
        }

        void SyncPlacements()
        {
            st.placements = current != null && current.band > 1 ? new[] { placedA, placedB } : new[] { placedA };
        }

        void HandlePointer()
        {
            if (MgfPointer.Down)
            {
                pointerDown = MgfPointer.Position;
                st.inputRevision++;
                if (flow == Flow.Title) { StartOpening(); return; }
                if (flow == Flow.End) { BeginRun(false); return; }
                if (flow == Flow.Opening || flow == Flow.Reveal)
                {
                    RefuseInput("착지가 끝난 뒤 횃대를 옮기시오.", pointerDown, false); return;
                }
                if (flow != Flow.Practice && flow != Flow.Playing) return;

                int handle = HitMovableHandle(pointerDown);
                if (handle < 0)
                {
                    RefuseInput("흰 손잡이 횃대를 옮기시오.", pointerDown, true); return;
                }
                if (current.band > 1 && handle == 1 && (st.handlePlacedMask & 1) == 0)
                {
                    RefuseInput("첫째 횃대를 먼저 놓으시오.", pointerDown, true); return;
                }
                draggingHandle = handle; valueAtDown = handle == 0 ? placedA : placedB; dragMoved = false;
                BeginHandleDrag(handle); MgfSfx.Play("tap", .16f); Notify();
            }

            if (draggingHandle >= 0 && MgfPointer.Held)
            {
                int next = ValueFromScreen(MgfPointer.Position, current.shift);
                if (next != (draggingHandle == 0 ? placedA : placedB))
                {
                    if (draggingHandle == 0) placedA = next; else placedB = next;
                    dragMoved = true; st.inputRevision++; SyncPlacements();
                    UpdateHandleDrag(draggingHandle, next); Notify();
                }
                if (Vector2.Distance(pointerDown, MgfPointer.Position) >= Mathf.Max(12f, Mathf.Min(Screen.width, Screen.height) * .018f)) dragMoved = true;
            }

            if (draggingHandle >= 0 && MgfPointer.Up)
            {
                int handle = draggingHandle; draggingHandle = -1; EndHandleDrag(handle);
                if (!dragMoved)
                {
                    if (handle == 0) placedA = valueAtDown; else placedB = valueAtDown;
                    SyncPlacements(); RefuseInput("손잡이를 잡고 다른 눈금까지 끌어 놓으시오.", MgfPointer.Position, true); return;
                }
                st.handlePlacedMask |= 1 << handle; st.inputRevision++; SyncPlacements();
                PinHandle(handle); Notify();
                if (current.band == 1 || handle == 1) Submit();
                else ShowSecondHandleCue();
            }
        }

        void Submit()
        {
            if (current == null || (flow != Flow.Practice && flow != Flow.Playing)) return;
            revealPractice = flow == Flow.Practice;
            revealCorrect = ChamsaeRules.IsCorrect(current, placedA, placedB);
            st.submitted = true; st.lastFeedback = revealCorrect ? "correct" : "wrong";

            if (!revealPractice && firstAttempt)
            {
                st.attemptedRounds++; st.firstAttemptRecorded = true;
                if (revealCorrect)
                {
                    st.firstCorrect++;
                    if (current.band >= 2) st.dispersionFirstCorrect++;
                }
            }

            if (revealCorrect)
            {
                st.misconceptionId = "";
                if (!revealPractice)
                {
                    st.solved++;
                    st.score += firstAttempt ? 120 : 35;
                }
            }
            else
            {
                st.misconceptionId = ChamsaeRules.Classify(current, placedA, placedB);
                if (!revealPractice)
                {
                    st.lives = Math.Max(0, st.lives - 1);
                    st.repairMode = true;
                }
                firstAttempt = false;
            }

            pendingEnd = false; pendingEndReason = "";
            if (!revealPractice && !revealCorrect)
            {
                int remaining = ChamsaeRules.RoundCount - st.attemptedRounds;
                int remainingDispersion = Math.Max(0, 4 - Math.Max(0, st.attemptedRounds - 2));
                if (st.lives <= 0) { pendingEnd = true; pendingEndReason = "hooks"; }
                else if (st.firstCorrect + remaining < ChamsaeRules.MasteryNeeded || st.dispersionFirstCorrect + remainingDispersion < ChamsaeRules.DispersionMasteryNeeded)
                { pendingEnd = true; pendingEndReason = "mastery"; }
            }

            flow = Flow.Reveal; st.phase = pendingEnd ? "gameover" : "playing"; revealClock = 0;
            BeginRevealVisual(revealCorrect, revealPractice, current, placedA, placedB, firstAttempt);
            Notify();
        }

        void FinishReveal()
        {
            if (flow != Flow.Reveal) return;
            FinishRevealVisual(revealCorrect);
            if (pendingEnd) { EndRun(false, pendingEndReason); return; }
            if (revealPractice)
            {
                if (revealCorrect) BeginRun(false);
                else
                {
                    flow = Flow.Practice; st.phase = "playing"; st.submitted = false; st.lastFeedback = ""; st.misconceptionId = "";
                    placedA = current.shift + 1; st.handlePlacedMask = 0; SyncPlacements();
                    LoadProblemVisual(current, true); BoostGuide(true); Notify();
                }
                return;
            }
            if (revealCorrect) LoadRound(st.solved);
            else
            {
                flow = Flow.Playing; st.phase = "playing"; st.submitted = false; st.handlePlacedMask = 0;
                ShowRepairVisual(); BoostGuide(false); Notify();
            }
        }

        void EndRun(bool clear, string reason)
        {
            flow = Flow.End; st.phase = clear ? "clear" : "gameover"; draggingHandle = -1;
            ShowEndVisual(clear, reason); Notify();
            MgfSfx.Play(clear ? "win" : "lose", .54f);
        }

        void RefuseInput(string message, Vector2 screen, bool pointToHandle)
        {
            st.lastFeedback = "refuse"; st.inputRevision++;
            RefuseVisual(message, screen, pointToHandle); Notify();
        }

        void Notify()
        {
            RefreshHud();
            MgfBridge.NotifyChanged();
        }

        public void TestStart()
        {
            BeginRun(true);
        }

        public void TestAnswerCorrect()
        {
            if (flow == Flow.Title || flow == Flow.Opening || flow == Flow.End || current == null) BeginRun(true);
            if (flow == Flow.Reveal) { FinishReveal(); if (flow == Flow.End) BeginRun(true); }
            if (current == null) return;
            placedA = current.answerA + current.shift;
            placedB = current.band > 1 ? current.answerB + current.shift : -1;
            st.handlePlacedMask = current.band > 1 ? 3 : 1; SyncPlacements(); Submit();
        }

        public void TestAnswerWrong()
        {
            if (flow == Flow.Title || flow == Flow.Opening || flow == Flow.End || current == null) BeginRun(true);
            if (flow == Flow.Reveal) { FinishReveal(); if (flow == Flow.End) BeginRun(true); }
            if (current == null) return;
            int[] wrong = ChamsaeRules.WrongPlacement(current);
            placedA = wrong[0]; placedB = wrong[1]; st.handlePlacedMask = current.band > 1 ? 3 : 1; SyncPlacements(); Submit();
        }

        public string StateJson()
        {
            SyncStateGeometry(st);
            return JsonUtility.ToJson(st);
        }

        public string ProblemBankJson() => MgfJson.Bank(bank);
    }
}
