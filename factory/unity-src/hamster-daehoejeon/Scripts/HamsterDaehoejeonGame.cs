// 햄스터 대회전 — 상태·실입력·수학 판정 정본.
// 유일한 답 입력은 60칸 회전 손잡이를 실제 pointer로 끌어 놓는 경로다.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.HamsterDaehoejeon
{
    public sealed class HamsterDaehoejeonGame : MonoBehaviour, IMgfGame
    {
        enum Flow { Title, Practice, Playing, Reveal, End }
        enum Training { None, First, Bridge }

        [Serializable]
        public sealed class GameState : MgfState
        {
            public int round;
            public int band;
            public int selectedAngle;
            public int firstAttempts;
            public int firstCorrect;
            public int firstAccuracy;
            public bool firstAttemptUsed;
            public int combo;
            public int wheelCells;
            public bool dragActive;
            public bool precision;
            public bool onboarding;
            public bool bridgePractice;
            public bool frozen;
            public string currentProblemId = "";
            public string lastResult = "";
            public string misconceptionId = "";
            public string[] attemptHistory = new string[0];
            public int inputCount;
            public int pointerVersion;
            public int invalidCount;
            public int allowedMin;
            public int allowedMax;
            public int allowedStep;
            public int dialCenterX;
            public int dialCenterY;
            public int dialRadius;
            public int handleX;
            public int handleY;
            public int screenW;
            public int screenH;
            public bool layoutLand;
            public string endReason = "";
        }

        readonly GameState st = new GameState();
        readonly List<AngleProblem> bank = new List<AngleProblem>();
        readonly List<string> history = new List<string>(24);
        List<AngleProblem> deck;
        AngleProblem current;
        HamsterView view;
        Flow flow = Flow.Title;
        Training training = Training.None;
        System.Random rng = new System.Random(20261022);
        int runSerial;
        float remaining = HamsterRules.RunSeconds;
        float revealClock;
        bool revealCorrect;
        bool failAfterReveal;
        bool dragging;
        bool hasDragSample;
        bool bridgeDone;
        int lastSelected;
        bool lastValidArc;
        bool lastPrecision;
        Vector2 dragStart;
        float dragDistance;

        public GameState State => st;
        public float Remaining => remaining;
        public AngleProblem Current => current;
        public bool IsTraining => training != Training.None;

        void Awake()
        {
            MgfLook.Quality(30f);
            Physics.simulationMode = SimulationMode.Script;
            // CreatePrimitive가 런타임에 붙이는 collider 타입을 IL2CPP 스트리핑에서 보존한다.
            // 실행 불가능한 분기지만 정적 타입 참조는 WebGL 링크 단계에 남는다.
            if (Time.frameCount < 0)
            {
                gameObject.AddComponent<BoxCollider>(); gameObject.AddComponent<SphereCollider>();
                gameObject.AddComponent<CapsuleCollider>(); gameObject.AddComponent<MeshCollider>();
            }
            bank.AddRange(HamsterRules.BuildBank());
            Prewarm();
            view = new HamsterView(this);
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb = new StringBuilder(26000);
            sb.Append("햄스터대회전레일을돌려달려라중학교2학년삼각형의성질빈각을맞추면햄스터가달린다");
            sb.Append("연습실전필름수리첫제출브레이크남은시간정밀눈금기본눈금모식도같은두변꼭지각밑각밑변이등분선교점");
            sb.Append("손잡이를누른채원호를따라끌고놓으시오고정중두밑각은같다내각의합은180도황동클립이잠겼습니다");
            sb.Append("두밑각의합을한각으로쓰지마시오밑각하나만빼지마시오꼭지각전체와반각을구분하시오레일을다시조절하시오");
            sb.Append("여덟프레임이이어졌습니다첫제출여섯칸을완성했습니다브레이크가모두풀렸습니다시간이끝났습니다다시돌리기");
            sb.Append("∠△ABCDPQRSEFGHJKLMNUVWXYZT=°×÷−⊥·0123456789");
            foreach (var p in bank) sb.Append(p.prompt).Append(p.answerText).Append(p.evidence).Append(p.unitConcept);
            MgfText.Prewarm(sb.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(.05f, Time.unscaledDeltaTime);
            HandlePointer();
            if (flow == Flow.Playing)
            {
                remaining -= dt;
                if (remaining <= 0f) { remaining = 0f; EndRun("time"); }
            }
            else if (flow == Flow.Reveal)
            {
                revealClock += dt;
                if (revealClock >= (revealCorrect ? 1.35f : 1.15f)) FinishReveal();
            }
            view.Tick(dt);
        }

        void ResetState()
        {
            st.score = 0; st.lives = HamsterRules.StartLives; st.level = 1; st.phase = "title"; st.solved = 0;
            st.round = 0; st.band = 1; st.selectedAngle = 0; st.firstAttempts = 0; st.firstCorrect = 0; st.firstAccuracy = 0;
            st.firstAttemptUsed = false; st.combo = 0; st.wheelCells = 0; st.dragActive = false; st.precision = false;
            st.onboarding = false; st.bridgePractice = false; st.frozen = false; st.currentProblemId = ""; st.lastResult = ""; st.misconceptionId = "";
            st.inputCount = 0; st.pointerVersion = 0; st.invalidCount = 0; st.allowedMin = 0; st.allowedMax = 0; st.allowedStep = 0;
            st.endReason = ""; history.Clear(); st.attemptHistory = new string[0];
            dragging = false; hasDragSample = false; bridgeDone = false; training = Training.None; failAfterReveal = false;
        }

        void ShowTitle()
        {
            ResetState(); flow = Flow.Title; training = Training.None; current = null; deck = null; remaining = HamsterRules.RunSeconds;
            st.phase = "title"; view.ShowTitle(); Notify();
        }

        void BeginFirstPractice()
        {
            ResetState(); flow = Flow.Practice; training = Training.First; st.phase = "playing"; st.onboarding = true; st.frozen = true;
            current = HamsterRules.FirstPractice(); LoadCurrent(); view.ShowPlay(true, false, false); view.LoadProblem(current, true, false); Notify();
        }

        void BeginBridgePractice()
        {
            flow = Flow.Practice; training = Training.Bridge; st.phase = "playing"; st.onboarding = false; st.bridgePractice = true; st.frozen = true;
            current = HamsterRules.BridgePractice(); LoadCurrent(); view.ShowPlay(false, true, false); view.LoadProblem(current, true, true); Notify();
        }

        void BeginRun(bool fromTest)
        {
            runSerial++;
            int seed = unchecked(20261022 ^ runSerial * 104729 ^ (fromTest ? 0 : Environment.TickCount));
            rng = new System.Random(seed);
            deck = HamsterRules.BuildDeck(rng);
            if (fromTest)
            {
                // 200회 실측에서 60답을 거의 균등하게 노출한다. 입력 정책과 답은 독립이다.
                int k = (runSerial * 17 + 7) % 60;
                deck[0] = HamsterRules.Make(1, k, runSerial % 6, "test-r1-k" + k + "-m" + (runSerial % 6));
            }
            ResetState(); training = Training.None; flow = Flow.Playing; st.phase = "playing"; st.frozen = false;
            remaining = HamsterRules.RunSeconds; view.ShowPlay(false, false, fromTest); LoadRound(0); Notify();
        }

        void LoadRound(int index)
        {
            if (index >= HamsterRules.RoundCount) { EndRun("complete"); return; }
            st.round = index + 1; current = deck[index]; LoadCurrent(); view.LoadProblem(current, false, false); Notify();
        }

        void LoadCurrent()
        {
            st.currentProblemId = current.id; st.band = current.band; st.level = current.band;
            st.allowedMin = current.allowedMin; st.allowedMax = current.allowedMax; st.allowedStep = current.allowedStep;
            int startIndex = (st.round * 19 + runSerial * 7 + current.band * 5) % 60;
            st.selectedAngle = current.allowedMin + current.allowedStep * startIndex;
            st.firstAttemptUsed = false; st.lastResult = ""; st.misconceptionId = ""; st.dragActive = false; st.precision = false;
            dragging = false; hasDragSample = false; dragDistance = 0f;
        }

        void RetryCurrent()
        {
            st.lastResult = ""; st.misconceptionId = ""; st.dragActive = false; st.precision = false; st.frozen = training != Training.None;
            flow = training == Training.None ? Flow.Playing : Flow.Practice; st.phase = "playing";
            view.FinishReveal(); view.LoadProblem(current, training != Training.None, training == Training.Bridge); Notify();
        }

        void Submit(int selected)
        {
            if (current == null || (flow != Flow.Playing && flow != Flow.Practice)) return;
            bool isTraining = training != Training.None;
            bool first = !st.firstAttemptUsed;
            bool correct = selected == current.answer;
            st.selectedAngle = selected; st.lastResult = correct ? "correct" : "wrong";
            st.misconceptionId = correct ? "" : HamsterRules.Diagnose(current, selected);
            if (!isTraining && first)
            {
                st.firstAttemptUsed = true; st.firstAttempts++; if (correct) st.firstCorrect++;
                st.firstAccuracy = st.firstAttempts == 0 ? 0 : st.firstCorrect * 100 / st.firstAttempts;
                history.Add(current.id + "|" + selected + "|" + (correct ? "correct" : st.misconceptionId)); st.attemptHistory = history.ToArray();
            }
            if (!isTraining)
            {
                if (correct)
                {
                    st.solved++; st.wheelCells = st.solved; st.combo++; st.score += 100 + (first ? 60 : 0) + Math.Min(4, st.combo) * 15;
                }
                else
                {
                    st.lives = Math.Max(0, st.lives - 1); st.combo = 0;
                }
                int remainingFirst = HamsterRules.RoundCount - st.firstAttempts;
                failAfterReveal = !correct && (st.lives <= 0 || st.firstCorrect + remainingFirst < HamsterRules.MasteryNeeded);
            }
            else failAfterReveal = false;

            revealCorrect = correct; revealClock = 0f; flow = Flow.Reveal; st.phase = "reveal"; st.frozen = true; st.dragActive = false;
            view.BeginReveal(correct, current, selected, first, training); Notify();
        }

        void FinishReveal()
        {
            if (flow != Flow.Reveal) return;
            if (training == Training.First)
            {
                if (revealCorrect) { view.FinishReveal(); BeginRun(false); }
                else RetryCurrent();
                return;
            }
            if (training == Training.Bridge)
            {
                if (revealCorrect)
                {
                    training = Training.None; bridgeDone = true; st.bridgePractice = false; st.frozen = false; view.FinishReveal();
                    flow = Flow.Playing; st.phase = "playing"; LoadRound(5);
                }
                else RetryCurrent();
                return;
            }
            if (failAfterReveal) { EndRun(st.lives <= 0 ? "brakes" : "mastery"); return; }
            if (!revealCorrect) { RetryCurrent(); return; }

            view.FinishReveal();
            if (st.solved >= HamsterRules.RoundCount)
            {
                EndRun(st.firstCorrect >= HamsterRules.MasteryNeeded ? "complete" : "mastery");
                return;
            }
            if (st.solved == 5 && !bridgeDone) { BeginBridgePractice(); return; }
            flow = Flow.Playing; st.phase = "playing"; st.frozen = false; LoadRound(st.solved);
        }

        void EndRun(string reason)
        {
            if (flow == Flow.End) return;
            bool clear = reason == "complete" && st.solved == HamsterRules.RoundCount && st.firstCorrect >= HamsterRules.MasteryNeeded && st.lives > 0;
            flow = Flow.End; training = Training.None; st.phase = clear ? "clear" : "gameover"; st.frozen = true; st.endReason = reason;
            st.dragActive = false; dragging = false; view.FinishReveal(); view.ShowEnd(clear, reason);
            int best = PlayerPrefs.GetInt("hamster-daehoejeon-best", 0); if (st.firstCorrect > best) PlayerPrefs.SetInt("hamster-daehoejeon-best", st.firstCorrect);
            MgfSfx.Play(clear ? "win" : "lose", .55f); Notify();
        }

        void HandlePointer()
        {
            if (MgfPointer.Down)
            {
                Vector2 p = MgfPointer.Position; st.inputCount++; st.pointerVersion++;
                if (flow == Flow.Title)
                {
                    if (view.HitTitleStart(p)) { MgfSfx.Play("pop", .38f); view.SplitCurtain(); BeginFirstPractice(); }
                    else Invalid("검은 손잡이를 눌러 시작하시오.", p, false);
                    return;
                }
                if (flow == Flow.End)
                {
                    if (view.HitRestart(p)) BeginFirstPractice(); else Invalid("가운데 손잡이를 눌러 다시 시작하시오.", p, false);
                    return;
                }
                if (flow == Flow.Reveal) { Invalid("황동 클립이 고정되는 동안 각을 확인하시오.", p, false); return; }
                if (flow != Flow.Playing && flow != Flow.Practice) return;
                bool hit = view.HitHandle(p) || (training != Training.None && view.HitTrainingFunnel(p));
                if (!hit)
                {
                    Invalid("둥근 손잡이를 누른 채 원호를 따라 움직이시오.", p, true); return;
                }
                dragging = true; hasDragSample = false; dragStart = p; dragDistance = 0f; st.dragActive = true;
                view.BeginDrag(); MgfSfx.Play("tap", .20f); Notify();
            }
            if (dragging && MgfPointer.Held)
            {
                Vector2 p = MgfPointer.Position; dragDistance = Mathf.Max(dragDistance, Vector2.Distance(dragStart, p));
                if (view.SelectionFromScreen(p, out int value, out bool validArc, out bool precision))
                {
                    hasDragSample = true; lastSelected = value; lastValidArc = validArc; lastPrecision = precision;
                    if (st.selectedAngle != value || st.precision != precision)
                    {
                        st.selectedAngle = value; st.precision = precision; st.pointerVersion++; Notify();
                    }
                    view.DragTo(value, validArc, precision);
                }
            }
            if (dragging && MgfPointer.Up)
            {
                dragging = false; st.dragActive = false; view.EndDrag();
                if (!hasDragSample || dragDistance < 6f) { Invalid("원호를 따라 움직인 뒤 놓으시오.", MgfPointer.Position, true); return; }
                if (!lastValidArc) { Invalid("눈금 원호 안에서 놓으시오.", MgfPointer.Position, true); return; }
                st.precision = lastPrecision; Submit(lastSelected);
            }
        }

        void Invalid(string message, Vector2 p, bool pointAtHandle)
        {
            st.invalidCount++; st.pointerVersion++; view.Refuse(message, p, pointAtHandle); MgfSfx.Play("wrong", .12f); Notify();
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
            EnsureTestPlaying(); Submit(current.answer);
        }

        public void TestAnswerWrong()
        {
            EnsureTestPlaying();
            int wrong = current.misconceptionValues != null && current.misconceptionValues.Length > 0
                ? current.misconceptionValues[0]
                : current.allowedMin + current.allowedStep * ((current.answer - current.allowedMin + current.allowedStep) / current.allowedStep % 60);
            Submit(wrong);
        }

        public string StateJson() => JsonUtility.ToJson(st);
        public string ProblemBankJson() => MgfJson.Bank(HamsterRules.ToMgfBank(bank));
    }
}
