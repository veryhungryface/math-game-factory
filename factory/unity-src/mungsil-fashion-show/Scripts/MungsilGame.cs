using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.MungsilFashionShow
{
    public partial class MungsilFashionShowGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int combo;
            public int rope;
            public int rouletteSpins;
            public int applause;
            public int firstAttemptTotal;
            public int firstAttemptCorrect;
            public int pointerVersion;
            public string problemId = "";
            public string misconceptionId = "";
            public bool onboarding;
        }

        enum ShowPhase { Title, Practice, Playing, Feedback, End }

        readonly State st = new State();
        readonly List<FashionOrder> catalog = new List<FashionOrder>();
        readonly List<MgfProblem> bank = new List<MgfProblem>();
        ShowPhase phase = ShowPhase.Title;
        FashionOrder current;
        int runSerial;
        int orderSerial;
        bool attempted;
        bool draggingRope;
        bool dragMoved;
        Vector2 dragStart;
        bool feedbackCorrect;
        bool feedbackPractice;
        float feedbackClock;
        float problemLeft;
        float idleGuide;
        string endReason = "";

        void Awake()
        {
            MgfLook.Quality(32f);
            catalog.AddRange(MungsilRules.BuildCatalog());
            for (int i = 0; i < catalog.Count; i++) bank.Add(catalog[i].ToMgf());
            Prewarm();
            BuildWorld();
            BuildUi();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb = new StringBuilder("뭉실패션쇼줄을끌어패턴랙을조립하라재단랙열기중학교2학년경우의수최고기록코디의경우의수만큼벨벳줄을끌고놓으시오모자리본룰렛핀쿠션바늘박수게이지앙코르연습주문정답오답부족초과빈옷걸이조립되지않은패턴심사위원무대폐막다시열기곱의법칙합의법칙서로다른두주사위동전순서쌍두자리자연수회장부회장대표직접나열");
            sb.Append("십의자리에는0이올수없습니다자격이다른역할은순서를구별합니다같은쌍은한번만셉니다");
            MgfText.Prewarm(sb.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(.05f, Time.deltaTime);
            HandleInput();

            if (phase == ShowPhase.Playing)
            {
                problemLeft -= dt;
                idleGuide += dt;
                if (problemLeft <= 0f) Resolve(false, "timeout");
                else if (idleGuide >= 8f) { idleGuide = 0f; BoostGuide(); }
            }
            else if (phase == ShowPhase.Practice)
            {
                idleGuide += dt; // 첫 조작을 끝낼 때까지 시간·피해·스폰 정지.
                if (idleGuide >= 8f) { idleGuide = 0f; BoostGuide(); }
            }
            else if (phase == ShowPhase.Feedback)
            {
                feedbackClock += dt;
                if (feedbackClock >= (feedbackCorrect ? 2.05f : 2.25f)) FinishFeedback();
            }

            UpdateWorld(dt);
            UpdateUi(dt);
        }

        void ShowTitle()
        {
            phase = ShowPhase.Title;
            current = MungsilRules.Practice();
            st.score = 0; st.lives = MungsilRules.StartLives; st.level = 1; st.solved = 0;
            st.combo = 0; st.rope = 0; st.rouletteSpins = 0; st.applause = 0;
            st.firstAttemptTotal = 0; st.firstAttemptCorrect = 0; st.pointerVersion = 0;
            st.problemId = current.id; st.misconceptionId = ""; st.onboarding = false; st.phase = "title";
            endReason = ""; draggingRope = false; problemLeft = 0f; idleGuide = 0f;
            ResetWorldForRun(); SetScreen(); MgfBridge.NotifyChanged();
        }

        void BeginRun(bool onboarding)
        {
            runSerial++; orderSerial = 0;
            phase = onboarding ? ShowPhase.Practice : ShowPhase.Playing;
            st.score = 0; st.lives = MungsilRules.StartLives; st.level = 1; st.solved = 0;
            st.combo = 0; st.rope = 0; st.rouletteSpins = 0; st.applause = 0;
            st.firstAttemptTotal = 0; st.firstAttemptCorrect = 0; st.misconceptionId = "";
            st.onboarding = onboarding; st.phase = "playing"; endReason = "";
            draggingRope = false; idleGuide = 0f;
            ResetWorldForRun();
            if (onboarding) LoadPractice(); else LoadOrder();
            SetScreen();
            if (onboarding) StartGuide();
            OpenCurtain(); MgfSfx.Play("whoosh", .30f); MgfBridge.NotifyChanged();
        }

        void LoadPractice()
        {
            current = MungsilRules.Practice();
            st.problemId = current.id; st.level = 1; st.rope = 0; st.rouletteSpins = 0;
            st.misconceptionId = ""; attempted = false; idleGuide = 0f;
            ResetWorldForProblem(); RefreshProblemUi(); SetRopeVisual(false);
        }

        void LoadOrder()
        {
            current = MungsilRules.PickSession(catalog, st.solved, runSerial, orderSerial);
            orderSerial++;
            st.problemId = current.id;
            st.level = st.solved < 2 ? 1 : st.solved < 5 ? 2 : 3;
            st.rope = 0; st.rouletteSpins = 0; st.misconceptionId = "";
            attempted = false; draggingRope = false; idleGuide = 0f;
            problemLeft = st.level == 1 ? 42f : st.level == 2 ? 34f : 27f;
            if (st.combo >= 2) problemLeft += 6f;
            ResetWorldForProblem(); RefreshProblemUi(); SetRopeVisual(false); MgfBridge.NotifyChanged();
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

            if (phase == ShowPhase.Title)
            {
                if (down)
                {
                    if (HitTitleSpool(MgfPointer.Position)) BeginRun(true);
                    else { Refuse("관객석 앞의 금색 줄 손잡이를 누르시오."); PointToTitleSpool(); }
                }
                return;
            }

            if (phase == ShowPhase.End)
            {
                if (down)
                {
                    if (HitEndSpool(MgfPointer.Position)) BeginRun(true);
                    else { Refuse("금색 실패를 눌러 새 쇼를 여시오."); PulseEndSpool(); }
                }
                return;
            }

            if (phase == ShowPhase.Feedback)
            {
                if (down) Refuse("뭉실이들이 무대를 확인 중입니다.");
                return;
            }

            if (phase != ShowPhase.Practice && phase != ShowPhase.Playing) return;

            if (down)
            {
                int roulette = HitRoulette(MgfPointer.Position);
                if (roulette >= 0) { SpinRoulette(roulette); return; }
                if (HitRopeTrack(MgfPointer.Position))
                {
                    draggingRope = true; dragMoved = false; dragStart = MgfPointer.Position;
                    AnticipateRope(); SetRopeFromPointer(MgfPointer.Position, false); MgfSfx.Play("tap", .18f); return;
                }
                Refuse(RopeGoal()); PointToActiveControl();
            }

            if (MgfPointer.Held && draggingRope)
            {
                if ((MgfPointer.Position - dragStart).sqrMagnitude > 100f) dragMoved = true;
                SetRopeFromPointer(MgfPointer.Position, true);
            }

            if (MgfPointer.Up && draggingRope)
            {
                draggingRope = false; ReleaseRope();
                if (!dragMoved) { Refuse("줄을 다른 칸까지 끌어 놓으시오."); BoostGuide(); return; }
                Submit();
            }
        }

        void SpinRoulette(int which)
        {
            int limit = phase == ShowPhase.Practice || st.level == 1 ? 99 : st.level == 2 ? 3 : 0;
            if (limit == 0) { Refuse("이번 주문은 조건만 보고 계산하시오."); PointToActiveControl(); return; }
            if (st.rouletteSpins >= limit) { Refuse("룰렛 확인은 세 번까지입니다. 이제 계산하시오."); PointToActiveControl(); return; }
            st.rouletteSpins++;
            idleGuide = 0f;
            PlayRoulette(which, st.rouletteSpins);
            RefreshProblemUi();
            MgfSfx.Play("pop", .18f);
            MgfBridge.NotifyChanged();
        }

        void SetRopeFromPointer(Vector2 screen, bool sound)
        {
            float t = RopeFraction(screen);
            int value = Mathf.Clamp(Mathf.RoundToInt(1f + t * (MungsilRules.MaxRope - 1)), 1, MungsilRules.MaxRope);
            SetRope(value, sound);
        }

        void SetRope(int value, bool sound)
        {
            value = Mathf.Clamp(value, 1, MungsilRules.MaxRope);
            if (st.rope == value) return;
            st.rope = value; idleGuide = 0f; st.misconceptionId = "";
            SetRopeVisual(true);
            if (sound && value % 2 == 0) MgfSfx.Play("tap", .075f);
            MgfBridge.NotifyChanged();
        }

        void Submit()
        {
            if (phase != ShowPhase.Practice && phase != ShowPhase.Playing) return;
            if (st.rope <= 0) { Refuse("벨벳 줄을 한 칸 이상 끌어 놓으시오."); return; }
            if (phase == ShowPhase.Practice && st.rouletteSpins < 1)
            {
                Refuse("먼저 모자나 리본 룰렛을 한 번 돌려 조합을 확인하시오.");
                PointToRoulette(); return;
            }
            bool correct = st.rope == current.answer;
            Resolve(correct, correct ? "" : current.MisconceptionFor(st.rope));
        }

        void Resolve(bool correct, string misconception)
        {
            if (phase != ShowPhase.Practice && phase != ShowPhase.Playing) return;
            // A fast first answer can arrive while the practice-complete toast is still fading.
            // Feedback owns this part of the stage, so never let the two messages overlap.
            toastT = 0f;
            toastG.alpha = 0f;
            bool practice = phase == ShowPhase.Practice;
            feedbackPractice = practice; feedbackCorrect = correct; feedbackClock = 0f;
            phase = ShowPhase.Feedback; draggingRope = false; st.misconceptionId = misconception;

            if (correct)
            {
                if (!practice)
                {
                    if (!attempted) { st.firstAttemptTotal++; st.firstAttemptCorrect++; }
                    st.solved++; st.combo++; st.applause = Mathf.Min(100, st.applause + 14 + st.combo * 3);
                    st.score += 120 + st.combo * 25 + st.level * 20;
                }
                st.phase = "playing";
                PlayCorrect(current.answer, st.combo, current.reveal);
                MgfSfx.Play("correct", .38f);
            }
            else
            {
                if (!practice)
                {
                    if (!attempted) st.firstAttemptTotal++;
                    attempted = true; st.combo = 0; st.applause = Mathf.Max(0, st.applause - 18); st.lives--;
                }
                st.phase = st.lives <= 0 ? "gameover" : "playing";
                PlayWrong(st.rope, current.answer, misconception, CorrectExplanation());
                MgfSfx.Play("wrong", .32f);
            }
            RefreshHud(); MgfBridge.NotifyChanged();
        }

        string CorrectExplanation()
        {
            switch (current.kind)
            {
                case OrderKind.Product:
                case OrderKind.ErrorFind: return $"{current.a}×{current.b}={current.answer}가지입니다.";
                case OrderKind.Sum: return $"동시에 일어나지 않으므로 {current.a}+{current.b}={current.answer}가지입니다.";
                case OrderKind.ZeroCards: return current.reveal + ".";
                case OrderKind.OrderedRoles: return $"역할이 달라 {current.a}×{current.a - 1}={current.answer}가지입니다.";
                case OrderKind.RepresentativePair: return $"같은 쌍을 합쳐 {current.a}×{current.a - 1}÷2={current.answer}가지입니다.";
                default: return current.reveal + ".";
            }
        }

        void FinishFeedback()
        {
            if (phase != ShowPhase.Feedback) return;
            StopFeedbackVisuals();
            if (feedbackPractice)
            {
                if (feedbackCorrect)
                {
                    phase = ShowPhase.Playing; st.onboarding = false; LoadOrder(); SetScreen();
                    ShowToast("연습 완료. 이제 8개 주문의 줄 위치를 정하시오.");
                }
                else
                {
                    phase = ShowPhase.Practice; st.rope = 0; ResetWorldForProblem(); RefreshProblemUi(); SetRopeVisual(false); StartGuide();
                }
            }
            else if (feedbackCorrect)
            {
                if (st.solved >= MungsilRules.Goal && st.firstAttemptCorrect >= 6) EndRun(true, "encore");
                else { phase = ShowPhase.Playing; LoadOrder(); SetScreen(); }
            }
            else
            {
                if (st.lives <= 0) EndRun(false, "needles-empty");
                else
                {
                    phase = ShowPhase.Playing; st.rope = 0;
                    problemLeft = st.level == 1 ? 42f : st.level == 2 ? 34f : 27f;
                    ResetWorldForProblem(); RefreshProblemUi(); SetRopeVisual(false);
                    ShowToast("같은 주문입니다. 식을 고쳐 다시 줄을 놓으시오.");
                }
            }
            MgfBridge.NotifyChanged();
        }

        void EndRun(bool clear, string reason)
        {
            phase = ShowPhase.End; endReason = reason;
            st.phase = clear ? "clear" : "gameover"; st.onboarding = false;
            ShowEnd(clear); MgfSfx.Play(clear ? "win" : "lose", .40f); MgfBridge.NotifyChanged();
        }

        void ReadyForHook()
        {
            if (phase == ShowPhase.Title || phase == ShowPhase.End || phase == ShowPhase.Practice) BeginRun(false);
            else if (phase == ShowPhase.Feedback)
            {
                feedbackClock = 99f; FinishFeedback();
                if (phase == ShowPhase.End) BeginRun(false);
            }
        }

        public void TestStart() { BeginRun(false); }
        public void TestAnswerCorrect() { ReadyForHook(); SetRope(current.answer, false); Submit(); }
        public void TestAnswerWrong()
        {
            ReadyForHook();
            int wrong = current.distractorA;
            if (wrong == current.answer || wrong < 1 || wrong > MungsilRules.MaxRope) wrong = current.answer == 1 ? 2 : current.answer - 1;
            SetRope(wrong, false); Submit();
        }
        public string StateJson() => JsonUtility.ToJson(st);
        public string ProblemBankJson() => MgfJson.Bank(bank);
    }
}
