using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.DalpaengiParade
{
    public partial class DalpaengiParadeGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public string roundId = "";
            public int firstCorrect;
            public int firstAttempts;
            public bool firstAttemptOpen;
            public string currentPermutation = "";
            public int placedCount;
            public int pointerVersion;
            public int invalidInputs;
            public bool layoutLand;
            public string lastMisconception = "";
            public string prompt = "";
        }

        enum LocalPhase { Title, Practice, Playing, Reveal, End }

        readonly State st = new State();
        readonly List<MgfProblem> bank = new List<MgfProblem>();
        List<ParadeProblem> deck;
        ParadeProblem current;
        LocalPhase localPhase = LocalPhase.Title;
        int runSerial;
        int roundIndex;
        bool firstSubmission;
        bool revealCorrect;
        bool revealPractice;
        bool pendingFail;
        string endReason = "";
        float sessionLeft = ParadeRules.SessionSeconds;
        float revealClock;
        float titleClock;
        float endClock;
        float shakeClock;
        float hitStopClock;
        float idleClock;
        float secondClock;

        readonly int[] slotToGroup = {-1,-1,-1,-1};
        readonly int[] groupToSlot = {-1,-1,-1,-1};
        int dragGroup = -1;
        int dragOldSlot = -1;
        Vector2 dragStartScreen;
        bool dragMoved;

        void Awake()
        {
            MgfLook.Quality(34f);
            string error;
            if (!ParadeRules.ValidateAll(out error)) Debug.LogError("[달팽이 퍼레이드] 정수 은행 검증 실패: " + error);
            bank.AddRange(ParadeRules.BuildBank());
            Prewarm();
            BuildWorld();
            BuildUi();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var s = new StringBuilder("달팽이퍼레이드무리를끌어줄세워감아시작중학교3학년산포도편차변량평균분산표준편차제곱작은순으로무리를놓으시오첫제출목표태엽리본남은시간연습실전다시감기행진성공행진중단빈칸에놓으시오기록초자료표준편차분산");
            for (int i = 0; i < bank.Count; i++) { s.Append(bank[i].prompt); s.Append(bank[i].answer); }
            MgfText.Prewarm(s.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(.05f, Time.unscaledDeltaTime);
            titleClock += dt;
            idleClock += dt;
            bool layoutChanged = UpdateLayoutIfNeeded();
            if (layoutChanged && dragGroup >= 0) CancelDrag(true);

            AnimateWorld(dt);
            AnimateUi(dt);

            if (hitStopClock > 0f) { hitStopClock -= dt; return; }

            HandlePointer();

            if (localPhase == LocalPhase.Playing)
            {
                sessionLeft -= dt;
                secondClock -= dt;
                if (secondClock <= 0f) { secondClock = 1f; RefreshHud(); }
                if (sessionLeft <= 0f) EndRun(false, "시간이 끝났다");
            }
            else if (localPhase == LocalPhase.Reveal)
            {
                revealClock -= dt;
                if (revealClock <= 0f) FinishReveal();
            }
            else if (localPhase == LocalPhase.End)
            {
                endClock += dt;
                RefreshEndCount();
            }
            PushStateShape();
        }

        void HandlePointer()
        {
            if (MgfPointer.Down)
            {
                st.pointerVersion++;
                ShowRipple(MgfPointer.Position);
                MgfBridge.NotifyChanged();
                if (localPhase == LocalPhase.Title)
                {
                    MgfSfx.Play("whoosh");
                    StartPractice();
                    return;
                }
                if (localPhase == LocalPhase.End)
                {
                    MgfSfx.Play("tap");
                    StartPractice();
                    return;
                }
                if (localPhase == LocalPhase.Reveal)
                {
                    Refuse("기록판이 돌아오는 중이다");
                    return;
                }
                if (localPhase != LocalPhase.Practice && localPhase != LocalPhase.Playing) return;

                dragGroup = NearestGroup(MgfPointer.Position);
                if (dragGroup < 0)
                {
                    Refuse("기록판을 잡아 끌어라");
                    return;
                }
                dragStartScreen = MgfPointer.Position;
                dragMoved = false;
                dragOldSlot = groupToSlot[dragGroup];
                if (dragOldSlot >= 0)
                {
                    slotToGroup[dragOldSlot] = -1;
                    groupToSlot[dragGroup] = -1;
                    SyncPlacedState();
                }
                SetGroupSelected(dragGroup, true);
                MgfSfx.Play("tap");
            }

            if (dragGroup >= 0 && MgfPointer.Held)
            {
                if (Vector2.Distance(dragStartScreen, MgfPointer.Position) >= 12f) dragMoved = true;
                Vector3 world;
                if (PointerWorld(out world))
                {
                    world.y = 1.05f;
                    SetDraggedPosition(dragGroup, world);
                    UpdateGuideTrail(dragGroup, world);
                }
            }

            bool recoveredRelease = dragGroup >= 0 && !MgfPointer.Held && !MgfPointer.Up;
            if (dragGroup >= 0 && (MgfPointer.Up || recoveredRelease))
            {
                int g = dragGroup;
                dragGroup = -1;
                SetGroupSelected(g, false);
                HideDragTrail();
                if (!dragMoved)
                {
                    RestoreGroup(g, dragOldSlot);
                    Refuse("기록판을 빈 출발 칸까지 끌어라");
                    return;
                }
                int slot = NearestSlot(MgfPointer.Position);
                if (slot < 0)
                {
                    // 출발 칸 밖에 놓으면 대기장으로 되돌린다. 이미 놓였던 무리는 취소가 된다.
                    RestoreGroup(g, -1);
                    Refuse("출발 칸 안에 놓으시오");
                    return;
                }
                if (slotToGroup[slot] >= 0)
                {
                    RestoreGroup(g, dragOldSlot);
                    Refuse("빈 칸에 놓으시오");
                    WobbleSlot(slot);
                    return;
                }
                if (localPhase == LocalPhase.Practice && current.answerOrder[slot] != g)
                {
                    RestoreGroup(g, dragOldSlot);
                    Refuse("편차 식을 보고 알맞은 칸으로 옮겨라");
                    WobbleGroup(g);
                    return;
                }
                PlaceGroup(g, slot, true);
                if (localPhase == LocalPhase.Practice) ShowPracticeMath(g, slot);
                if (PlacedCount() == current.groups.Length) SubmitCurrent();
            }
        }

        void StartPractice()
        {
            localPhase = LocalPhase.Practice;
            current = ParadeRules.Practice();
            firstSubmission = true;
            sessionLeft = ParadeRules.SessionSeconds;
            ClearPlacements();
            SetTitleVisible(false);
            SetEndVisible(false);
            SetGameVisible(true);
            PrepareProblemVisuals(true);
            st.phase = "playing";
            st.level = 0;
            st.prompt = current.prompt;
            st.roundId = current.id;
            st.firstAttemptOpen = true;
            ShowMessage("편차 순으로 무리를 놓아 행진시켜라", 0f);
            MgfBridge.NotifyChanged();
        }

        void StartRun(bool resetScore)
        {
            runSerial++;
            deck = ParadeRules.RunDeck(runSerial);
            roundIndex = 0;
            if (resetScore) st.score = 0;
            st.lives = ParadeRules.StartLives;
            st.solved = 0;
            st.firstCorrect = 0;
            st.firstAttempts = 0;
            st.lastMisconception = "";
            sessionLeft = ParadeRules.SessionSeconds;
            pendingFail = false;
            endReason = "";
            LoadRound();
        }

        void LoadRound()
        {
            current = deck[roundIndex];
            localPhase = LocalPhase.Playing;
            firstSubmission = true;
            ClearPlacements();
            st.level = roundIndex + 1;
            st.phase = "playing";
            st.roundId = current.id;
            st.prompt = current.prompt;
            st.firstAttemptOpen = true;
            PrepareProblemVisuals(false);
            RefreshHud();
            MgfBridge.NotifyChanged();
        }

        void SubmitCurrent()
        {
            int[] order = CurrentOrder();
            bool correct = current.IsCorrect(order);
            revealCorrect = correct;
            revealPractice = localPhase == LocalPhase.Practice;
            pendingFail = false;
            string submittedText = current.OrderText(order);

            if (revealPractice)
            {
                if (!correct) { ClearPlacements(); Refuse("−2 < 0 < 2 순서를 다시 확인하라"); return; }
                st.score += 40;
                ShowReveal(true, "−2 < 0 < 2 · 편차=변량−평균");
                revealClock = 1.8f;
            }
            else
            {
                st.firstAttempts++;
                bool wasFirst = firstSubmission;
                firstSubmission = false;
                st.firstAttemptOpen = false;
                if (correct)
                {
                    st.solved++;
                    if (wasFirst) st.firstCorrect++;
                    st.score += wasFirst ? 180 : 100;
                    hitStopClock = .07f;
                    ShowReveal(true, CorrectEvidence());
                    revealClock = 1.65f;
                }
                else
                {
                    st.lives--;
                    st.lastMisconception = current.Diagnose(order);
                    shakeClock = .34f;
                    pendingFail = st.lives <= 0 || st.firstCorrect + (ParadeRules.GoalRounds - roundIndex - 1) < ParadeRules.RequiredFirstCorrect;
                    ShowReveal(false, WrongEvidence(order));
                    revealClock = 1.75f;
                }
            }
            st.currentPermutation = submittedText;
            localPhase = LocalPhase.Reveal;
            st.phase = "paused";
            RefreshHud();
            MgfBridge.NotifyChanged();
        }

        void FinishReveal()
        {
            HideReveal();
            if (revealPractice)
            {
                ShowConceptBridge("+2와 −2도 제곱하면 모두 4");
                StartRun(true);
                return;
            }
            if (!revealCorrect)
            {
                if (pendingFail) { EndRun(false, st.lives <= 0 ? "태엽 리본이 모두 풀렸다" : "첫 제출 5회를 채울 수 없다"); return; }
                localPhase = LocalPhase.Playing;
                st.phase = "playing";
                ClearPlacements();
                PrepareProblemVisuals(false);
                return;
            }
            roundIndex++;
            if (roundIndex >= ParadeRules.GoalRounds)
            {
                EndRun(st.firstCorrect >= ParadeRules.RequiredFirstCorrect && st.lives > 0,
                    st.firstCorrect >= ParadeRules.RequiredFirstCorrect ? "나선 행렬이 완성됐다" : "첫 제출이 부족하다");
            }
            else LoadRound();
        }

        void EndRun(bool won, string reason)
        {
            CancelDrag(true);
            localPhase = LocalPhase.End;
            endReason = reason;
            endClock = 0f;
            st.phase = won ? "clear" : "gameover";
            SetGameVisible(false);
            ShowEnd(won, reason);
            MgfSfx.Play(won ? "win" : "lose");
            MgfBridge.NotifyChanged();
        }

        string CorrectEvidence()
        {
            if (current.band == ParadeBand.Deviation)
                return current.AnswerText + " · 편차=변량−평균";
            if (current.band == ParadeBand.Variance)
                return current.AnswerText + " · 분산=Σ(편차²)÷4";
            return current.AnswerText + " · 표준편차=√분산";
        }

        string WrongEvidence(int[] order)
        {
            int at = 0;
            for (int i = 0; i < order.Length - 1; i++)
                if (current.groups[order[i]].key > current.groups[order[i + 1]].key) { at = i; break; }
            var a = current.groups[order[at]];
            var b = current.groups[order[Mathf.Min(at + 1, order.Length - 1)]];
            if (current.band == ParadeBand.Deviation)
                return a.label + " 편차 " + Signed(a.key) + " > " + b.label + " 편차 " + Signed(b.key);
            if (current.band == ParadeBand.Variance)
                return a.label + " 분산 " + a.key + " > " + b.label + " 분산 " + b.key;
            return a.label + "의 분산 " + a.key + " > " + b.label + "의 분산 " + b.key;
        }

        static string Signed(int v) { return v > 0 ? "+" + v : v.ToString().Replace("-", "−"); }

        int[] CurrentOrder()
        {
            int n = current.groups.Length;
            var a = new int[n];
            for (int i = 0; i < n; i++) a[i] = slotToGroup[i];
            return a;
        }

        void ClearPlacements()
        {
            for (int i = 0; i < 4; i++) { slotToGroup[i] = -1; groupToSlot[i] = -1; }
            dragGroup = -1;
            SyncPlacedState();
            ResetGroupTargets();
        }

        int PlacedCount()
        {
            int n = 0;
            if (current != null) for (int i = 0; i < current.groups.Length; i++) if (groupToSlot[i] >= 0) n++;
            return n;
        }

        void SyncPlacedState()
        {
            st.placedCount = PlacedCount();
            if (current == null) { st.currentPermutation = ""; return; }
            var s = new StringBuilder();
            for (int i = 0; i < current.groups.Length; i++)
            {
                if (i > 0) s.Append("|");
                s.Append(slotToGroup[i] < 0 ? "_" : current.groups[slotToGroup[i]].label);
            }
            st.currentPermutation = s.ToString();
            MgfBridge.NotifyChanged();
        }

        void PlaceGroup(int group, int slot, bool sound)
        {
            slotToGroup[slot] = group;
            groupToSlot[group] = slot;
            SetGroupTarget(group, SlotPosition(slot));
            TriggerDropJuice(group);
            if (sound) MgfSfx.Play("pop");
            SyncPlacedState();
        }

        void RestoreGroup(int group, int oldSlot)
        {
            if (oldSlot >= 0 && slotToGroup[oldSlot] < 0)
            {
                slotToGroup[oldSlot] = group; groupToSlot[group] = oldSlot;
                SetGroupTarget(group, SlotPosition(oldSlot));
            }
            else { groupToSlot[group] = -1; SetGroupTarget(group, HomePosition(group)); }
            SyncPlacedState();
        }

        void CancelDrag(bool restore)
        {
            if (dragGroup < 0) return;
            int g = dragGroup; dragGroup = -1; SetGroupSelected(g, false); HideDragTrail();
            if (restore) RestoreGroup(g, dragOldSlot);
        }

        void Refuse(string reason)
        {
            st.invalidInputs++;
            ShowMessage(reason, .9f);
            MgfSfx.Play("wrong");
            PulseGuide();
            MgfBridge.NotifyChanged();
        }

        void PushStateShape()
        {
            st.layoutLand = Screen.width >= 1024;
            st.firstAttemptOpen = firstSubmission && (localPhase == LocalPhase.Playing || localPhase == LocalPhase.Practice);
        }

        public void TestStart()
        {
            SetTitleVisible(false); SetEndVisible(false); SetGameVisible(true);
            StartRun(true);
        }

        public void TestAnswerCorrect()
        {
            if (localPhase != LocalPhase.Playing) TestStart();
            ClearPlacements();
            for (int slot = 0; slot < current.groups.Length; slot++) PlaceGroup(current.answerOrder[slot], slot, false);
            SubmitCurrent();
        }

        public void TestAnswerWrong()
        {
            if (localPhase != LocalPhase.Playing) TestStart();
            ClearPlacements();
            int[] wrong = (int[])current.answerOrder.Clone();
            int t = wrong[0]; wrong[0] = wrong[1]; wrong[1] = t;
            for (int slot = 0; slot < wrong.Length; slot++) PlaceGroup(wrong[slot], slot, false);
            SubmitCurrent();
        }

        public string StateJson() { return JsonUtility.ToJson(st); }
        public string ProblemBankJson() { return MgfJson.Bank(bank); }
    }
}
