using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.PpallaetjulChulhang
{
    public partial class PpallaetjulGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int combo;
            public int firstAttemptTotal;
            public int firstAttemptCorrect;
            public int finalBandFirst;
            public int attempts;
            public int presented;
            public int pointerVersion;
            public int selectedI = -1;
            public int selectedJ = -1;
            public string problemId = "";
            public string misconceptionId = "";
            public string result = "";
            public bool firstAttempt;
            public bool onboarding;
            public bool locked;
        }

        enum RunPhase { Title, Practice, Playing, Feedback, End }

        readonly State st = new State();
        readonly List<SailProblem> catalog = new List<SailProblem>();
        readonly List<MgfProblem> bank = new List<MgfProblem>();
        readonly List<SailProblem> deck = new List<SailProblem>(PpallaetjulRules.Goal);
        RunPhase runPhase = RunPhase.Title;
        SailProblem current;
        System.Random runRng;
        int runSerial;
        int deckIndex;
        bool dragging;
        bool firstTry;
        bool feedbackCorrect;
        bool feedbackPractice;
        float feedbackT;
        float sessionLeft;
        float guideIdle;
        string endReason = "";
        int keyboardI = 4, keyboardJ = 4, keyboardSide;

        void Awake()
        {
            MgfLook.Quality(38f);
            catalog.AddRange(PpallaetjulRules.BuildCatalog());
            for (int i = 0; i < catalog.Count; i++) bank.Add(catalog[i].ToMgf(i + 1));
            Prewarm();
            BuildWorld();
            BuildUi();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb = new StringBuilder("빨랫줄출항돛을닮게잘라라중학교2학년도형의닮음비갠벽돌세탁골목재단사달팽이황동가위법랑집게곤돌라주문남길돛수선패치시간첫시도연속출항연습다른변까지그으시오두대응변의비와그끼인각이같다닮음넓이의비절단선수리완료실패다시자르기출항했지만판단연습이더필요하다완벽한항로공통각대응변0123456789APQBCSAS∽△∠=:cm");
            for (int i = 0; i < bank.Count; i++) { sb.Append(bank[i].prompt); sb.Append(bank[i].answer); sb.Append(bank[i].unitConcept); }
            MgfText.Prewarm(sb.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(.05f, Time.deltaTime);
            HandleInput();

            if (runPhase == RunPhase.Playing)
            {
                if (transitionT <= 0f) sessionLeft -= dt;
                guideIdle += dt;
                if (sessionLeft <= 0f) EndSession("time");
            }
            else if (runPhase == RunPhase.Practice)
            {
                // 첫 실제 절단을 성공할 때까지 판정·타이머·패치·스폰을 모두 동결한다.
                guideIdle += dt;
                if (guideIdle >= 8f) { guideIdle = 0f; BoostGuide(); }
            }
            else if (runPhase == RunPhase.Feedback)
            {
                feedbackT += dt;
                float wait = feedbackCorrect ? 1.55f : 1.25f;
                if (feedbackT >= wait) FinishFeedback();
            }

            UpdateView(dt);
        }

        void ShowTitle()
        {
            runPhase = RunPhase.Title;
            current = PpallaetjulRules.Practice();
            st.score = 0; st.lives = PpallaetjulRules.StartPatches; st.level = 1; st.solved = 0;
            st.combo = 0; st.firstAttemptTotal = 0; st.firstAttemptCorrect = 0; st.finalBandFirst = 0;
            st.attempts = 0; st.presented = 0; st.selectedI = st.selectedJ = -1;
            st.problemId = current.id; st.misconceptionId = ""; st.result = "";
            st.firstAttempt = true; st.onboarding = false; st.locked = false; st.phase = "title";
            sessionLeft = PpallaetjulRules.SessionSeconds; guideIdle = 0f; dragging = false; endReason = "";
            ShowTitleVisual();
            SetScreen();
            MgfBridge.NotifyChanged();
        }

        void BeginSession(bool onboarding)
        {
            runSerial++;
            runRng = new System.Random(20261013 + runSerial * 7919 + Environment.TickCount % 997);
            BuildDeck();
            deckIndex = 0;
            st.score = 0; st.lives = PpallaetjulRules.StartPatches; st.level = 1; st.solved = 0;
            st.combo = 0; st.firstAttemptTotal = 0; st.firstAttemptCorrect = 0; st.finalBandFirst = 0;
            st.attempts = 0; st.presented = 0; st.selectedI = st.selectedJ = -1;
            st.misconceptionId = ""; st.result = ""; st.onboarding = onboarding; st.locked = false;
            st.phase = "playing"; sessionLeft = PpallaetjulRules.SessionSeconds; guideIdle = 0f;
            keyboardI = keyboardJ = 4; keyboardSide = 0; dragging = false; endReason = "";
            ResetRunVisual();

            if (onboarding)
            {
                runPhase = RunPhase.Practice;
                LoadPractice();
            }
            else
            {
                runPhase = RunPhase.Playing;
                LoadProblem(0);
            }
            SetScreen();
            MgfSfx.Play("whoosh", .28f);
            MgfBridge.NotifyChanged();
        }

        void BuildDeck()
        {
            deck.Clear();
            SailOrderKind[] kinds =
            {
                SailOrderKind.SimilarityRatio, SailOrderKind.SimilarityRatio, SailOrderKind.GivenAP,
                SailOrderKind.GivenAP, SailOrderKind.RepairCut, SailOrderKind.SimilarityRatio,
                SailOrderKind.AreaRatio, SailOrderKind.GivenAP, SailOrderKind.RepairCut
            };
            var used = new HashSet<string>();
            for (int n = 0; n < kinds.Length; n++)
            {
                int k = runRng.Next(1, 8);
                var matches = new List<SailProblem>();
                for (int i = 0; i < catalog.Count; i++)
                    if (catalog[i].kind == kinds[n] && catalog[i].k == k && !used.Contains(catalog[i].id)) matches.Add(catalog[i]);
                SailProblem pick = matches[runRng.Next(matches.Count)];
                used.Add(pick.id); deck.Add(pick);
            }
        }

        void LoadPractice()
        {
            current = PpallaetjulRules.Practice();
            firstTry = true; dragging = false; guideIdle = 0f;
            st.problemId = current.id; st.level = 1; st.selectedI = st.selectedJ = -1;
            st.misconceptionId = ""; st.firstAttempt = true; st.onboarding = true;
            LoadProblemVisual(true);
            StartGuide();
        }

        void LoadProblem(int index)
        {
            deckIndex = Mathf.Clamp(index, 0, deck.Count - 1);
            current = deck[deckIndex];
            current.band = 1 + deckIndex / 3;
            firstTry = true; dragging = false; guideIdle = 0f;
            st.problemId = current.id; st.level = current.band; st.selectedI = st.selectedJ = -1;
            st.misconceptionId = ""; st.firstAttempt = true; st.onboarding = false; st.presented++;
            keyboardI = keyboardJ = 4; keyboardSide = 0;
            LoadProblemVisual(false);
            MgfBridge.NotifyChanged();
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

            if (runPhase == RunPhase.Title)
            {
                if (down)
                {
                    // 첫 화면은 설명을 읽고 작은 히트 영역을 찾아내는 시험이 아니다.
                    // 어느 곳을 눌러도 실제 연습 절단으로 이어지고, 가위는 시작 위치를 시각적으로 알려 준다.
                    BeginSession(true);
                }
                return;
            }

            if (runPhase == RunPhase.End)
            {
                if (down)
                {
                    if (HitRestart(MgfPointer.Position)) BeginSession(false);
                    else { Refuse("결과 현수막의 가위를 눌러 다시 자르시오."); PointToRestart(); }
                }
                return;
            }

            if (runPhase == RunPhase.Feedback)
            {
                if (down) Refuse("절단 결과를 확인 중이다. 잠시 기다리시오.");
                return;
            }

            if (runPhase != RunPhase.Practice && runPhase != RunPhase.Playing) return;

            if (down)
            {
                int side, tick;
                if (TryStartCut(MgfPointer.Position, out side, out tick))
                {
                    dragging = true; guideIdle = 0f; HideGuide();
                    if (side == 0) { st.selectedI = tick; st.selectedJ = -1; }
                    else { st.selectedJ = tick; st.selectedI = -1; }
                    st.firstAttempt = firstTry;
                    MgfSfx.Play("tap", .24f); MgfBridge.NotifyChanged();
                }
                else
                {
                    Refuse("돛의 한 변 눈금에서 눌러 다른 변 눈금까지 그으시오.");
                    NudgeSail(); PointToSail(); MgfSfx.Play("wrong", .13f);
                }
            }

            if (dragging && MgfPointer.Held) DragCut(MgfPointer.Position);

            if (dragging && MgfPointer.Up)
            {
                dragging = false;
                int i, j;
                if (TryFinishCut(MgfPointer.Position, out i, out j)) Submit(i, j);
                else
                {
                    st.selectedI = st.selectedJ = -1;
                    Refuse("다른 변의 눈금까지 그으시오.");
                    RejectCut(); PointToSail(); MgfSfx.Play("wrong", .18f); MgfBridge.NotifyChanged();
                }
            }

            HandleKeyboard();
        }

        void HandleKeyboard()
        {
            bool changed = false;
            if (Input.GetKeyDown(KeyCode.Tab)) { keyboardSide = 1 - keyboardSide; changed = true; }
            int delta = Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.DownArrow) ? -1
                : Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.UpArrow) ? 1 : 0;
            if (delta != 0)
            {
                if (keyboardSide == 0) keyboardI = Mathf.Clamp(keyboardI + delta, 1, 7);
                else keyboardJ = Mathf.Clamp(keyboardJ + delta, 1, 7);
                changed = true;
            }
            if (changed)
            {
                st.selectedI = keyboardI; st.selectedJ = keyboardJ;
                UpdateKeyboardCut(keyboardI, keyboardJ, keyboardSide);
                MgfBridge.NotifyChanged();
            }
            if (Input.GetKeyDown(KeyCode.Space)) Submit(keyboardI, keyboardJ);
        }

        void Submit(int i, int j)
        {
            if (runPhase != RunPhase.Practice && runPhase != RunPhase.Playing) return;
            bool practice = runPhase == RunPhase.Practice;
            bool correct = current.IsCorrect(i, j);
            feedbackCorrect = correct; feedbackPractice = practice; feedbackT = 0f;
            runPhase = RunPhase.Feedback;
            st.selectedI = i; st.selectedJ = j; st.attempts++;

            if (correct)
            {
                if (!practice)
                {
                    if (firstTry)
                    {
                        st.firstAttemptTotal++; st.firstAttemptCorrect++;
                        if (deckIndex >= 6) st.finalBandFirst++;
                    }
                    st.solved++; st.combo++;
                    st.score += 120 + current.band * 40 + Mathf.Min(5, st.combo) * 25;
                }
                st.misconceptionId = "";
                OnCorrect(current, i, j, practice, st.combo);
                MgfSfx.Play("correct", .45f);
            }
            else
            {
                st.misconceptionId = PpallaetjulRules.MisconceptionFor(current, i, j);
                if (!practice)
                {
                    if (firstTry) st.firstAttemptTotal++;
                    st.lives--; st.combo = 0;
                }
                OnWrong(current, i, j, practice, st.misconceptionId);
                MgfSfx.Play("wrong", .40f);
            }
            firstTry = false; st.firstAttempt = false;
            SetScreen();
            MgfBridge.NotifyChanged();
        }

        void FinishFeedback()
        {
            HideFeedback();
            if (feedbackPractice)
            {
                if (feedbackCorrect)
                {
                    st.onboarding = false;
                    runPhase = RunPhase.Playing;
                    LoadProblem(0);
                    SwapGoalAfterPractice();
                }
                else
                {
                    runPhase = RunPhase.Practice;
                    LoadPractice();
                }
                SetScreen();
                return;
            }

            if (st.lives <= 0) { EndSession("patches"); return; }
            if (!feedbackCorrect)
            {
                runPhase = RunPhase.Playing;
                st.firstAttempt = false;
                ResumeRepairVisual();
                SetScreen();
                return;
            }

            if (st.solved >= PpallaetjulRules.Goal)
            {
                bool mastered = st.firstAttemptCorrect >= 7 && st.finalBandFirst >= 2;
                EndSession(mastered ? "clear" : "mastery");
                return;
            }

            int next = deckIndex + 1;
            runPhase = RunPhase.Playing;
            if (next == 3) ShowBandTransition(2);
            if (next == 6) ShowBandTransition(3);
            LoadProblem(next);
            SetScreen();
        }

        void EndSession(string reason)
        {
            endReason = reason;
            runPhase = RunPhase.End;
            dragging = false; st.locked = true; st.result = reason;
            st.phase = reason == "clear" ? "clear" : "gameover";
            PrepareEnd(reason);
            SetScreen();
            MgfSfx.Play(reason == "clear" ? "win" : "lose", .52f);
            MgfBridge.NotifyChanged();
        }

        void EnsureTestPlayable()
        {
            if (runPhase == RunPhase.Title || runPhase == RunPhase.End) BeginSession(false);
            else if (runPhase == RunPhase.Practice)
            {
                runPhase = RunPhase.Playing; st.onboarding = false; LoadProblem(0); SetScreen();
            }
            else if (runPhase == RunPhase.Feedback)
            {
                HideFeedback();
                if (st.lives <= 0 || st.solved >= PpallaetjulRules.Goal) BeginSession(false);
                else if (feedbackCorrect) { runPhase = RunPhase.Playing; LoadProblem(Mathf.Min(deckIndex + 1, deck.Count - 1)); }
                else { runPhase = RunPhase.Playing; ResumeRepairVisual(); }
                SetScreen();
            }
        }

        // ── IMgfGame: 검증 훅도 실제 Submit 정수 판정 경로를 그대로 탄다.
        public void TestStart() { BeginSession(false); }

        public void TestAnswerCorrect()
        {
            EnsureTestPlayable();
            Submit(current.k, current.k);
        }

        public void TestAnswerWrong()
        {
            EnsureTestPlayable();
            Submit(current.wrong1I, current.wrong1J);
        }

        public string StateJson() { return JsonUtility.ToJson(st); }
        public string ProblemBankJson() { return MgfJson.Bank(bank); }
    }
}
