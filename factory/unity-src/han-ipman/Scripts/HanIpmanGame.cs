// 한 입만 — 상태 머신과 실제 중심→외곽 포인터 절단 입력.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.HanIpman
{
    public partial class HanIpmanGame : MonoBehaviour, IMgfGame
    {
        // Unity의 Cylinder 프리미티브는 내부적으로 CapsuleCollider를 붙인다. 코드에서 타입을 직접
        // 참조하지 않으면 WebGL 엔진 스트리핑이 타입을 제거해 CreatePrimitive가 콘솔 오류를 낸다.
        static readonly Type PreserveCapsuleColliderType = typeof(CapsuleCollider);
        [Serializable]
        sealed class State : MgfState
        {
            public int roundId;
            public string problemId = "";
            public int inputRevision;
            public int qInput;
            public int submittedCount;
            public int firstAttempted;
            public int firstCorrect;
            public int firstWrong;
            public int combo;
            public bool repairMode;
            public bool dragging;
            public int oaTick;
            public string orderType = "";
            public string prompt = "";
            public string misconceptionId = "";
            public int cakeXPermille;
            public int cakeYPermille;
            public int outerRadiusPermille;
        }

        enum Flow { Title, Opening, Practice, Playing, Reveal, End }
        readonly State st = new State();
        readonly List<MgfProblem> bank = new List<MgfProblem>(322);
        Flow flow = Flow.Title;
        List<CakeProblem> deck;
        CakeProblem current;
        int runSerial;
        bool firstAttempt;
        bool revealCorrect;
        bool revealPractice;
        float timeLeft;
        float revealClock;
        float openingClock;
        float idleGuide;
        string endReason = "";

        void Awake()
        {
            GC.KeepAlive(PreserveCapsuleColliderType);
            MgfLook.Quality(32f);
            bank.AddRange(HanIpmanRules.BuildBank());
            BuildWorld();
            BuildUi();
            Prewarm();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb = new StringBuilder("한입만케이크를잘라라중학교1학년평면도형의성질호의길이부채꼴넓이중심각반지름남은부분주문아치타일무대완성첫제출냅킨연속수선영업성공종료원밖에서놓으시오반시계방향");
            for (int i = 0; i < bank.Count; i++) { sb.Append(bank[i].prompt); sb.Append(bank[i].answer); sb.Append(bank[i].unitConcept); }
            MgfText.Prewarm(sb.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(.05f, Time.deltaTime);
            HandleInput();
            if (flow == Flow.Opening)
            {
                openingClock += dt;
                if (openingClock >= .34f) BeginPractice();
            }
            else if (flow == Flow.Practice)
            {
                idleGuide += dt;
                if (idleGuide >= 8f) { idleGuide = 0f; BoostGuide(); }
            }
            else if (flow == Flow.Playing)
            {
                timeLeft -= dt;
                if (timeLeft <= 0f) EndRun("timer");
            }
            else if (flow == Flow.Reveal)
            {
                revealClock += dt;
                float wait = revealCorrect ? (revealPractice ? 1.15f : .92f) : 1.20f;
                if (revealClock >= wait) FinishReveal();
            }
            UpdateWorld(dt);
            UpdateUi(dt);
        }

        void ShowTitle()
        {
            flow = Flow.Title;
            st.score = 0; st.lives = HanIpmanRules.StartLives; st.level = 1; st.solved = 0;
            st.roundId = 0; st.problemId = ""; st.inputRevision = 0; st.qInput = 0; st.submittedCount = 0;
            st.firstAttempted = 0; st.firstCorrect = 0; st.firstWrong = 0; st.combo = 0; st.repairMode = false;
            st.dragging = false; st.oaTick = 0; st.orderType = ""; st.prompt = ""; st.misconceptionId = "";
            current = null; deck = null; endReason = ""; timeLeft = HanIpmanRules.SessionSeconds;
            ResetRunVisuals();
            SetScreen();
            MgfBridge.NotifyChanged();
        }

        void ResetRun()
        {
            runSerial++;
            deck = HanIpmanRules.RunDeck(runSerial);
            st.score = 0; st.lives = HanIpmanRules.StartLives; st.level = 1; st.solved = 0;
            st.roundId = 0; st.problemId = ""; st.qInput = 0; st.submittedCount = 0;
            st.firstAttempted = 0; st.firstCorrect = 0; st.firstWrong = 0; st.combo = 0; st.repairMode = false;
            st.dragging = false; st.misconceptionId = ""; endReason = "";
            timeLeft = HanIpmanRules.SessionSeconds; revealClock = 0f; idleGuide = 0f;
            ResetRunVisuals();
        }

        void StartOpening()
        {
            if (flow != Flow.Title && flow != Flow.End) return;
            ResetRun(); flow = Flow.Opening; openingClock = 0f;
            BeginCoverOpening(); SetScreen(); MgfBridge.NotifyChanged();
            MgfSfx.Play("pop", .35f);
        }

        void BeginPractice()
        {
            flow = Flow.Practice;
            current = HanIpmanRules.Practice();
            st.roundId = 0; st.problemId = current.id; st.prompt = current.prompt; st.level = 1;
            st.oaTick = 0; st.qInput = 0; st.repairMode = false; st.dragging = false; firstAttempt = true;
            PrepareProblemVisual(current, true); RefreshProblemUi(); BoostGuide(); SetScreen();
            MgfBridge.NotifyChanged();
        }

        void StartRunDirect()
        {
            ResetRun(); flow = Flow.Playing; NextProblem(); SetScreen(); MgfBridge.NotifyChanged();
        }

        void BeginMainAfterPractice()
        {
            st.score = 0; st.solved = 0; st.lives = HanIpmanRules.StartLives;
            st.firstAttempted = 0; st.firstCorrect = 0; st.firstWrong = 0; st.combo = 0; st.submittedCount = 0;
            st.repairMode = false; st.misconceptionId = ""; timeLeft = HanIpmanRules.SessionSeconds;
            flow = Flow.Playing; NextProblem(); SetScreen(); MgfBridge.NotifyChanged();
        }

        void NextProblem()
        {
            if (st.solved >= HanIpmanRules.TargetSolved)
            {
                if (st.firstCorrect >= HanIpmanRules.MasteryFirstCorrect && st.lives > 0 && timeLeft > 0) EndRun("clear");
                else EndRun("mastery");
                return;
            }
            current = deck[st.solved];
            st.roundId = st.solved + 1; st.problemId = current.id; st.prompt = current.prompt;
            st.level = current.band; st.orderType = current.kind.ToString(); st.qInput = 0;
            st.repairMode = false; st.misconceptionId = ""; st.dragging = false; firstAttempt = true;
            st.oaTick = IndependentOaTick(runSerial, st.roundId, current.qAnswer);
            PrepareProblemVisual(current, false); RefreshProblemUi(); MgfBridge.NotifyChanged();
        }

        static int IndependentOaTick(int serial, int round, int q)
        {
            // q와 같은 값에서 직접 파생하지 않고 별도 정수 혼합을 쓴다.
            uint x = unchecked((uint)serial * 2654435761u) ^ unchecked((uint)round * 2246822519u) ^ 0xA53C9E17u;
            x ^= x >> 15; x *= 2246822519u; x ^= x >> 13;
            return (int)(x % 24u);
        }

        void Submit(int qInput)
        {
            if ((flow != Flow.Practice && flow != Flow.Playing) || current == null) return;
            qInput = Mathf.Clamp(qInput, 0, 23);
            st.qInput = qInput; st.submittedCount++;
            revealCorrect = qInput == current.qAnswer;
            revealPractice = flow == Flow.Practice;
            if (!revealPractice && firstAttempt)
            {
                st.firstAttempted++;
                if (revealCorrect) st.firstCorrect++;
                else st.firstWrong++;
            }
            if (revealCorrect)
            {
                st.misconceptionId = "";
                if (revealPractice) { st.score = 100; st.solved = 1; }
                else
                {
                    if (firstAttempt) st.combo++; else st.combo = 0;
                    st.score += firstAttempt ? 100 + Mathf.Min(3, st.combo) * 15 : 20;
                    st.solved++;
                }
            }
            else
            {
                st.combo = 0;
                st.misconceptionId = HanIpmanRules.MisconceptionId(current, qInput);
                if (!revealPractice) { st.lives--; st.repairMode = true; }
                firstAttempt = false;
            }
            flow = Flow.Reveal; revealClock = 0f; st.dragging = false;
            BeginRevealVisual(revealCorrect, revealPractice, qInput, st.misconceptionId);
            SetScreen(); MgfBridge.NotifyChanged();
        }

        void FinishReveal()
        {
            if (flow != Flow.Reveal) return;
            CompleteRevealVisual(revealCorrect);
            if (revealPractice)
            {
                if (revealCorrect) BeginMainAfterPractice();
                else { flow = Flow.Practice; st.qInput = 0; PrepareProblemVisual(current, true); RefreshProblemUi(); BoostGuide(); }
                return;
            }
            if (!revealCorrect)
            {
                if (st.lives <= 0 || st.firstWrong >= 3) EndRun("napkin");
                else
                {
                    flow = Flow.Playing; st.repairMode = true; st.qInput = 0;
                    PrepareProblemVisual(current, false); RefreshProblemUi(); ShowWrongReason(st.misconceptionId);
                }
            }
            else { flow = Flow.Playing; NextProblem(); }
            SetScreen(); MgfBridge.NotifyChanged();
        }

        void EndRun(string reason)
        {
            if (flow == Flow.End) return;
            endReason = reason; flow = Flow.End; st.dragging = false;
            st.phase = reason == "clear" ? "clear" : "gameover";
            if (reason == "clear")
            {
                int best = PlayerPrefs.GetInt("han-ipman-best", 0);
                if (st.score > best) { PlayerPrefs.SetInt("han-ipman-best", st.score); PlayerPrefs.Save(); }
            }
            ShowEndVisual(reason); SetScreen(); MgfBridge.NotifyChanged();
            MgfSfx.Play(reason == "clear" ? "win" : "lose", .50f);
        }

        void HandleInput()
        {
            if (flow == Flow.Title)
            {
                if (MgfPointer.Down) { st.inputRevision++; RippleAtScreen(MgfPointer.Position, "무대를 엽니다"); StartOpening(); }
                return;
            }
            if (flow == Flow.End)
            {
                if (MgfPointer.Down) { st.inputRevision++; StartOpening(); }
                return;
            }
            if (flow == Flow.Opening || flow == Flow.Reveal)
            {
                if (MgfPointer.Down)
                {
                    st.inputRevision++; RippleAtScreen(MgfPointer.Position, flow == Flow.Opening ? "무대를 여는 중" : "타일을 놓는 중");
                    MgfBridge.NotifyChanged();
                }
                return;
            }
            if (flow != Flow.Practice && flow != Flow.Playing) return;

            if (MgfPointer.Down)
            {
                st.inputRevision++; idleGuide = 0f;
                if (PointerNearCakeCenter(MgfPointer.Position))
                {
                    st.dragging = true; st.qInput = 0; BeginDragVisual(); MgfSfx.Play("tap", .20f);
                }
                else
                {
                    st.dragging = false; RippleAtScreen(MgfPointer.Position, "중심 O에서 시작하세요"); BoostGuide(); MgfSfx.Play("wrong", .10f);
                }
                MgfBridge.NotifyChanged();
            }
            if (st.dragging && MgfPointer.Held)
            {
                int q = QuantizedPointerQ(MgfPointer.Position);
                if (q != st.qInput) { st.qInput = q; UpdateCutPreview(q); st.inputRevision++; MgfBridge.NotifyChanged(); }
            }
            if (st.dragging && MgfPointer.Up)
            {
                st.dragging = false;
                if (PointerBeyondCake(MgfPointer.Position)) Submit(st.qInput);
                else
                {
                    CancelDragVisual(); RippleAtScreen(MgfPointer.Position, "원 밖에서 놓으세요"); BoostGuide(); MgfSfx.Play("wrong", .12f); MgfBridge.NotifyChanged();
                }
            }
        }

        public void TestStart() { StartRunDirect(); }

        public void TestAnswerCorrect()
        {
            if (flow == Flow.Title || flow == Flow.End || flow == Flow.Opening) StartRunDirect();
            if (flow == Flow.Reveal) FinishReveal();
            if (flow == Flow.Practice || flow == Flow.Playing) Submit(current.qAnswer);
        }

        public void TestAnswerWrong()
        {
            if (flow == Flow.Title || flow == Flow.End || flow == Flow.Opening) StartRunDirect();
            if (flow == Flow.Reveal) FinishReveal();
            if (flow == Flow.Practice || flow == Flow.Playing)
            {
                int wrong = current.qAnswer == 23 ? 22 : current.qAnswer + 1;
                Submit(wrong);
            }
        }

        public string StateJson()
        {
            if (flow == Flow.Title) st.phase = "title";
            else if (flow == Flow.End) st.phase = endReason == "clear" ? "clear" : "gameover";
            else st.phase = "playing";
            UpdateLayoutState(st);
            return JsonUtility.ToJson(st);
        }

        public string ProblemBankJson() => MgfJson.Bank(bank);
    }
}
