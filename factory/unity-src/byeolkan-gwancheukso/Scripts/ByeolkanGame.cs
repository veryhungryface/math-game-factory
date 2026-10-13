using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.ByeolkanGwancheukso
{
    public partial class ByeolkanGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int round;
            public int combo;
            public int firstAttemptTotal;
            public int firstAttemptCorrect;
            public int finalTwoFirstCorrect;
            public int pointerVersion;
            public int remainingTime;
            public string rackTopology = "none";
            public string maskIds = "";
            public string foldState = "open";
            public string litOutcomeIds = "";
            public string problemId = "";
            public string misconceptionId = "";
            public bool onboarding;
        }

        enum Flow { Title, Practice, Playing, Feedback, End }
        enum DragMode { None, RackA, RackB, Crank, Fold }

        readonly State st = new State();
        readonly List<ObservationProblem> catalog = new List<ObservationProblem>();
        readonly List<MgfProblem> bank = new List<MgfProblem>();
        readonly bool[] marked = new bool[36];
        readonly bool[] feedbackMissing = new bool[36];
        readonly bool[] feedbackExtra = new bool[36];

        Flow flow = Flow.Title;
        ObservationProblem current;
        RackTopology rackA = RackTopology.None, rackB = RackTopology.None;
        bool folded;
        bool attempted;
        bool feedbackCorrect;
        bool feedbackWasPractice;
        float feedbackClock;
        float runClock;
        float idleClock;
        float correctHitStop;
        int missionIndex;
        int tailFirstCorrect;
        DragMode dragMode;
        Vector2 dragStart;
        Vector2 dragNow;
        float crankAccum;
        float crankLastAngle;
        bool crankHasAngle;
        string endReason = "";
        int runSerial;

        void Awake()
        {
            MgfLook.Quality(36f);
            catalog.AddRange(OutcomeGraphRules.BuildCatalog());
            for (int i = 0; i < catalog.Count; i++) bank.Add(catalog[i].ToMgf());
            Prewarm();
            BuildWorld();
            BuildUi();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb = new StringBuilder("별칸관측소랙을엮어밤하늘을켜라중학교2학년경우의수관측환당기기최고기록두사건을엮어정확한별칸을밝히시오십의자리일의자리서로다른두개의주사위를동시에던질때역할구분없이대표두명교차홈끝접속홈월식마개접이손잡이크랭크첫시도렌즈봉인연습관측완료다시관측하기");
            for (int i = 0; i < catalog.Count; i++) { sb.Append(catalog[i].prompt); sb.Append(catalog[i].reveal); sb.Append(catalog[i].concept); }
            MgfText.Prewarm(sb.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(.05f, Time.deltaTime);
            HandleInput();

            if (flow == Flow.Playing)
            {
                runClock += dt;
                idleClock += dt;
                int remain = Mathf.Max(0, Mathf.CeilToInt(110f - runClock));
                if (remain != st.remainingTime) { st.remainingTime = remain; RefreshHud(); MgfBridge.NotifyChanged(); }
                if (runClock >= 110f) EndRun("lost", "110초 관측 시간이 끝났다");
                else if (idleClock >= 8f) { idleClock = 0f; BoostGuide(); }
            }
            else if (flow == Flow.Practice)
            {
                idleClock += dt;
                if (idleClock >= 8f) { idleClock = 0f; BoostGuide(); }
            }
            else if (flow == Flow.Feedback)
            {
                if (correctHitStop > 0f) correctHitStop -= dt;
                else feedbackClock += dt;
                float duration = feedbackCorrect ? (feedbackWasPractice ? 1.45f : 1.35f) : 1.15f;
                if (feedbackClock >= duration) FinishFeedback();
            }

            UpdateWorld(dt);
            UpdateUi(dt);
        }

        void ShowTitle()
        {
            flow = Flow.Title;
            current = OutcomeGraphRules.Practice();
            missionIndex = 0; tailFirstCorrect = 0; runClock = 0f; idleClock = 0f;
            st.score = 0; st.lives = OutcomeGraphRules.StartLives; st.level = 1; st.solved = 0;
            st.round = 0; st.combo = 0; st.firstAttemptTotal = 0; st.firstAttemptCorrect = 0;
            st.finalTwoFirstCorrect = 0; st.remainingTime = 110; st.pointerVersion = 0;
            st.problemId = current.id; st.misconceptionId = ""; st.onboarding = false; st.phase = "title";
            endReason = "";
            ResetBoard();
            ResetWorldForRun();
            SetScreen();
            MgfBridge.NotifyChanged();
        }

        void BeginRun(bool onboarding)
        {
            runSerial++;
            flow = onboarding ? Flow.Practice : Flow.Playing;
            missionIndex = 0; tailFirstCorrect = 0; runClock = 0f; idleClock = 0f;
            st.score = 0; st.lives = OutcomeGraphRules.StartLives; st.level = 1; st.solved = 0;
            st.round = onboarding ? 0 : 1; st.combo = 0; st.firstAttemptTotal = 0; st.firstAttemptCorrect = 0;
            st.finalTwoFirstCorrect = 0; st.remainingTime = 110; st.misconceptionId = "";
            st.onboarding = onboarding; st.phase = "playing"; endReason = "";
            ResetWorldForRun();
            LoadProblem(onboarding ? OutcomeGraphRules.Practice() : OutcomeGraphRules.Mission(0, runSerial));
            SetScreen();
            StartGuide();
            MgfSfx.Play("whoosh", .32f);
            MgfBridge.NotifyChanged();
        }

        void LoadProblem(ObservationProblem p)
        {
            current = p;
            attempted = false;
            st.problemId = p.id;
            st.misconceptionId = "";
            st.rackTopology = "none";
            st.maskIds = "";
            st.foldState = "open";
            st.litOutcomeIds = "";
            ResetBoard();
            RefreshProblemUi();
            ResetWorldForProblem();
            MgfBridge.NotifyChanged();
        }

        void ResetBoard()
        {
            rackA = RackTopology.None; rackB = RackTopology.None; folded = false;
            Array.Clear(marked, 0, marked.Length);
            Array.Clear(feedbackMissing, 0, feedbackMissing.Length);
            Array.Clear(feedbackExtra, 0, feedbackExtra.Length);
            dragMode = DragMode.None; crankAccum = 0f; crankHasAngle = false;
        }

        void HandleInput()
        {
            bool down = MgfPointer.Down;
            if (down)
            {
                st.pointerVersion++;
                SpawnRipple(MgfPointer.Position);
                MgfBridge.NotifyChanged();
            }

            if (flow == Flow.Title)
            {
                if (down)
                {
                    if (HitTitleHandle(MgfPointer.Position)) BeginRun(true);
                    else { Refuse("빛나는 관측환 손잡이를 당기시오."); PointToTitleHandle(); }
                }
                return;
            }

            if (flow == Flow.End)
            {
                if (down)
                {
                    if (HitRestart(MgfPointer.Position)) BeginRun(true);
                    else { Refuse("회전하는 관측환을 눌러 다시 시작하시오."); PulseRestart(); }
                }
                return;
            }

            if (flow == Flow.Feedback)
            {
                if (down) Refuse(feedbackCorrect ? "별칸이 돔에 기록되고 있다." : "어두운 기포가 된 접점을 확인하시오.");
                return;
            }
            if (flow != Flow.Practice && flow != Flow.Playing) return;

            if (down)
            {
                dragStart = dragNow = MgfPointer.Position;
                if (HitRackHandle(0, dragStart)) { dragMode = DragMode.RackA; BeginRackDrag(0); return; }
                if (HitRackHandle(1, dragStart)) { dragMode = DragMode.RackB; BeginRackDrag(1); return; }
                if (current.needsFold && HitFoldHandle(dragStart)) { dragMode = DragMode.Fold; BeginFoldDrag(); return; }
                if (HitCrank(dragStart))
                {
                    dragMode = DragMode.Crank; crankAccum = 0f; crankHasAngle = false; BeginCrankDrag(); TrackCrank(dragStart); return;
                }
                int cell = HitCell(dragStart, current.rows, current.cols);
                if (cell >= 0 && current.cellAction != CellAction.None) { ToggleCell(cell); return; }
                Refuse(CurrentInstruction());
                PointToNextAction();
            }

            if (MgfPointer.Held && dragMode != DragMode.None)
            {
                dragNow = MgfPointer.Position;
                if (dragMode == DragMode.RackA) UpdateRackDrag(0, dragNow);
                else if (dragMode == DragMode.RackB) UpdateRackDrag(1, dragNow);
                else if (dragMode == DragMode.Fold) UpdateFoldDrag(dragNow);
                else if (dragMode == DragMode.Crank) TrackCrank(dragNow);
            }

            if (MgfPointer.Up && dragMode != DragMode.None)
            {
                dragNow = MgfPointer.Position;
                if (dragMode == DragMode.RackA || dragMode == DragMode.RackB)
                {
                    int rack = dragMode == DragMode.RackA ? 0 : 1;
                    RackTopology drop = TopologyAtDrop(dragNow, rack);
                    if (flow == Flow.Practice && drop != RackTopology.Crossed)
                    {
                        drop = RackTopology.None;
                        Refuse("첫 관측은 두 랙을 직각 교차 홈에 꽂으시오.");
                    }
                    SetRack(rack, drop);
                }
                else if (dragMode == DragMode.Fold)
                {
                    float delta = dragStart.x - dragNow.x;
                    if (delta >= Mathf.Max(62f, Screen.width * .09f)) SetFold(true);
                    else { CancelFoldDrag(); Refuse("접이 손잡이를 격자 반대쪽까지 끌어 포개시오."); }
                }
                else if (dragMode == DragMode.Crank)
                {
                    EndCrankDrag();
                    if (crankAccum >= 105f) SubmitBoard();
                    else { Refuse("크랭크를 원형 궤도로 더 돌리시오."); BoostCrankGuide(); }
                }
                dragMode = DragMode.None;
            }
        }

        void SetRack(int rack, RackTopology topology)
        {
            if (rack == 0) rackA = topology; else rackB = topology;
            idleClock = 0f;
            UpdateTopologyState();
            DockRack(rack, topology);
            if (topology == RackTopology.None)
            {
                Refuse("끝접속 홈 또는 중앙 교차 홈에 랙을 놓으시오.");
                MgfSfx.Play("wrong", .13f);
            }
            else
            {
                MgfSfx.Play("pop", .25f);
                PulseDock(topology);
            }
            MgfBridge.NotifyChanged();
        }

        void SetFold(bool value)
        {
            folded = value;
            st.foldState = value ? "folded" : "open";
            idleClock = 0f;
            FoldBoard(value);
            MgfSfx.Play("whoosh", .24f);
            MgfBridge.NotifyChanged();
        }

        void ToggleCell(int index)
        {
            if (index < 0 || index >= current.expectedCells.Length) return;
            marked[index] = !marked[index];
            idleClock = 0f;
            st.misconceptionId = "";
            RefreshMarkedState();
            CellPressed(index, marked[index]);
            MgfSfx.Play("tap", marked[index] ? .20f : .12f);
            MgfBridge.NotifyChanged();
        }

        void TrackCrank(Vector2 pos)
        {
            Vector2 center = CrankCenterScreen();
            float angle = Mathf.Atan2(pos.y - center.y, pos.x - center.x) * Mathf.Rad2Deg;
            if (crankHasAngle)
            {
                float d = Mathf.Abs(Mathf.DeltaAngle(crankLastAngle, angle));
                if (d < 80f) crankAccum += d;
            }
            crankLastAngle = angle; crankHasAngle = true;
            RotateCrank(angle, crankAccum);
        }

        void SubmitBoard()
        {
            if (rackA == RackTopology.None || rackB == RackTopology.None)
            {
                Refuse("두 랙을 모두 홈에 꽂은 뒤 크랭크를 돌리시오.");
                PointToNextAction();
                return;
            }
            bool correct = OutcomeGraphRules.Matches(current, rackA, rackB, marked, folded);
            Resolve(correct, correct ? "" : OutcomeGraphRules.Diagnose(current, rackA, rackB, marked, folded));
        }

        void Resolve(bool correct, string misconception)
        {
            if (flow != Flow.Practice && flow != Flow.Playing) return;
            bool practice = flow == Flow.Practice;
            feedbackWasPractice = practice;
            feedbackCorrect = correct;
            feedbackClock = 0f;
            correctHitStop = correct ? .075f : 0f;
            flow = Flow.Feedback;
            st.misconceptionId = misconception;
            Array.Clear(feedbackMissing, 0, feedbackMissing.Length);
            Array.Clear(feedbackExtra, 0, feedbackExtra.Length);
            for (int i = 0; i < current.expectedCells.Length && i < marked.Length; i++)
            {
                feedbackMissing[i] = current.expectedCells[i] && !marked[i];
                feedbackExtra[i] = !current.expectedCells[i] && marked[i];
            }

            if (correct)
            {
                st.combo++;
                int gain = practice ? 60 : 120 + st.combo * 25;
                st.score += gain;
                if (!practice)
                {
                    if (!attempted)
                    {
                        st.firstAttemptTotal++;
                        st.firstAttemptCorrect++;
                        if (missionIndex >= 4) tailFirstCorrect++;
                    }
                    st.finalTwoFirstCorrect = tailFirstCorrect;
                    st.solved++;
                }
                st.litOutcomeIds = CorrectOutcomeIds();
                CorrectFeedback(current.reveal, gain);
                MgfSfx.Play(practice ? "correct" : st.solved >= OutcomeGraphRules.Goal ? "win" : "correct", .50f);
            }
            else
            {
                if (!practice)
                {
                    if (!attempted) st.firstAttemptTotal++;
                    attempted = true;
                    st.combo = 0;
                    st.lives = Mathf.Max(0, st.lives - 1);
                }
                WrongFeedback(misconception, CorrectiveMessage(misconception));
                MgfSfx.Play("wrong", .34f);
            }
            RefreshHud();
            MgfBridge.NotifyChanged();
        }

        void FinishFeedback()
        {
            if (feedbackCorrect)
            {
                if (feedbackWasPractice)
                {
                    flow = Flow.Playing;
                    st.onboarding = false;
                    missionIndex = 0; st.round = 1; st.level = 1;
                    LoadProblem(OutcomeGraphRules.Mission(0, runSerial));
                    AdvanceGoalBanner();
                    return;
                }
                if (st.solved >= OutcomeGraphRules.Goal)
                {
                    bool mastered = st.firstAttemptCorrect >= 5 && tailFirstCorrect >= 2;
                    EndRun(mastered ? "clear" : "practice_complete",
                        mastered ? "돔의 여섯 별자리가 모두 이어졌다" : "관측은 끝냈지만 최초 정확도를 더 연습해야 한다");
                    return;
                }
                missionIndex++;
                st.round = missionIndex + 1;
                st.level = missionIndex < 2 ? 1 : missionIndex < 4 ? 2 : 3;
                flow = Flow.Playing;
                LoadProblem(OutcomeGraphRules.Mission(missionIndex, runSerial));
            }
            else
            {
                if (!feedbackWasPractice && st.lives <= 0) { EndRun("lost", "렌즈 봉인 세 개가 모두 갈라졌다"); return; }
                flow = feedbackWasPractice ? Flow.Practice : Flow.Playing;
                ClearFeedbackVisuals();
                RefreshProblemUi();
                MgfBridge.NotifyChanged();
            }
        }

        void EndRun(string phaseName, string reason)
        {
            flow = Flow.End;
            st.phase = phaseName;
            st.onboarding = false;
            endReason = reason;
            ShowEnd(phaseName, reason);
            MgfSfx.Play(phaseName == "clear" ? "win" : "lose", .48f);
            MgfBridge.NotifyChanged();
        }

        string CorrectiveMessage(string id)
        {
            switch (id)
            {
                case "sum-product-swapped": return current.expectedTopology == RackTopology.Crossed
                    ? "각각 하나씩 고르므로 두 랙을 교차해야 한다." : "둘 중 하나만 고르므로 두 랙을 끝으로 이어야 한다.";
                case "ordered-representatives": return "역할이 없으므로 (가,나)와 (나,가)를 접어 포개야 한다.";
                case "leading-zero-allowed": return "0은 두 자리 자연수의 십의 자리에 올 수 없다.";
                case "repeated-card-allowed": return "카드는 각각 한 장이므로 같은 카드를 두 번 쓸 수 없다.";
                case "same-person-allowed": return "한 사람을 두 번 뽑는 대각 접점은 가려야 한다.";
                case "ordered-pair-set-mismatch": return "두 주사위는 서로 다르므로 순서쌍을 하나씩 확인하시오.";
                default: return "밝힐 접점과 가릴 접점을 다시 비교하시오.";
            }
        }

        string CurrentInstruction()
        {
            if (rackA == RackTopology.None) return "왼쪽 오팔 랙을 홈으로 끌어 놓으시오.";
            if (rackB == RackTopology.None) return "오른쪽 오팔 랙도 같은 방식의 홈에 놓으시오.";
            if (current.cellAction == CellAction.MaskForbidden && !CellsMatch()) return "금지 접점에 월식 마개를 놓으시오.";
            if (current.cellAction == CellAction.KeepTargets && !CellsMatch()) return "조건에 맞는 순서쌍 접점만 눌러 표시하시오.";
            if (current.needsFold && !folded) return "대각선을 가린 뒤 접이 손잡이를 왼쪽으로 끌어 포개시오.";
            return "오른쪽 크랭크를 원형으로 돌려 관측하시오.";
        }

        bool CellsMatch()
        {
            if (current.cellAction == CellAction.None) return true;
            for (int i = 0; i < current.expectedCells.Length; i++) if (marked[i] != current.expectedCells[i]) return false;
            return true;
        }

        void RefreshMarkedState()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < current.expectedCells.Length; i++) if (marked[i])
            {
                if (sb.Length > 0) sb.Append(',');
                sb.Append(i);
            }
            st.maskIds = sb.ToString();
        }

        void UpdateTopologyState()
        {
            st.rackTopology = rackA.ToString().ToLowerInvariant() + "+" + rackB.ToString().ToLowerInvariant();
        }

        string CorrectOutcomeIds()
        {
            if (current.cellAction == CellAction.None) return "all:" + current.answerCount;
            var sb = new StringBuilder();
            if (current.cellAction == CellAction.KeepTargets)
            {
                for (int i = 0; i < current.expectedCells.Length; i++) if (current.expectedCells[i]) { if (sb.Length > 0) sb.Append(','); sb.Append(i); }
            }
            else sb.Append("remaining:").Append(current.answerCount);
            return sb.ToString();
        }

        void ForceBoardCorrect()
        {
            rackA = rackB = current.expectedTopology;
            folded = current.needsFold;
            Array.Clear(marked, 0, marked.Length);
            for (int i = 0; i < current.expectedCells.Length; i++) marked[i] = current.expectedCells[i];
            UpdateTopologyState(); RefreshMarkedState(); st.foldState = folded ? "folded" : "open";
            SnapBoardToState();
        }

        public void TestStart() { BeginRun(false); }

        public void TestAnswerCorrect()
        {
            if (flow == Flow.Title || flow == Flow.End) BeginRun(false);
            if (flow == Flow.Feedback) { feedbackClock = 99f; FinishFeedback(); }
            if (flow != Flow.Playing && flow != Flow.Practice) return;
            ForceBoardCorrect();
            SubmitBoard();
        }

        public void TestAnswerWrong()
        {
            if (flow == Flow.Title || flow == Flow.End) BeginRun(false);
            if (flow == Flow.Feedback) { feedbackClock = 99f; FinishFeedback(); }
            if (flow != Flow.Playing && flow != Flow.Practice) return;
            ResetBoard();
            rackA = rackB = current.expectedTopology == RackTopology.Crossed ? RackTopology.EndJoined : RackTopology.Crossed;
            UpdateTopologyState(); SnapBoardToState(); SubmitBoard();
        }

        public string StateJson() { return JsonUtility.ToJson(st); }
        public string ProblemBankJson() { return MgfJson.Bank(bank); }
    }
}
