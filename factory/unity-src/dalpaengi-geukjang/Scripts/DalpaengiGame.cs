// 달팽이 극장 — 플레이 상태, 홀드/드래그/릴리스 입력, 실패와 회복 경로.
// 학생이 좌석 수를 직접 만들고 코드는 정수 일치만 판정한다.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.DalpaengiGeukjang
{
    public partial class DalpaengiGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int firstAttempts;
            public int firstCorrect;
            public int firstAccuracy;
            public int currentSeatCount;
            public int submittedValue;
            public int combo;
            public int pointerVersion;
            public string mode = "title";
            public string currentProblemId = "";
            public string prompt = "";
            public string lastError = "";
            public bool onboarding;
            public bool holding;
            public bool layoutLand;
        }

        enum LocalPhase { Title, SumPractice, ProductPractice, Playing, Reveal, End }

        readonly State st = new State();
        readonly List<MgfProblem> bank = new List<MgfProblem>();
        List<ShowProblem> deck;
        ShowProblem current;
        LocalPhase localPhase = LocalPhase.Title;
        int runSerial;
        int problemIndex;
        bool firstSubmission;
        bool revealCorrect;
        bool revealPractice;
        bool revealWasFirst;
        bool titleOpening;
        bool productPracticeDone;
        bool holding;
        bool manualMode;
        int holdStartSeats;
        float holdStartY;
        float holdClock;
        float autoClock;
        float revealClock;
        float titleClock;
        float idleClock;
        float sessionLeft = DalpaengiRules.SessionSeconds;
        string endReason = "";

        void Awake()
        {
            MgfLook.Quality(34f);
            string validationError;
            if (!DalpaengiRules.ValidateAll(out validationError))
                Debug.LogError("[달팽이 극장] 문제 풀 검증 실패: " + validationError);
            bank.AddRange(DalpaengiRules.BuildBank());
            PrewarmText();
            BuildWorld();
            BuildUi();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void PrewarmText()
        {
            var text = new StringBuilder("달팽이극장객석을펼쳐입장시켜라중학교2학년경우의수합의법칙곱의법칙전체부분독주곡합주곡무대배경각각하나씩한곡만고른다두목록에같은곡은없다모든조합이가능하다경우의수를구하시오입장종첫제출정확도연습공연완료폐장다시울리기");
            for (int i = 0; i < bank.Count; i++)
            {
                text.Append(bank[i].prompt);
                text.Append(bank[i].answer);
                text.Append(bank[i].unitConcept);
            }
            MgfText.Prewarm(text.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(.05f, Time.unscaledDeltaTime);
            bool layoutChanged = UpdateLayoutIfNeeded();
            if (layoutChanged && holding)
            {
                CancelHold("화면 방향이 바뀌어 펼치기를 멈췄다. 손잡이를 다시 잡으시오.");
            }

            HandleInput(dt);

            if (titleOpening)
            {
                titleClock += dt;
                if (titleClock >= .55f)
                {
                    titleOpening = false;
                    StartSumPractice();
                }
            }
            else if (localPhase == LocalPhase.Playing)
            {
                sessionLeft -= dt;
                idleClock += dt;
                if (sessionLeft <= 0f) EndRun(false, "120초가 지나 공연장이 문을 닫았다");
                else if (idleClock >= 8f)
                {
                    idleClock = 0f;
                    BoostGuide();
                    ShowNotice("잎 손잡이를 누르고, 계산한 자리 수에서 놓으시오.");
                }
            }
            else if (localPhase == LocalPhase.SumPractice || localPhase == LocalPhase.ProductPractice)
            {
                idleClock += dt;
                if (idleClock >= 8f)
                {
                    idleClock = 0f;
                    BoostGuide();
                    ShowNotice("맥동하는 잎 손잡이를 눌러 직접 펼쳐 보시오.");
                }
            }
            else if (localPhase == LocalPhase.Reveal)
            {
                revealClock += dt;
                if (revealClock >= (revealCorrect ? 1.75f : 1.45f)) FinishReveal();
            }

            UpdateWorldVisual(dt);
            UpdateUiVisual(dt);
        }

        void ShowTitle()
        {
            localPhase = LocalPhase.Title;
            titleOpening = false;
            productPracticeDone = false;
            current = null;
            deck = null;
            holding = false;
            st.score = 0;
            st.lives = DalpaengiRules.StartLives;
            st.level = 1;
            st.phase = "title";
            st.solved = 0;
            st.firstAttempts = 0;
            st.firstCorrect = 0;
            st.firstAccuracy = 0;
            st.currentSeatCount = 0;
            st.submittedValue = 0;
            st.combo = 0;
            st.mode = "title";
            st.currentProblemId = "";
            st.prompt = "";
            st.lastError = "";
            st.onboarding = false;
            st.holding = false;
            st.layoutLand = IsLand();
            endReason = "";
            ResetWorldForRun();
            ShowTitleVisual();
            RefreshUi();
            MgfBridge.NotifyChanged();
        }

        void ResetRun()
        {
            runSerial++;
            deck = DalpaengiRules.RunDeck(runSerial);
            problemIndex = 0;
            st.score = 0;
            st.lives = DalpaengiRules.StartLives;
            st.level = 1;
            st.solved = 0;
            st.firstAttempts = 0;
            st.firstCorrect = 0;
            st.firstAccuracy = 0;
            st.currentSeatCount = 0;
            st.submittedValue = 0;
            st.combo = 0;
            st.lastError = "";
            st.holding = false;
            holding = false;
            manualMode = false;
            sessionLeft = DalpaengiRules.SessionSeconds;
            idleClock = 0f;
            endReason = "";
            ResetWorldForRun();
        }

        void StartSumPractice()
        {
            ResetRun();
            localPhase = LocalPhase.SumPractice;
            st.phase = "playing";
            st.mode = "sum-practice";
            st.onboarding = true;
            current = DalpaengiRules.SumPractice();
            firstSubmission = true;
            BeginProblem();
            BeginPetalReveal();
            MgfSfx.Play("whoosh", .28f);
        }

        void StartRunDirect()
        {
            ResetRun();
            localPhase = LocalPhase.Playing;
            st.phase = "playing";
            st.mode = "sum";
            st.onboarding = false;
            productPracticeDone = true;
            problemIndex = 0;
            LoadProblem();
        }

        void StartMainAfterPractice()
        {
            // 연습은 점수·목숨·첫 제출 통계에 섞지 않는다.
            st.score = 0;
            st.lives = DalpaengiRules.StartLives;
            st.solved = 0;
            st.firstAttempts = 0;
            st.firstCorrect = 0;
            st.firstAccuracy = 0;
            st.combo = 0;
            st.lastError = "";
            st.onboarding = false;
            sessionLeft = DalpaengiRules.SessionSeconds;
            problemIndex = 0;
            localPhase = LocalPhase.Playing;
            st.phase = "playing";
            LoadProblem();
        }

        void StartProductPractice()
        {
            productPracticeDone = true;
            localPhase = LocalPhase.ProductPractice;
            st.phase = "playing";
            st.mode = "product-practice";
            st.onboarding = true;
            current = DalpaengiRules.ProductPractice();
            firstSubmission = true;
            BeginProblem();
            ShowNotice("두 칸짜리 공연표를 보고 2 × 2만큼 펼치시오.");
            BoostGuide();
        }

        void LoadProblem()
        {
            if (st.solved >= DalpaengiRules.Goal)
            {
                bool mastered = st.firstCorrect >= DalpaengiRules.RequiredFirstCorrect;
                EndRun(mastered, mastered ? "여덟 공연의 객석이 모두 찼다" : "첫 제출 정답이 6회에 미치지 못했다");
                return;
            }
            if (st.solved == 3 && !productPracticeDone)
            {
                StartProductPractice();
                return;
            }
            problemIndex = st.solved;
            current = deck[problemIndex];
            firstSubmission = true;
            localPhase = LocalPhase.Playing;
            st.phase = "playing";
            st.mode = current.kind == ShowKind.Sum ? "sum" : current.kind == ShowKind.Product ? "product" : "reverse";
            BeginProblem();
        }

        void BeginProblem()
        {
            st.currentProblemId = current.id;
            st.prompt = current.prompt;
            st.level = current.band <= 0 ? 1 : current.band;
            st.currentSeatCount = 0;
            st.submittedValue = 0;
            st.lastError = "";
            st.holding = false;
            holding = false;
            manualMode = false;
            idleClock = 0f;
            StartProblemVisual(current, IsPractice());
            RefreshUi();
            MgfBridge.NotifyChanged();
        }

        bool IsPractice()
        {
            return localPhase == LocalPhase.SumPractice || localPhase == LocalPhase.ProductPractice;
        }

        void HandleInput(float dt)
        {
            bool spaceDown = Input.GetKeyDown(KeyCode.Space);
            bool spaceHeld = Input.GetKey(KeyCode.Space);
            bool spaceUp = Input.GetKeyUp(KeyCode.Space);

            if (MgfPointer.Down || spaceDown)
            {
                st.pointerVersion++;
                MgfBridge.NotifyChanged();
                Vector2 pointer = MgfPointer.Position;
                RippleAt(pointer);

                if (localPhase == LocalPhase.Title)
                {
                    // 타이틀에서는 무음 버튼을 제외한 첫 월드 탭을 모두 하나뿐인 CTA인
                    // 입장종 동작으로 모은다. 종의 시각적 히트 영역은 그대로 유지하지만,
                    // 순진한 첫 사용자가 막·간판을 눌러도 무반응으로 30초 머물지 않는다.
                    if (!titleOpening)
                    {
                        titleOpening = true;
                        titleClock = 0f;
                        RingEntranceBell();
                        MgfSfx.Play("tap", .34f);
                    }
                    return;
                }

                if (localPhase == LocalPhase.End)
                {
                    if (spaceDown || HitRestartBell(pointer))
                    {
                        RingEntranceBell();
                        StartSumPractice();
                    }
                    else RefuseAt(pointer, "입장종을 눌러 새 공연을 여시오.");
                    return;
                }

                if (localPhase == LocalPhase.Reveal)
                {
                    RefuseAt(pointer, "관객 입장이 끝날 때까지 기다리시오.");
                    ReactAudience(false);
                    return;
                }

                if (localPhase != LocalPhase.Playing && !IsPractice()) return;

                if (spaceDown || HitLeafHandle(pointer)) BeginHold(pointer.y);
                else
                {
                    RefuseAt(pointer, "아래쪽 둥근 잎 손잡이를 누르시오.");
                    PointToHandle();
                    ReactAudience(false);
                }
            }

            if (holding && (MgfPointer.Held || spaceHeld))
            {
                float actualDt = dt;
                holdClock += actualDt;
                if (!spaceHeld)
                {
                    float dy = MgfPointer.Position.y - holdStartY;
                    if (!manualMode && Mathf.Abs(dy) >= 12f * ReferenceScale())
                    {
                        manualMode = true;
                        autoClock = 0f;
                    }
                    if (manualMode)
                    {
                        int delta = Mathf.RoundToInt(dy / (24f * ReferenceScale()));
                        SetSeatCount(Mathf.Clamp(holdStartSeats + delta, DalpaengiRules.MinSeats, DalpaengiRules.MaxSeats));
                    }
                }
                if (!manualMode)
                {
                    autoClock += actualDt;
                    while (autoClock >= .6f && st.currentSeatCount < DalpaengiRules.MaxSeats)
                    {
                        autoClock -= .6f;
                        SetSeatCount(st.currentSeatCount + 1);
                    }
                }
            }

            if (holding && (MgfPointer.Up || spaceUp))
            {
                holding = false;
                st.holding = false;
                EndHandlePress();
                if (holdClock < .16f)
                {
                    ShowNotice("누른 채 펼치고, 계산한 자리 수에서 놓으시오.");
                    BoostGuide();
                    MgfSfx.Play("tap", .16f);
                    MgfBridge.NotifyChanged();
                }
                else SubmitSeats(st.currentSeatCount);
            }

            if (!holding && (localPhase == LocalPhase.Playing || IsPractice()))
            {
                if (Input.GetKeyDown(KeyCode.UpArrow))
                    SetSeatCount(Mathf.Clamp(Math.Max(DalpaengiRules.MinSeats, st.currentSeatCount) + 1,
                        DalpaengiRules.MinSeats, DalpaengiRules.MaxSeats));
                if (Input.GetKeyDown(KeyCode.DownArrow))
                    SetSeatCount(Mathf.Clamp(Math.Max(DalpaengiRules.MinSeats, st.currentSeatCount) - 1,
                        DalpaengiRules.MinSeats, DalpaengiRules.MaxSeats));
            }
        }

        void BeginHold(float screenY)
        {
            holding = true;
            st.holding = true;
            manualMode = false;
            holdClock = 0f;
            autoClock = 0f;
            holdStartY = screenY;
            holdStartSeats = Mathf.Clamp(Math.Max(DalpaengiRules.MinSeats, st.currentSeatCount),
                DalpaengiRules.MinSeats, DalpaengiRules.MaxSeats);
            SetSeatCount(holdStartSeats);
            BeginHandlePress();
            idleClock = 0f;
            MgfSfx.Play("tap", .20f);
            MgfBridge.NotifyChanged();
        }

        void SetSeatCount(int count)
        {
            count = Mathf.Clamp(count, DalpaengiRules.MinSeats, DalpaengiRules.MaxSeats);
            if (st.currentSeatCount == count) return;
            int previous = st.currentSeatCount;
            st.currentSeatCount = count;
            AnimateSeatCounter(previous, count);
            SetOpenSeats(count);
            MgfSfx.Play("pop", .07f);
            MgfBridge.NotifyChanged();
        }

        void CancelHold(string reason)
        {
            holding = false;
            st.holding = false;
            EndHandlePress();
            ShowNotice(reason);
            MgfBridge.NotifyChanged();
        }

        void SubmitSeats(int submitted)
        {
            if ((localPhase != LocalPhase.Playing && !IsPractice()) || current == null) return;
            bool practice = IsPractice();
            bool correct = submitted == current.answer;
            revealPractice = practice;
            revealCorrect = correct;
            revealClock = 0f;
            revealWasFirst = firstSubmission;
            st.submittedValue = submitted;
            st.lastError = correct ? "" : DalpaengiRules.Diagnose(current, submitted);

            if (!practice && firstSubmission)
            {
                st.firstAttempts++;
                if (correct) st.firstCorrect++;
                firstSubmission = false;
                st.firstAccuracy = st.firstAttempts == 0 ? 0 :
                    Mathf.RoundToInt(100f * st.firstCorrect / st.firstAttempts);
            }

            if (correct)
            {
                if (!practice)
                {
                    st.solved++;
                    st.combo++;
                    st.score += 140 + current.band * 35 + Mathf.Min(5, st.combo) * 20;
                }
                localPhase = LocalPhase.Reveal;
                st.phase = "playing";
                BeginCorrectReveal(current, submitted, practice, st.combo);
                MgfSfx.Play("correct", .46f);
            }
            else
            {
                if (!practice)
                {
                    st.lives = Mathf.Max(0, st.lives - 1);
                    st.combo = 0;
                }
                localPhase = LocalPhase.Reveal;
                st.phase = "playing";
                BeginWrongReveal(current, submitted, st.lastError, practice);
                MgfSfx.Play("wrong", .34f);
                if (!practice && st.lives <= 0) endReason = "세 개의 입장종이 모두 깨졌다";
            }
            RefreshUi();
            MgfBridge.NotifyChanged();
        }

        void FinishReveal()
        {
            if (localPhase != LocalPhase.Reveal) return;
            CompleteRevealVisual(revealCorrect);
            if (revealCorrect)
            {
                if (revealPractice)
                {
                    if (current.kind == ShowKind.Sum) StartMainAfterPractice();
                    else
                    {
                        localPhase = LocalPhase.Playing;
                        st.phase = "playing";
                        st.onboarding = false;
                        LoadProblem();
                    }
                }
                else LoadProblem();
            }
            else
            {
                if (!revealPractice && st.lives <= 0) EndRun(false, endReason);
                else
                {
                    localPhase = revealPractice
                        ? (current.kind == ShowKind.Sum ? LocalPhase.SumPractice : LocalPhase.ProductPractice)
                        : LocalPhase.Playing;
                    st.phase = "playing";
                    st.currentSeatCount = 0;
                    st.submittedValue = 0;
                    holding = false;
                    st.holding = false;
                    idleClock = 0f;
                    ResumeAfterWrong(current, st.lastError, revealWasFirst);
                    SetOpenSeats(0);
                    BoostGuide();
                    RefreshUi();
                    MgfBridge.NotifyChanged();
                }
            }
        }

        void EndRun(bool clear, string reason)
        {
            if (localPhase == LocalPhase.End) return;
            localPhase = LocalPhase.End;
            st.phase = clear ? "clear" : "gameover";
            st.mode = clear ? "finale" : "closed";
            st.onboarding = false;
            st.holding = false;
            holding = false;
            endReason = reason;
            ShowEndVisual(clear, reason);
            RefreshUi();
            MgfBridge.NotifyChanged();
            MgfSfx.Play(clear ? "win" : "lose", .50f);
        }

        bool IsLand()
        {
            return Screen.width >= 1024 || (Screen.width >= Screen.height && Screen.width >= 700);
        }

        float ReferenceScale()
        {
            return Mathf.Sqrt(Mathf.Max(.30f, (Screen.width * Screen.height) / (390f * 844f)));
        }

        bool HitLeafHandle(Vector2 point)
        {
            // 390×844에서 화면 아래 56px 손잡이와 외곽 링 전체를 포함한다.
            // QA 격자 y=72%(상단 원점)에도 닿도록 하단 객석 부채 전체를 합법 grab 영역으로 둔다.
            if (IsLand())
                return point.x >= Screen.width * .30f && point.x <= Screen.width * .74f &&
                       point.y >= Screen.height * .08f && point.y <= Screen.height * .40f;
            return point.x >= Screen.width * .14f && point.x <= Screen.width * .86f &&
                   point.y >= Screen.height * .06f && point.y <= Screen.height * .36f;
        }

        bool HitTitleBell(Vector2 point)
        {
            Vector2 center = new Vector2(Screen.width * .5f, Screen.height * (IsLand() ? .23f : .21f));
            return Vector2.Distance(point, center) <= 112f * ReferenceScale();
        }

        bool HitRestartBell(Vector2 point)
        {
            Vector2 center = new Vector2(Screen.width * .5f, Screen.height * (IsLand() ? .20f : .18f));
            return Vector2.Distance(point, center) <= 112f * ReferenceScale();
        }

        public void TestStart()
        {
            StartRunDirect();
        }

        public void TestAnswerCorrect()
        {
            if (localPhase != LocalPhase.Playing) StartRunDirect();
            SetSeatCount(current.answer);
            SubmitSeats(current.answer);
        }

        public void TestAnswerWrong()
        {
            if (localPhase != LocalPhase.Playing) StartRunDirect();
            int wrong = current.misconceptionA;
            if (wrong < DalpaengiRules.MinSeats || wrong > DalpaengiRules.MaxSeats || wrong == current.answer)
                wrong = current.misconceptionB;
            if (wrong < DalpaengiRules.MinSeats || wrong > DalpaengiRules.MaxSeats || wrong == current.answer)
                wrong = current.answer == DalpaengiRules.MaxSeats ? current.answer - 1 : current.answer + 1;
            SetSeatCount(wrong);
            SubmitSeats(wrong);
        }

        public string StateJson() { return JsonUtility.ToJson(st); }
        public string ProblemBankJson() { return MgfJson.Bank(bank); }
    }
}
