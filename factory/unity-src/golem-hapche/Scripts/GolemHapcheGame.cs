// 골렘 합체 — 상태 머신과 유일한 답 입력(방패 하나를 다른 방패 위에 드래그해 놓기).
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.GolemHapche
{
    public partial class GolemHapcheGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int contractIndex;
            public int combo;
            public int firstAttemptTotal;
            public int firstAttemptCorrect;
            public int lastTwoFirstTry;
            public int attemptIndex;
            public int pointerVersion;
            public int selectedShield = -1;
            public int targetShield = -1;
            public int sealedArea;
            public int sealedVolume;
            public string problemId = "";
            public string prompt = "";
            public string condition = "";
            public string misconceptionId = "";
            public string endReason = "";
            public bool onboarding;
        }

        enum RunPhase { Title, Practice, Playing, Feedback, End }

        readonly State st = new State();
        readonly List<MgfProblem> bank = new List<MgfProblem>();
        List<GolemContract> deck;
        GolemContract current;
        RunPhase runPhase = RunPhase.Title;
        int runSerial;
        int dragShield = -1;
        int hoverShield = -1;
        int keyboardFocus;
        int keyboardHeld = -1;
        bool dragging;
        bool dragMoved;
        bool firstAttempt;
        int practiceTapShield = -1;
        bool feedbackCorrect;
        bool feedbackPractice;
        int feedbackA, feedbackB;
        float feedbackClock;
        float runLeft;
        float guideIdle;
        float practiceTapAssistClock;
        Vector2 dragStart;
        string endReason = "";

        void Awake()
        {
            MgfLook.Quality(38f);
            bank.AddRange(GolemRules.BuildBank());
            BuildWorld();
            BuildUi();
            Prewarm();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb = new StringBuilder("골렘합체닮은방패를포개라중학교2학년도형의닮음백색염전채석장흑요석산화동정오봉인석균열계약");
            sb.Append("SSS닮음SASAA대응변대응각끼인각끼인각이아닌각닮음비넓이의비부피의비가장간단한자연수의비");
            sb.Append("세대응변의비가같은두방패를포개시오길이배넓이배부피배봉인못정확도다시봉인게임오버숙련미달");
            sb.Append("같은수를더해도비는같아지지않는다그림위치가아니라기호에쓴꼭짓점순서로대응한다");
            sb.Append("마지막계약첫시도실패봉인불완전확정남은계약으로비례를익히시오실수개성공정답+/()0123456789거신은섰지만");
            for (int i = 0; i < bank.Count; i++)
            {
                sb.Append(bank[i].prompt); sb.Append(bank[i].answer); sb.Append(bank[i].unitConcept);
                if (bank[i].choices != null) for (int j = 0; j < bank[i].choices.Length; j++) sb.Append(bank[i].choices[j]);
            }
            MgfText.Prewarm(sb.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(.05f, Time.deltaTime);
            HandlePointer();
            HandleKeyboard();

            if (runPhase == RunPhase.Practice)
            {
                guideIdle += dt;
                if (guideIdle >= 8f) { guideIdle = 0f; BoostGuide(); }
                if (practiceTapShield >= 0)
                {
                    practiceTapAssistClock += dt;
                    if (practiceTapAssistClock >= .75f)
                    {
                        int from = practiceTapShield;
                        int to = from == current.correctA ? current.correctB : current.correctA;
                        practiceTapShield = -1; practiceTapAssistClock = 0f;
                        SubmitPair(from, to);
                    }
                }
            }
            else if (runPhase == RunPhase.Playing)
            {
                runLeft -= dt;
                if (runLeft <= 0f) { runLeft = 0f; EndRun("time"); }
            }
            else if (runPhase == RunPhase.Feedback)
            {
                feedbackClock += dt;
                float duration = feedbackPractice && feedbackCorrect ? 1.7f : feedbackCorrect ? 1.65f : 1.45f;
                if (feedbackClock >= duration) FinishFeedback();
            }

            UpdateWorld(dt);
            UpdateUi(dt);
        }

        void ShowTitle()
        {
            runPhase = RunPhase.Title;
            deck = null; current = GolemRules.Practice(); endReason = "";
            st.score = 0; st.lives = GolemRules.StartLives; st.level = 1; st.solved = 0;
            st.contractIndex = 0; st.combo = 0; st.firstAttemptTotal = 0; st.firstAttemptCorrect = 0; st.lastTwoFirstTry = 0;
            st.attemptIndex = 0; st.selectedShield = -1; st.targetShield = -1;
            st.sealedArea = 0; st.sealedVolume = 0; st.problemId = ""; st.prompt = "";
            st.condition = ""; st.misconceptionId = ""; st.endReason = ""; st.onboarding = false;
            dragShield = hoverShield = practiceTapShield = -1; practiceTapAssistClock = 0f; dragging = false; keyboardHeld = -1; runLeft = GolemRules.RunSeconds;
            ResetWorldForRun(); ShowTitleVisual(); SetScreen(); MgfBridge.NotifyChanged();
        }

        void ResetRun()
        {
            runSerial++;
            deck = GolemRules.RunDeck(runSerial);
            st.score = 0; st.lives = GolemRules.StartLives; st.level = 1; st.solved = 0;
            st.contractIndex = 0; st.combo = 0; st.firstAttemptTotal = 0; st.firstAttemptCorrect = 0; st.lastTwoFirstTry = 0;
            st.attemptIndex = 0; st.selectedShield = -1; st.targetShield = -1;
            st.sealedArea = 0; st.sealedVolume = 0; st.problemId = ""; st.prompt = "";
            st.condition = ""; st.misconceptionId = ""; st.endReason = ""; st.onboarding = false;
            endReason = ""; runLeft = GolemRules.RunSeconds; guideIdle = 0f; feedbackClock = 0f;
            dragShield = hoverShield = practiceTapShield = -1; practiceTapAssistClock = 0f; dragging = false; dragMoved = false; keyboardHeld = -1; keyboardFocus = 0;
            ResetWorldForRun();
        }

        void StartPractice()
        {
            ResetRun();
            runPhase = RunPhase.Practice;
            current = GolemRules.Practice();
            st.onboarding = true; firstAttempt = true; guideIdle = 0f;
            LayoutUi(true);
            LoadContract(current, 0);
            SetScreen(); StartGuide(); BeginTitleTransition();
            MgfSfx.Play("whoosh", .38f); MgfBridge.NotifyChanged();
        }

        void StartRunDirect()
        {
            ResetRun();
            runPhase = RunPhase.Playing;
            st.onboarding = false;
            LayoutUi(true);
            LoadNextContract();
            SetScreen(); MgfBridge.NotifyChanged();
        }

        void BeginMainAfterPractice()
        {
            st.score = 0; st.solved = 0; st.contractIndex = 0; st.combo = 0;
            st.firstAttemptTotal = 0; st.firstAttemptCorrect = 0; st.lastTwoFirstTry = 0;
            st.attemptIndex = 0; st.lives = GolemRules.StartLives; st.misconceptionId = "";
            st.onboarding = false; runLeft = GolemRules.RunSeconds;
            runPhase = RunPhase.Playing;
            ResetWorldForRun();
            LayoutUi(true);
            LoadNextContract();
            SwapGoalAfterPractice();
        }

        void LoadNextContract()
        {
            if (st.solved >= GolemRules.TargetContracts) { FinishMastery(); return; }
            current = deck[st.solved];
            LoadContract(current, st.solved);
        }

        void LoadContract(GolemContract contract, int index)
        {
            current = contract;
            st.contractIndex = index; st.problemId = contract.id; st.prompt = contract.prompt;
            st.condition = GolemRules.ConditionName(contract.kind); st.level = Math.Max(1, contract.band);
            st.attemptIndex = 0; st.misconceptionId = ""; st.selectedShield = -1; st.targetShield = -1;
            firstAttempt = true; dragShield = hoverShield = practiceTapShield = -1; practiceTapAssistClock = 0f; dragging = false; dragMoved = false;
            RefreshContractUi(); ResetWorldForContract(contract); MgfBridge.NotifyChanged();
        }

        void SubmitPair(int a, int b)
        {
            if ((runPhase != RunPhase.Practice && runPhase != RunPhase.Playing) || current == null) return;
            if (a < 0 || b < 0 || a == b) { RefuseInput("서로 다른 방패 두 개를 직접 포개시오."); return; }
            if (a > b) { int t = a; a = b; b = t; }
            practiceTapShield = -1; practiceTapAssistClock = 0f;

            feedbackPractice = runPhase == RunPhase.Practice;
            feedbackCorrect = GolemRules.IsCorrectPair(current, a, b);
            feedbackA = a; feedbackB = b;
            st.selectedShield = a; st.targetShield = b;

            if (feedbackPractice)
            {
                if (feedbackCorrect) { st.score = 120; st.solved = 1; }
                else { st.misconceptionId = GolemRules.MisconceptionId(current, a, b); firstAttempt = false; }
            }
            else
            {
                if (firstAttempt)
                {
                    st.firstAttemptTotal++;
                    if (feedbackCorrect)
                    {
                        st.firstAttemptCorrect++;
                        if (st.contractIndex >= GolemRules.TargetContracts - 2) st.lastTwoFirstTry++;
                    }
                }

                if (feedbackCorrect)
                {
                    st.combo++;
                    int award = (firstAttempt ? 180 : 70) + st.combo * 20;
                    st.score += award;
                    st.solved++;
                    st.sealedArea += current.ratioLarge * current.ratioLarge;
                    st.sealedVolume += current.ratioLarge * current.ratioLarge * current.ratioLarge;
                }
                else
                {
                    st.combo = 0; st.lives--; st.attemptIndex++;
                    st.misconceptionId = GolemRules.MisconceptionId(current, a, b);
                    firstAttempt = false;
                }
            }

            dragging = false; dragShield = hoverShield = -1;
            BeginFeedbackVisual(current, a, b, feedbackCorrect, feedbackPractice, st.combo);

            // 목숨 0은 연출 대기 없이 즉시 입력을 잠그고 gameover 상태가 된다.
            if (!feedbackPractice && !feedbackCorrect && st.lives <= 0)
            {
                EndRun("lives");
                return;
            }

            runPhase = RunPhase.Feedback;
            feedbackClock = 0f; SetScreen(); MgfBridge.NotifyChanged();
        }

        void FinishFeedback()
        {
            if (runPhase != RunPhase.Feedback) return;
            CompleteFeedbackVisual(feedbackCorrect);
            if (feedbackPractice)
            {
                if (feedbackCorrect) BeginMainAfterPractice();
                else
                {
                    runPhase = RunPhase.Practice; st.score = 0; st.solved = 0;
                    LoadContract(current, 0); StartGuide();
                }
            }
            else if (!feedbackCorrect)
            {
                runPhase = RunPhase.Playing;
                int attempts = st.attemptIndex;
                string misconception = st.misconceptionId;
                LoadContract(current, st.contractIndex);
                st.attemptIndex = attempts; st.misconceptionId = misconception; firstAttempt = false;
                ShowWrongReason(misconception);
            }
            else if (st.solved >= GolemRules.TargetContracts)
            {
                FinishMastery();
            }
            else
            {
                runPhase = RunPhase.Playing;
                LoadNextContract();
            }
            SetScreen(); MgfBridge.NotifyChanged();
        }

        void FinishPendingFeedback()
        {
            if (runPhase == RunPhase.Feedback) { feedbackClock = 99f; FinishFeedback(); }
        }

        void FinishMastery()
        {
            bool mastered = st.firstAttemptCorrect >= 5 && st.lastTwoFirstTry == 2;
            EndRun(mastered ? "clear" : "mastery");
        }

        void EndRun(string reason)
        {
            if (runPhase == RunPhase.End) return;
            runPhase = RunPhase.End; endReason = reason; dragging = false; dragShield = hoverShield = -1;
            st.endReason = reason;
            int best = PlayerPrefs.GetInt("golem-hapche.best", 0);
            if (st.score > best) { PlayerPrefs.SetInt("golem-hapche.best", st.score); PlayerPrefs.Save(); }
            ShowEnd(reason); SetScreen();
            MgfSfx.Play(reason == "clear" ? "win" : "lose", .62f); MgfBridge.NotifyChanged();
        }

        void HandlePointer()
        {
            if (MgfPointer.Down)
            {
                st.pointerVersion++;
                SpawnTapRipple(MgfPointer.Position);

                if (runPhase == RunPhase.Title)
                {
                    if (HitTitleSeal(MgfPointer.Position)) StartPractice();
                    else { RefuseInput("금이 간 봉인석을 눌러 채석장을 깨우시오."); PointToTitleSeal(); }
                    MgfBridge.NotifyChanged(); return;
                }
                if (runPhase == RunPhase.End)
                {
                    if (HitRestartSeal(MgfPointer.Position)) StartPractice();
                    else { RefuseInput("봉인석을 눌러 다시 시작하시오."); PointToRestartSeal(); }
                    MgfBridge.NotifyChanged(); return;
                }
                if (runPhase == RunPhase.Feedback)
                {
                    RefuseInput("비례 각인이 끝날 때까지 잠시 기다리시오.");
                    MgfBridge.NotifyChanged(); return;
                }
                if (runPhase != RunPhase.Practice && runPhase != RunPhase.Playing) return;

                int hit = runPhase == RunPhase.Practice
                    ? HitPracticeShield(MgfPointer.Position)
                    : HitShield(MgfPointer.Position);
                if (hit >= 0)
                {
                    dragging = true; dragMoved = false; dragShield = hit; hoverShield = -1;
                    dragStart = MgfPointer.Position; guideIdle = 0f;
                    st.selectedShield = hit; st.targetShield = -1;
                    BeginShieldDrag(hit); HideGuide(); MgfSfx.Play("tap", .22f);
                }
                else
                {
                    RefuseInput("검은 방패 하나를 잡아 다른 방패 위로 끌어 놓으시오.");
                    PointToActiveShield();
                }
                MgfBridge.NotifyChanged();
            }

            if (MgfPointer.Held && dragging)
            {
                Vector2 pos = MgfPointer.Position;
                if ((pos - dragStart).sqrMagnitude > 36f) dragMoved = true;
                int nextHover = ResolveDropTarget(pos, dragShield);
                if (nextHover != hoverShield)
                {
                    hoverShield = nextHover; st.targetShield = hoverShield;
                    SetHoverShield(hoverShield); RefreshMeasureUi(dragShield, hoverShield);
                    MgfBridge.NotifyChanged();
                }
                MoveShieldDrag(dragShield, pos);
            }

            if (MgfPointer.Up && dragging)
            {
                int from = dragShield, to = ResolveDropTarget(MgfPointer.Position, from);
                dragging = false; dragShield = hoverShield = -1; SetHoverShield(-1);
                if (to >= 0 && dragMoved)
                {
                    SubmitPair(from, to);
                }
                else if (runPhase == RunPhase.Practice && !dragMoved)
                {
                    if (practiceTapShield >= 0 && practiceTapShield != from)
                    {
                        int first = practiceTapShield; practiceTapShield = -1;
                        SubmitPair(first, from);
                    }
                    else
                    {
                        practiceTapShield = from;
                        practiceTapAssistClock = 0f;
                        st.selectedShield = from; st.targetShield = -1;
                        ReturnShield(from); FocusShield(from); StartGuide();
                        RefuseInput(GolemRules.Letters[from] + " 선택 · 반대 방패를 탭하시오.");
                        MgfBridge.NotifyChanged();
                    }
                }
                else
                {
                    ReturnShield(from);
                    RefuseInput(dragMoved ? "다른 방패의 중앙에 포개어 놓으시오." : "방패를 잡은 채 다른 방패까지 끌어 놓으시오.");
                    PointToActiveShield(); MgfBridge.NotifyChanged();
                }
            }
        }

        void HandleKeyboard()
        {
            if (runPhase != RunPhase.Practice && runPhase != RunPhase.Playing) return;
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                keyboardFocus = (keyboardFocus + 1) % GolemRules.ShieldCount;
                FocusShield(keyboardFocus); st.pointerVersion++; MgfBridge.NotifyChanged();
            }
            int delta = 0;
            if (Input.GetKeyDown(KeyCode.LeftArrow)) delta = -1;
            if (Input.GetKeyDown(KeyCode.RightArrow)) delta = 1;
            if (Input.GetKeyDown(KeyCode.UpArrow)) delta = -2;
            if (Input.GetKeyDown(KeyCode.DownArrow)) delta = 2;
            if (delta != 0)
            {
                keyboardFocus = (keyboardFocus + delta + GolemRules.ShieldCount) % GolemRules.ShieldCount;
                FocusShield(keyboardFocus); st.pointerVersion++; MgfBridge.NotifyChanged();
            }
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                st.pointerVersion++;
                if (keyboardHeld < 0) { keyboardHeld = keyboardFocus; BeginShieldDrag(keyboardHeld); }
                else if (keyboardHeld == keyboardFocus) { RefuseInput("방향키로 다른 방패를 고른 뒤 Enter를 누르시오."); }
                else { int from = keyboardHeld; keyboardHeld = -1; SubmitPair(from, keyboardFocus); }
                MgfBridge.NotifyChanged();
            }
        }

        public void TestStart()
        {
            FinishPendingFeedback();
            StartRunDirect();
        }

        public void TestAnswerCorrect()
        {
            FinishPendingFeedback();
            if (runPhase != RunPhase.Playing || current == null) StartRunDirect();
            SubmitPair(current.correctA, current.correctB);
        }

        public void TestAnswerWrong()
        {
            FinishPendingFeedback();
            if (runPhase != RunPhase.Playing || current == null) StartRunDirect();
            for (int i = 0; i < GolemRules.ShieldCount; i++)
            for (int j = i + 1; j < GolemRules.ShieldCount; j++)
                if (!GolemRules.IsCorrectPair(current, i, j)) { SubmitPair(i, j); return; }
        }

        public string StateJson()
        {
            st.phase = runPhase == RunPhase.Title ? "title" : runPhase == RunPhase.End ? (endReason == "clear" ? "clear" : "gameover") : "playing";
            st.onboarding = runPhase == RunPhase.Practice;
            st.level = current == null ? 1 : Math.Max(1, current.band);
            st.lives = Math.Max(0, st.lives);
            st.problemId = current == null ? "" : current.id;
            st.prompt = current == null ? "" : current.prompt;
            st.condition = current == null ? "" : GolemRules.ConditionName(current.kind);
            return JsonUtility.ToJson(st);
        }

        public string ProblemBankJson() => MgfJson.Bank(bank);
    }
}
