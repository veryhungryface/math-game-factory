// 지붕 갈라 — 상태·입력·수학 판정 정본.
// 답 입력은 꼭짓점 A에서 BC 너머까지 실제 pointer 궤적을 긋는 한 경로뿐이다.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.JibungGalla
{
    public sealed class JibungGallaGame : MonoBehaviour, IMgfGame
    {
        enum Flow { Title, Practice, Playing, Reveal, End }

        [Serializable]
        public sealed class GameState : MgfState
        {
            public int round;
            public int firstCorrect;
            public int firstAttemptCount;
            public bool firstAttemptUsed;
            public string currentProblemId = "";
            public int selectedAngle;
            public bool lastAttemptCorrect;
            public string[] attemptHistory = new string[0];
            public string misconceptionId = "";
            public string lastResult = "";
            public int combo;
            public int inputCount;
            public int pointerVersion;
            public int invalidCount;
            public bool onboarding;
            public bool practiceAssistArmed;
            public bool frozen;
            public int roofRotation;
            public int apexX, apexY, bX, bY, cX, cY;
            public int screenW, screenH;
            public bool layoutLand;
            public int hudLeft, hudRight, hudTop, hudBottom;
            public int goalLeft, goalRight, goalTop, goalBottom;
            public int promptLeft, promptRight, promptTop, promptBottom, safeGap;
            public string endReason = "";
        }

        readonly GameState st = new GameState();
        readonly List<RoofProblem> bank = new List<RoofProblem>();
        readonly List<string> history = new List<string>(24);
        List<RoofProblem> deck;
        RoofProblem current;
        JibungView view;
        Flow flow = Flow.Title;
        System.Random rng = new System.Random(20261022);
        int runSerial;
        float timeLeft = JibungRules.RunSeconds;
        float revealClock;
        bool dragging;
        bool revealCorrect;
        bool revealPractice;
        bool failAfterReveal;
        bool invalidPracticeGesture;
        bool practiceAssistArmed;
        bool projectedPracticeDrag;
        Vector2 pointerDown;

        public GameState State => st;
        public float TimeLeft => timeLeft;
        public bool IsPractice => flow == Flow.Practice;

        void Awake()
        {
            MgfLook.Quality(34f);
            Physics.simulationMode = SimulationMode.Script;
            if (Time.frameCount < 0)
            {
                gameObject.AddComponent<BoxCollider>(); gameObject.AddComponent<SphereCollider>();
                gameObject.AddComponent<CapsuleCollider>(); gameObject.AddComponent<MeshCollider>();
            }
            bank.AddRange(JibungRules.BuildBank());
            Prewarm();
            view = new JibungView(this);
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb = new StringBuilder(18000);
            sb.Append("지붕갈라지붕을반으로갈라라중학교2학년삼각형의성질꼭지각밑각밑변이등분선수직이등분합동");
            sb.Append("연습실전지붕첫절단안전벨트남은시간점수꼭지각을반으로갈라지붕선을완성하시오한번에같은두조각수리한이음매가맞았습니다");
            sb.Append("두반각이다릅니다꼭지각전체를그대로쓰지말고반으로나누시오밑각과꼭지각의반각을구분하시오잘못그은선을따라긋지마시오");
            sb.Append("A를누른채점선을따라BC밖에서놓으시오빛나는깔때기안에서시작해직접끌어놓으시오다음지붕을기다리시오열개이등분선이이어졌습니다절단시간이끝났습니다다시가르기");
            sb.Append("∠△ABCD=AE°0123456789·−⊥");
            foreach (var p in bank) sb.Append(p.prompt).Append(p.answer).Append(p.evidence).Append(p.unitConcept);
            MgfText.Prewarm(sb.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(.05f, Time.unscaledDeltaTime);
            HandlePointer();
            if (flow == Flow.Playing)
            {
                timeLeft -= dt;
                if (timeLeft <= 0f) { timeLeft = 0; EndRun("time"); }
            }
            else if (flow == Flow.Reveal)
            {
                revealClock += dt;
                if (revealClock >= (revealCorrect ? 1.85f : 1.55f)) FinishReveal();
            }
            view.Tick(dt);
        }

        void ResetState()
        {
            st.score = 0; st.lives = JibungRules.StartLives; st.level = 1; st.solved = 0; st.round = 0;
            st.firstCorrect = 0; st.firstAttemptCount = 0; st.firstAttemptUsed = false;
            st.currentProblemId = ""; st.selectedAngle = 0; st.lastAttemptCorrect = false;
            st.misconceptionId = ""; st.lastResult = ""; st.combo = 0; st.inputCount = 0; st.pointerVersion = 0; st.invalidCount = 0;
            st.onboarding = false; st.practiceAssistArmed = false; st.frozen = false; st.roofRotation = 0; st.endReason = "";
            history.Clear(); st.attemptHistory = new string[0]; dragging = false; failAfterReveal = false;
            invalidPracticeGesture = false; practiceAssistArmed = false; projectedPracticeDrag = false;
        }

        void ShowTitle()
        {
            flow = Flow.Title; current = null; deck = null; ResetState(); st.phase = "title"; timeLeft = JibungRules.RunSeconds;
            view.ShowTitle(); Notify();
        }

        void BeginPractice()
        {
            ResetState(); flow = Flow.Practice; st.phase = "playing"; st.onboarding = true; st.frozen = true;
            current = JibungRules.Practice(); st.roofRotation = -7; LoadCurrent(true); view.ShowPlay(true, false); view.LoadProblem(current, true, st.roofRotation); Notify();
        }

        void BeginRun(bool fromTest)
        {
            runSerial++;
            int seed = unchecked(20261022 ^ runSerial * 104729 ^ (fromTest ? 0 : Environment.TickCount));
            rng = new System.Random(seed); deck = JibungRules.BuildDeck(rng);
            if (fromTest)
            {
                // 40개 답을 200판에서 각 5회씩 보여 주되 입력 정책과 상관없는 순열로 배치한다.
                int h = JibungRules.MinHalf + ((runSerial * 17 + 11) % 40);
                deck[0] = JibungRules.Make(1, h, 0, "run-r1-h" + h);
            }
            ResetState(); timeLeft = JibungRules.RunSeconds; flow = Flow.Playing; st.phase = "playing"; st.onboarding = false; st.frozen = false;
            view.ShowPlay(false, fromTest); LoadRound(0); Notify();
        }

        void LoadRound(int index)
        {
            if (index >= JibungRules.RoundCount) { EndRun("complete"); return; }
            st.round = index + 1; current = deck[index];
            st.roofRotation = rng.Next(-8, 9); if (st.roofRotation == 0) st.roofRotation = index % 2 == 0 ? -3 : 3;
            LoadCurrent(false); view.LoadProblem(current, false, st.roofRotation); Notify();
        }

        void LoadCurrent(bool practice)
        {
            st.currentProblemId = current.id; st.level = current.band; st.selectedAngle = 0; st.lastResult = ""; st.misconceptionId = "";
            st.firstAttemptUsed = false; st.lastAttemptCorrect = false; dragging = false;
            if (practice) { st.onboarding = true; st.frozen = true; }
        }

        void RetryCurrent()
        {
            st.selectedAngle = 0; st.lastResult = ""; st.misconceptionId = ""; dragging = false;
            flow = st.onboarding ? Flow.Practice : Flow.Playing; st.phase = "playing";
            view.FinishReveal(); view.LoadProblem(current, st.onboarding, st.roofRotation); Notify();
        }

        void Submit(int selected)
        {
            if (current == null || (flow != Flow.Practice && flow != Flow.Playing)) return;
            bool practice = flow == Flow.Practice;
            bool first = !st.firstAttemptUsed;
            bool correct = selected == current.h;
            st.selectedAngle = selected; st.lastAttemptCorrect = correct; st.lastResult = correct ? "correct" : "wrong";
            st.misconceptionId = correct ? "" : JibungRules.Diagnose(current, selected);
            if (!practice && first)
            {
                st.firstAttemptUsed = true; st.firstAttemptCount++; if (correct) st.firstCorrect++;
                history.Add(current.id + "|" + selected + "|" + (correct ? "correct" : st.misconceptionId)); st.attemptHistory = history.ToArray();
            }
            if (!practice)
            {
                if (correct)
                {
                    st.solved++; st.combo++; st.score += 100 + (first ? 50 : 0) + Math.Min(4, st.combo) * 10;
                }
                else { st.lives = Math.Max(0, st.lives - 1); st.combo = 0; }
                int remainingFirstAttempts = JibungRules.RoundCount - st.firstAttemptCount;
                failAfterReveal = !correct && (st.lives <= 0 || st.firstCorrect + remainingFirstAttempts < JibungRules.MasteryNeeded);
            }
            else failAfterReveal = false;
            revealCorrect = correct; revealPractice = practice; revealClock = 0; flow = Flow.Reveal; st.phase = "reveal"; st.frozen = true;
            view.BeginReveal(correct, current, selected, first); Notify();
        }

        void FinishReveal()
        {
            if (flow != Flow.Reveal) return;
            if (revealPractice)
            {
                if (revealCorrect) { view.FinishReveal(); BeginRun(false); }
                else RetryCurrent();
                return;
            }
            if (failAfterReveal) { EndRun(st.lives <= 0 ? "belts" : "mastery"); return; }
            if (revealCorrect)
            {
                view.FinishReveal();
                if (st.solved >= JibungRules.RoundCount) EndRun(st.firstCorrect >= JibungRules.MasteryNeeded ? "complete" : "mastery");
                else { flow = Flow.Playing; st.phase = "playing"; st.frozen = false; LoadRound(st.solved); }
            }
            else RetryCurrent();
        }

        void EndRun(string reason)
        {
            if (flow == Flow.End) return;
            bool clear = reason == "complete" && st.solved == JibungRules.RoundCount && st.firstCorrect >= JibungRules.MasteryNeeded && st.lives > 0;
            flow = Flow.End; st.phase = clear ? "clear" : "gameover"; st.frozen = true; st.endReason = reason; dragging = false;
            view.FinishReveal(); view.ShowEnd(clear, reason); MgfSfx.Play(clear ? "win" : "lose", .55f); Notify();
        }

        void Invalid(string message, Vector2 at, bool pointAtA)
        {
            st.invalidCount++; st.pointerVersion++; view.Refuse(message, at, pointAtA); Notify();
        }

        void HandlePointer()
        {
            if (MgfPointer.Down)
            {
                pointerDown = MgfPointer.Position; st.inputCount++; st.pointerVersion++;
                if (flow == Flow.Title)
                {
                    if (view.HitStart(pointerDown)) { MgfSfx.Play("pop", .35f); BeginPractice(); }
                    else Invalid("지붕 위 ‘지붕 가르기’를 누르시오.", pointerDown, false);
                    return;
                }
                if (flow == Flow.End)
                {
                    if (view.HitRestart(pointerDown)) BeginPractice(); else Invalid("호박색 지붕 표식을 눌러 다시 시작하시오.", pointerDown, false);
                    return;
                }
                if (flow == Flow.Reveal) { Invalid("두 지붕 조각의 각을 확인하시오.", pointerDown, false); return; }
                if (flow != Flow.Practice && flow != Flow.Playing) return;
                bool directApex = view.HitApex(pointerDown);
                bool assistedApex = flow == Flow.Practice && practiceAssistArmed && view.HitPracticeGrabFunnel(pointerDown);
                if (!directApex && !assistedApex)
                {
                    invalidPracticeGesture = flow == Flow.Practice;
                    Invalid(flow == Flow.Practice ? "빛나는 깔때기 안에서 누른 채 BC 밖까지 끌어 놓으시오." : "꼭짓점 A에서 그으시오.", pointerDown, true);
                    return;
                }
                invalidPracticeGesture = false; dragging = true; projectedPracticeDrag = assistedApex; view.BeginDrag(assistedApex); Notify();
            }
            if (dragging && MgfPointer.Held)
            {
                Vector2 effective = MgfPointer.Position;
                int selected;
                bool selectedOk;
                if (projectedPracticeDrag) selectedOk = view.AssistedSelectionFromDrag(pointerDown, MgfPointer.Position, out effective, out selected, out _, out _);
                else selectedOk = view.SelectionFromScreen(MgfPointer.Position, out selected, out _, out _);
                if (selectedOk)
                {
                    if (selected != st.selectedAngle) { st.selectedAngle = selected; st.pointerVersion++; Notify(); }
                    view.DragTo(effective, selected);
                }
            }
            if (dragging && MgfPointer.Up)
            {
                dragging = false; view.EndDrag();
                int selected; bool beyond, inside;
                bool selectedOk;
                if (projectedPracticeDrag) selectedOk = view.AssistedSelectionFromDrag(pointerDown, MgfPointer.Position, out _, out selected, out beyond, out inside);
                else selectedOk = view.SelectionFromScreen(MgfPointer.Position, out selected, out beyond, out inside);
                projectedPracticeDrag = false;
                if (!selectedOk) { Invalid("점선을 따라 더 길게 끌어 놓으시오.", MgfPointer.Position, true); return; }
                if (!inside) { Invalid("삼각형 안쪽으로 선을 그으시오.", MgfPointer.Position, true); return; }
                if (!beyond) { Invalid("밑변 BC 너머까지 그으시오.", MgfPointer.Position, true); return; }
                Submit(selected);
            }
            else if (!dragging && invalidPracticeGesture && MgfPointer.Up)
            {
                // 첫 무효 제스처 뒤에는 연습의 강조 영역에서 시작한 down만 A로 투영한다.
                // 끝점·BC 통과·각도 판정은 여전히 학생의 실제 pointer 좌표를 사용한다.
                invalidPracticeGesture = false; practiceAssistArmed = true; st.practiceAssistArmed = true;
                view.ArmPracticeAssist(); Notify();
            }
        }

        void Notify() => MgfBridge.NotifyChanged();

        public void TestStart() => BeginRun(true);

        void EnsureTestPlaying()
        {
            if (flow == Flow.Reveal) FinishReveal();
            if (flow != Flow.Playing) BeginRun(true);
        }

        public void TestAnswerCorrect()
        {
            EnsureTestPlaying(); Submit(current.h);
        }

        public void TestAnswerWrong()
        {
            EnsureTestPlaying(); int wrong = current.h + 1 < current.apex ? current.h + 1 : current.h - 1; Submit(wrong);
        }

        public string StateJson() => JsonUtility.ToJson(st);
        public string ProblemBankJson() => MgfJson.Bank(JibungRules.ToMgfBank(bank));
    }
}
