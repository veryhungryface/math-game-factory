// 유령 이사 — 상태 머신, 실제 포인터 드래그, 첫 시도 장부, 수리와 실패 상태.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.YuryeongIsa
{
    public partial class YuryeongIsaGame : MonoBehaviour, IMgfGame
    {
        // IL2CPP 엔진 코드 스트리핑이 CreatePrimitive 내부 콜라이더 타입을 제거하지 못하게 한다.
        static readonly Type PreserveSphereColliderType = typeof(SphereCollider);
        static readonly Type PreserveBoxColliderType = typeof(BoxCollider);
        static readonly Type PreserveCapsuleColliderType = typeof(CapsuleCollider);

        [Serializable]
        public sealed class GameState : MgfState
        {
            public int round;
            public int firstAttemptCorrectCount;
            public int firstAttemptTotal;
            public bool firstAttemptLocked;
            public int attempts;
            public bool repairState;
            public bool onboarding;
            public int[] originalValues = Array.Empty<int>();
            public int[] currentValues = Array.Empty<int>();
            public int mean;
            public int startVariance;
            public int targetVariance;
            public string targetSigmaExact = "";
            public int timeLeft;
            public string problemId = "";
            public string lastFeedback = "";
            public string misconceptionId = "";
            public int inputRevision;
            public bool dragging;
            public int dragFrom = -1;
            public int[] houseXPx = new int[4];
            public int handleYPx;
            public int dropYPx;
            public bool layoutLand;
        }

        enum Flow { Title, Practice, Playing, Feedback, Repair, End }
        enum FeedbackKind { None, PracticeCorrect, PracticeWrong, Correct, Wrong, Repair }

        readonly GameState st = new GameState();
        readonly List<MgfProblem> bank = new List<MgfProblem>(960);
        List<int[]> pool;
        List<TransferProblem> deck;
        TransferProblem current;
        Flow flow = Flow.Title;
        FeedbackKind feedbackKind;
        int runSerial;
        int dragFrom = -1;
        bool dragMoved;
        Vector2 dragStart;
        int wrongFrom = -1, wrongTo = -1;
        float remainingTime;
        float feedbackClock;
        float idleGuide;
        bool firstAttemptOpen;
        bool pendingImpossible;

        public GameState State => st;
        public TransferProblem Current => current;
        public bool IsTitle => flow == Flow.Title;
        public bool IsPractice => flow == Flow.Practice;
        public bool IsRepair => flow == Flow.Repair;
        public bool IsFeedback => flow == Flow.Feedback;
        public float RemainingTime => remainingTime;

        void Awake()
        {
            GC.KeepAlive(PreserveSphereColliderType);
            GC.KeepAlive(PreserveBoxColliderType);
            GC.KeepAlive(PreserveCapsuleColliderType);
            MgfLook.Quality(36f);
            pool = YuryeongIsaRules.BuildBasePool();
            var generated = YuryeongIsaRules.BuildBank();
            for (int i = 0; i < generated.Count; i++) bank.Add(generated[i].ToMgf());
            Prewarm();
            BuildWorld();
            BuildUi();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb = new StringBuilder(90000);
            sb.Append("유령이사유령두마리를옮겨라중학교3학년산포도네집주민수평균분산표준편차편차제곱의합첫시도봉인남은시간목표실전연습수리다시하기");
            sb.Append("두유령을다른한집으로옮겨목표산포도를만드시오집주인을잡아창문으로끌어옮기시오바뀌는두편차의제곱만다시비교하시오");
            sb.Append("표준편차를제곱해목표분산을먼저확인하시오편차의합은영이지만분산은편차제곱의평균입니다정답오답원래집으로돌려보내시오");
            for (int i = 0; i < bank.Count; i++) sb.Append(bank[i].prompt).Append(bank[i].answer).Append(bank[i].unitConcept);
            MgfText.Prewarm(sb.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(.05f, Time.unscaledDeltaTime);
            Relayout(false);
            HandlePointer();
            if (flow == Flow.Practice || flow == Flow.Playing || flow == Flow.Repair)
            {
                idleGuide += dt;
                if (idleGuide >= 8f) { idleGuide = 0f; BoostGuide(flow == Flow.Practice || flow == Flow.Repair); }
            }
            if (flow == Flow.Playing)
            {
                remainingTime -= dt;
                if (remainingTime <= 0f) { remainingTime = 0f; EndRun(false, "180초가 끝났습니다."); }
            }
            else if (flow == Flow.Feedback)
            {
                feedbackClock += dt;
                float duration = feedbackKind == FeedbackKind.PracticeCorrect ? 2.05f : feedbackKind == FeedbackKind.Correct ? 1.75f : 1.05f;
                if (feedbackClock >= duration) FinishFeedback();
            }
            TickWorld(dt);
            TickUi(dt);
        }

        void ResetState()
        {
            st.score = 0; st.lives = YuryeongIsaRules.StartLives; st.level = 1; st.phase = "title"; st.solved = 0;
            st.round = 0; st.firstAttemptCorrectCount = 0; st.firstAttemptTotal = 0; st.firstAttemptLocked = false;
            st.attempts = 0; st.repairState = false; st.onboarding = false; st.originalValues = Array.Empty<int>();
            st.currentValues = Array.Empty<int>(); st.mean = 0; st.startVariance = 0; st.targetVariance = 0; st.targetSigmaExact = "";
            st.timeLeft = YuryeongIsaRules.SessionSeconds; st.problemId = ""; st.lastFeedback = ""; st.misconceptionId = "";
            st.inputRevision = 0; st.dragging = false; st.dragFrom = -1;
            dragFrom = wrongFrom = wrongTo = -1; dragMoved = false; feedbackClock = idleGuide = 0f;
            firstAttemptOpen = true; pendingImpossible = false; remainingTime = YuryeongIsaRules.SessionSeconds;
        }

        void ShowTitle()
        {
            ResetState(); flow = Flow.Title; current = YuryeongIsaRules.Practice();
            ResetWorldForTitle(); ShowTitleUi(); Notify();
        }

        void BeginPractice()
        {
            ResetState(); flow = Flow.Practice; st.phase = "playing"; st.onboarding = true;
            current = YuryeongIsaRules.Practice(); LoadProblem(current, true); OpenShutters();
            MgfSfx.Play("whoosh", .30f); Notify();
        }

        void BeginRun()
        {
            runSerial++;
            ResetState(); flow = Flow.Playing; st.phase = "playing"; st.onboarding = false;
            deck = YuryeongIsaRules.BuildDeck(pool, runSerial); remainingTime = YuryeongIsaRules.SessionSeconds;
            LoadRound(0); Notify();
        }

        void LoadRound(int index)
        {
            if (index >= YuryeongIsaRules.RoundCount)
            {
                bool mastered = st.firstAttemptCorrectCount >= YuryeongIsaRules.MasteryNeeded;
                EndRun(mastered, mastered ? "여덟 번의 이사를 마쳤습니다." : "완주했지만 첫 시도 6/8에 도달하지 못했습니다.");
                return;
            }
            flow = Flow.Playing; st.phase = "playing"; st.onboarding = false;
            st.round = index + 1; current = deck[index]; st.level = current.band;
            LoadProblem(current, false);
        }

        void LoadProblem(TransferProblem problem, bool practice)
        {
            current = problem; firstAttemptOpen = true; dragFrom = wrongFrom = wrongTo = -1; dragMoved = false;
            st.problemId = problem.id; st.originalValues = (int[])problem.values.Clone(); st.currentValues = (int[])problem.values.Clone();
            st.mean = problem.mean; st.startVariance = problem.startVariance; st.targetVariance = problem.targetVariance;
            st.targetSigmaExact = YuryeongIsaRules.SqrtForm(problem.targetVariance); st.firstAttemptLocked = false; st.attempts = 0;
            st.repairState = false; st.lastFeedback = ""; st.misconceptionId = ""; st.dragging = false; st.dragFrom = -1;
            st.timeLeft = Mathf.CeilToInt(remainingTime); idleGuide = 0f;
            LoadProblemWorld(problem, practice); ShowPlayUi(); RefreshAllUi();
            if (practice) BoostGuide(true);
            Notify();
        }

        void HandlePointer()
        {
            if (MgfPointer.Down)
            {
                st.inputRevision++; st.timeLeft = Mathf.CeilToInt(remainingTime); idleGuide = 0f;
                SpawnTapRipple(MgfPointer.Position); Notify();
                if (flow == Flow.Title) { PressTitleTag(); BeginPractice(); return; }
                if (flow == Flow.End) { BeginRun(); return; }
                if (flow == Flow.Feedback) { RefuseInput("결과를 확인하고 있습니다.", MgfPointer.Position); return; }
                if (flow != Flow.Practice && flow != Flow.Playing && flow != Flow.Repair) return;

                int house = HitHouseHandle(MgfPointer.Position);
                if (house < 0)
                {
                    RefuseInput(flow == Flow.Repair ? "방금 옮긴 두 유령을 원래 집으로 돌려보내시오." : "지붕의 두 유령 묶음을 잡으시오.", MgfPointer.Position);
                    PointToNearestHandle(MgfPointer.Position); return;
                }
                if (flow == Flow.Repair && house != wrongTo)
                {
                    RefuseInput("새집에 머문 두 유령을 잡아 원래 집으로 보내시오.", MgfPointer.Position);
                    PointRepairPath(wrongTo, wrongFrom); return;
                }
                if (st.currentValues[house] < 2)
                {
                    RefuseInput("이 집에는 옮길 두 유령이 없습니다.", MgfPointer.Position); RefuseHouse(house); return;
                }
                dragFrom = house; dragStart = MgfPointer.Position; dragMoved = false;
                st.dragging = true; st.dragFrom = house; BeginGhostDrag(house, MgfPointer.Position); MgfSfx.Play("tap", .14f); Notify();
            }

            if (MgfPointer.Held && dragFrom >= 0)
            {
                if ((MgfPointer.Position - dragStart).sqrMagnitude > 100f) dragMoved = true;
                MoveGhostDrag(MgfPointer.Position, HitHouseDrop(MgfPointer.Position));
            }

            if (MgfPointer.Up && dragFrom >= 0)
            {
                // 브라우저가 빠른 pointermove를 한 렌더 프레임 사이에 합쳐도 실제 시작→끝 이동은 유효한 드래그다.
                if ((MgfPointer.Position - dragStart).sqrMagnitude > 100f) dragMoved = true;
                int from = dragFrom; int to = HitHouseDrop(MgfPointer.Position);
                dragFrom = -1; st.dragging = false; st.dragFrom = -1;
                if (!dragMoved)
                {
                    CancelGhostDrag(from); RefuseInput("두 유령을 다른 집 창문까지 끌어 놓으시오.", MgfPointer.Position); Notify(); return;
                }
                if (to < 0)
                {
                    CancelGhostDrag(from); RefuseInput("창문 안에 놓아야 이사가 확정됩니다.", MgfPointer.Position); PointToNearestDrop(MgfPointer.Position); Notify(); return;
                }
                if (to == from)
                {
                    CancelGhostDrag(from); RefuseHouse(from); RefuseInput("다른 집으로 옮기시오.", MgfPointer.Position); Notify(); return;
                }
                if (flow == Flow.Repair)
                {
                    if (from == wrongTo && to == wrongFrom) CompleteRepair();
                    else { CancelGhostDrag(from); RefuseInput("점선 윤곽이 남은 원래 집으로 돌려보내시오.", MgfPointer.Position); PointRepairPath(wrongTo, wrongFrom); }
                    return;
                }
                CommitMove(from, to, flow == Flow.Practice);
            }
        }

        void CommitMove(int from, int to, bool practice)
        {
            int[] before = (int[])st.currentValues.Clone();
            st.currentValues[from] -= 2; st.currentValues[to] += 2; st.attempts++;
            bool correct = YuryeongIsaRules.IsCorrect(current, from, to);
            feedbackClock = 0f; flow = Flow.Feedback; st.phase = "playing"; st.firstAttemptLocked = true;
            PlayDropFlight(from, to, correct); SyncResidents(st.currentValues, true);
            if (practice)
            {
                feedbackKind = correct ? FeedbackKind.PracticeCorrect : FeedbackKind.PracticeWrong;
                st.lastFeedback = correct ? "평균 10은 그대로이고 분산은 46에서 30으로 바뀝니다." : "연습 목표는 분산 30입니다. 정답 상태를 확인한 뒤 다시 옮기시오.";
                st.misconceptionId = correct ? "" : YuryeongIsaRules.ClassifyWrong(current, from, to);
                if (correct) PlayCorrectChain(current, before, st.currentValues, true);
                else PlayPracticeWrong(current, from, to);
                Notify(); return;
            }

            if (firstAttemptOpen)
            {
                st.firstAttemptTotal++; firstAttemptOpen = false;
                if (correct) st.firstAttemptCorrectCount++;
            }
            if (correct)
            {
                feedbackKind = FeedbackKind.Correct; st.solved++; st.score += 120 + st.level * 30 + (st.firstAttemptLocked && st.attempts == 1 ? 40 : 0);
                st.lastFeedback = "목표 분산 " + current.targetVariance + "을 만들었습니다."; st.misconceptionId = "";
                PlayCorrectChain(current, before, st.currentValues, false); MgfSfx.Play("correct", .42f);
            }
            else
            {
                feedbackKind = FeedbackKind.Wrong; st.lives--; wrongFrom = from; wrongTo = to; st.repairState = true;
                st.misconceptionId = YuryeongIsaRules.ClassifyWrong(current, from, to);
                st.lastFeedback = YuryeongIsaRules.MisconceptionFeedback(st.misconceptionId);
                pendingImpossible = st.firstAttemptCorrectCount + (YuryeongIsaRules.RoundCount - st.round) < YuryeongIsaRules.MasteryNeeded;
                PlayWrongChain(from, to, st.misconceptionId); MgfSfx.Play("wrong", .34f);
            }
            RefreshAllUi(); Notify();
        }

        void CompleteRepair()
        {
            st.currentValues = (int[])current.values.Clone(); st.repairState = false; st.lastFeedback = "원래 자료로 돌아왔습니다. 같은 목표를 다시 판단하시오.";
            st.misconceptionId = ""; feedbackKind = FeedbackKind.Repair; feedbackClock = 0f; flow = Flow.Feedback;
            PlayRepairFlight(wrongTo, wrongFrom); SyncResidents(st.currentValues, true); MgfSfx.Play("whoosh", .24f);
            RefreshAllUi(); Notify();
        }

        void FinishFeedback()
        {
            if (flow != Flow.Feedback) return;
            StopFeedbackVisuals();
            if (feedbackKind == FeedbackKind.PracticeCorrect) { BeginRun(); return; }
            if (feedbackKind == FeedbackKind.PracticeWrong)
            {
                flow = Flow.Practice; st.currentValues = (int[])current.values.Clone(); st.firstAttemptLocked = false; st.attempts = 0;
                SyncResidents(st.currentValues, false); ShowPlayUi(); RefreshAllUi(); BoostGuide(true); Notify(); return;
            }
            if (feedbackKind == FeedbackKind.Correct) { LoadRound(st.solved); return; }
            if (feedbackKind == FeedbackKind.Wrong)
            {
                if (st.lives <= 0) { EndRun(false, "이삿짐 봉인 세 개가 모두 찢어졌습니다."); return; }
                if (pendingImpossible) { EndRun(false, "첫 시도 6/8에 도달할 수 없게 되었습니다."); return; }
                flow = Flow.Repair; st.phase = "playing"; ShowPlayUi(); RefreshAllUi(); PointRepairPath(wrongTo, wrongFrom); Notify(); return;
            }
            if (feedbackKind == FeedbackKind.Repair)
            {
                flow = Flow.Playing; st.phase = "playing"; wrongFrom = wrongTo = -1; ShowPlayUi(); RefreshAllUi(); Notify();
            }
        }

        void EndRun(bool clear, string reason)
        {
            flow = Flow.End; st.phase = clear ? "clear" : "gameover"; st.onboarding = false; st.dragging = false;
            st.timeLeft = Mathf.CeilToInt(remainingTime); st.lastFeedback = reason;
            int oldBest = PlayerPrefs.GetInt("yuryeong-isa-best-first", 0);
            if (st.firstAttemptCorrectCount > oldBest) { PlayerPrefs.SetInt("yuryeong-isa-best-first", st.firstAttemptCorrectCount); PlayerPrefs.Save(); }
            ShowEndUi(clear, reason); PlayEndWorld(clear); MgfSfx.Play(clear ? "win" : "lose", .46f); Notify();
        }

        void EnsureHookPlaying()
        {
            if (flow == Flow.Title || flow == Flow.Practice || flow == Flow.End) { BeginRun(); return; }
            if (flow == Flow.Feedback) { feedbackClock = 99f; FinishFeedback(); if (flow == Flow.End) BeginRun(); }
            if (flow == Flow.Repair)
            {
                st.currentValues = (int[])current.values.Clone(); st.repairState = false; wrongFrom = wrongTo = -1; flow = Flow.Playing;
                SyncResidents(st.currentValues, false);
            }
        }

        public void TestStart() { BeginRun(); }

        public void TestAnswerCorrect()
        {
            EnsureHookPlaying();
            CommitMove(current.from, current.to, false);
        }

        public void TestAnswerWrong()
        {
            EnsureHookPlaying();
            for (int from = 0; from < 4; from++) for (int to = 0; to < 4; to++) if (from != to && !YuryeongIsaRules.IsCorrect(current, from, to))
            { CommitMove(from, to, false); return; }
        }

        public string StateJson() => JsonUtility.ToJson(st);
        public string ProblemBankJson() => MgfJson.Bank(bank);
        void Notify() => MgfBridge.NotifyChanged();
    }
}
