// 게를 꺼내 — 상태·입력·판정 정본.
// 답 입력은 줄 길이 드래그 → 절단선 스와이프 → 잘린 줄을 점선 구간에 놓는 한 경로뿐이다.
using System;
using System.Collections.Generic;
using Mgf;
using UnityEngine;

namespace Mgf.GereulKkeonae
{
    public sealed class GereulKkeonaeGame : MonoBehaviour, IMgfGame
    {
        public enum Flow { Title, PracticeOne, PracticeTwo, Playing, Reveal, End }
        public enum ToolStep { Pull, Cut, Connect }

        [Serializable]
        public sealed class GameState : MgfState
        {
            public int round;
            public int firstCorrect;
            public int firstAttempts;
            public bool firstAttemptUsed;
            public int drawnLength;
            public int cutLength;
            public string repairKind = "";
            public string problemId = "";
            public string misconceptionId = "";
            public string lastResult = "";
            public string toolStep = "pull";
            public int inputCount;
            public int pointerVersion;
            public int retrims;
            public int discardedCm;
            public bool onboarding;
            public bool frozen;
            public string layout = "portrait";
            // 정답을 노출하지 않는 실입력 감사 좌표(Unity의 좌하단 원점 화면 좌표).
            // B3 검증은 start()로 판만 연 뒤 이 좌표에 실제 pointer 이벤트를 보낸다.
            public int handleX;
            public int handleY;
            public int rulerMin;
            public int rulerMax;
            public int cutX;
            public int cutY;
            public int pieceX;
            public int pieceY;
            public int gapX;
            public int gapY;
        }

        readonly GameState st = new GameState();
        readonly List<NetProblem> bank = new List<NetProblem>();
        List<NetProblem> deck;
        NetProblem current;
        GereulKkeonaeView view;
        Flow flow = Flow.Title;
        ToolStep step = ToolStep.Pull;
        System.Random rng = new System.Random(20261022);
        int runSerial;
        float timeLeft = GereulRules.RunSeconds;
        float revealClock;
        bool revealCorrect;
        bool revealWasFirst;
        bool draggingPull;
        bool pullMoved;
        bool swipingCut;
        bool draggingPiece;
        bool pieceMoved;
        Vector2 pointerDown;
        int pullStartLength;

        public GameState State => st;
        public NetProblem Current => current;
        public Flow CurrentFlow => flow;
        public ToolStep CurrentStep => step;
        public float TimeLeft => timeLeft;

        void Awake()
        {
            MgfLook.Quality(36f);
            Physics.simulationMode = SimulationMode.Script;
            // CreatePrimitive 콜라이더 타입이 WebGL 스트리핑에서 빠지지 않게 정적 참조를 남긴다.
            if (Time.frameCount < 0)
            {
                gameObject.AddComponent<BoxCollider>(); gameObject.AddComponent<SphereCollider>();
                gameObject.AddComponent<CapsuleCollider>(); gameObject.AddComponent<MeshCollider>();
            }
            bank.AddRange(GereulRules.BuildBank());
            Prewarm();
            view = new GereulKkeonaeView(this);
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var text = new System.Text.StringBuilder(12000);
            text.Append("게를꺼내줄을재서조간대다리를펼쳐라도르래를눌러구조시작중학교2학년삼각형의성질최고기록");
            text.Append("줄을맞게잘라게를꺼내시오매듭을끌어길이를정하고절단선을가로질러자른뒤점선구간에거시오");
            text.Append("달빛삼각항로직조도면천막자홍색인장증거선이잠겼습니다대응하는변이포개져구조망이완성됩니다시작점도착점까지");
            text.Append("연습실전안전부표남은시간첫연결정답목표구조완료기준미달다시구조같은두각의맞은편두변은같다");
            text.Append("이등변삼각형의꼭지각이등분선은밑변을이등분한다전체는한쪽의두배다줄이짧습니다길어서처졌습니다");
            text.Append("줄을먼저뽑으시오절단선을길게가로지르시오잘린줄의매듭을점선구간으로끌어놓으시오부표소진시간종료");
            text.Append("∠△ABCD=cm0123456789./·−");
            foreach (var p in bank) text.Append(p.prompt).Append(p.evidence);
            MgfText.Prewarm(text.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(.05f, Time.unscaledDeltaTime);
            HandlePointer();
            if (flow == Flow.Playing)
            {
                timeLeft -= dt;
                if (timeLeft <= 0f) { timeLeft = 0f; EndRun("time"); }
            }
            else if (flow == Flow.Reveal)
            {
                revealClock += dt;
                if (revealClock >= (revealCorrect ? 1.8f : 1.35f)) FinishReveal();
            }
            view.Tick(dt);
        }

        void ResetState()
        {
            st.score = 0; st.lives = GereulRules.StartLives; st.level = 1; st.solved = 0;
            st.round = 0; st.firstCorrect = 0; st.firstAttempts = 0; st.firstAttemptUsed = false;
            st.drawnLength = 3; st.cutLength = 0; st.repairKind = ""; st.problemId = "";
            st.misconceptionId = ""; st.lastResult = ""; st.toolStep = "pull";
            st.retrims = 0; st.discardedCm = 0; st.onboarding = false; st.frozen = false;
            draggingPull = swipingCut = draggingPiece = false; pieceMoved = false;
        }

        void ShowTitle()
        {
            StopAllCoroutines();
            flow = Flow.Title; current = null; deck = null; step = ToolStep.Pull;
            ResetState(); st.phase = "title";
            view.ShowTitle(); Notify();
        }

        void BeginPractice(int which)
        {
            flow = which == 1 ? Flow.PracticeOne : Flow.PracticeTwo;
            st.phase = "playing"; st.onboarding = true; st.frozen = true;
            LoadProblem(which == 1 ? GereulRules.PracticeAngles() : GereulRules.PracticeBisector(), true);
            view.ShowPractice(which); Notify();
        }

        void BeginRun(bool fromTest)
        {
            runSerial++;
            // 자동 검증은 같은 200판을 재현할 수 있어야 한다. 실제 플레이만 시각을 섞는다.
            int seed = unchecked(20261022 ^ (runSerial * 104729) ^ (fromTest ? 0 : Environment.TickCount));
            rng = new System.Random(seed);
            deck = GereulRules.BuildDeck(bank, rng);
            ResetState(); timeLeft = GereulRules.RunSeconds;
            flow = Flow.Playing; st.phase = "playing"; st.onboarding = false; st.frozen = false;
            LoadRound(0);
            view.ShowRun(fromTest); Notify();
        }

        void LoadRound(int index)
        {
            if (index >= GereulRules.RoundCount) { EndRun("complete"); return; }
            st.round = index + 1;
            LoadProblem(deck[index], false);
        }

        void LoadProblem(NetProblem p, bool practice)
        {
            current = p;
            step = ToolStep.Pull;
            st.drawnLength = practice ? 3 : GereulRules.WrongStart(p, rng);
            st.cutLength = 0; st.firstAttemptUsed = false; st.lastResult = ""; st.misconceptionId = "";
            st.problemId = p.id; st.repairKind = p.repairKind; st.level = p.band; st.toolStep = "pull";
            draggingPull = swipingCut = draggingPiece = false; pieceMoved = false;
            view.LoadProblem(p, practice); Notify();
        }

        void SetLength(int cm)
        {
            cm = Mathf.Clamp(cm, GereulRules.MinLength, GereulRules.MaxLength);
            if (cm == st.drawnLength) return;
            if (draggingPull) pullMoved = true;
            st.drawnLength = cm;
            st.pointerVersion++;
            view.SetPulledLength(cm, true);
            Notify();
        }

        void FinishPull()
        {
            if (!pullMoved)
            {
                Refuse("매듭을 눈금 띠를 따라 끌어 길이를 정하시오.", pointerDown, true); return;
            }
            step = ToolStep.Cut; st.toolStep = "cut";
            view.OnLengthSet(st.drawnLength); MgfSfx.Play("tap", .22f); Notify();
        }

        void FinishCut(Vector2 at)
        {
            Vector2 delta = at - pointerDown;
            if (!view.ValidCutSwipe(pointerDown, at) || delta.magnitude < 45f)
            {
                Refuse("절단선을 가로질러 45 px 이상 스와이프하시오.", at, true); return;
            }
            st.cutLength = st.drawnLength;
            step = ToolStep.Connect; st.toolStep = "connect"; st.pointerVersion++;
            view.OnCut(st.cutLength); MgfSfx.Play("whoosh", .4f); Notify();
        }

        void SubmitConnection()
        {
            if (current == null || step != ToolStep.Connect) return;
            revealCorrect = st.cutLength == current.answerCm;
            revealWasFirst = !st.firstAttemptUsed;
            bool practice = flow == Flow.PracticeOne || flow == Flow.PracticeTwo;
            if (!practice && revealWasFirst)
            {
                st.firstAttemptUsed = true; st.firstAttempts++;
                if (revealCorrect) st.firstCorrect++;
            }
            st.lastResult = revealCorrect ? "correct" : "wrong";
            st.misconceptionId = revealCorrect ? "" : GereulRules.Diagnose(current, st.cutLength);
            if (!practice)
            {
                if (revealCorrect)
                {
                    st.solved++; st.score += 180 + 20 * Math.Max(0, st.firstCorrect - 1);
                }
                else st.lives--;
            }
            revealClock = 0f; flow = Flow.Reveal; st.phase = "reveal";
            view.BeginReveal(revealCorrect, revealWasFirst, current, st.cutLength);
            Notify();
        }

        void FinishReveal()
        {
            bool wasPracticeOne = st.onboarding && current != null && current.id == "practice-angle";
            bool wasPracticeTwo = st.onboarding && current != null && current.id == "practice-bisector";
            if (revealCorrect)
            {
                if (wasPracticeOne) { BeginPractice(2); return; }
                if (wasPracticeTwo) { BeginRun(false); return; }
                flow = Flow.Playing; st.phase = "playing"; LoadRound(st.solved); return;
            }
            if (!st.onboarding && st.lives <= 0) { EndRun("buoys"); return; }
            // 긴 줄은 같은 조각을 다시 줄여 자를 수 있고, 짧은 줄은 폐기하고 새 줄을 뽑는다.
            if (st.cutLength > current.answerCm)
            {
                st.retrims++; st.drawnLength = st.cutLength;
            }
            else
            {
                st.discardedCm += st.cutLength;
                st.drawnLength = GereulRules.WrongStart(current, rng);
            }
            st.cutLength = 0; step = ToolStep.Pull; st.toolStep = "pull";
            flow = st.onboarding ? (current.id == "practice-angle" ? Flow.PracticeOne : Flow.PracticeTwo) : Flow.Playing;
            st.phase = "playing"; view.RetryProblem(current, st.drawnLength); Notify();
        }

        void EndRun(string reason)
        {
            if (flow == Flow.End) return;
            bool clear = reason == "complete" && st.solved == GereulRules.RoundCount && st.firstCorrect >= GereulRules.MasteryNeeded;
            flow = Flow.End; st.phase = clear ? "clear" : "gameover"; st.frozen = true;
            draggingPull = swipingCut = draggingPiece = false;
            view.ShowEnd(clear, reason); MgfSfx.Play(clear ? "win" : "lose", .55f); Notify();
        }

        void Refuse(string message, Vector2 at, bool guide)
        {
            st.inputCount++; st.pointerVersion++;
            view.Refuse(message, at, guide); MgfSfx.Play("tap", .13f); Notify();
        }

        void HandlePointer()
        {
            if (MgfPointer.Down)
            {
                pointerDown = MgfPointer.Position; st.inputCount++;
                if (flow == Flow.Title)
                {
                    if (view.HitStart(pointerDown)) { view.PressStart(); BeginPractice(1); MgfSfx.Play("pop", .35f); }
                    else Refuse("화면 가운데 자홍색 매듭 인장을 눌러 시작하시오.", pointerDown, true);
                    return;
                }
                if (flow == Flow.End)
                {
                    if (view.HitRestart(pointerDown)) ShowTitle(); else Refuse("구명 부표를 눌러 다시 구조하시오.", pointerDown, true);
                    return;
                }
                if (flow == Flow.Reveal) { Refuse("그물의 변화를 확인하시오.", pointerDown, false); return; }
                if (step == ToolStep.Pull && (view.HitHandleOrRuler(pointerDown) || st.onboarding))
                {
                    draggingPull = true; pullMoved = false; pullStartLength = st.drawnLength; view.PressHandle(); Notify(); return;
                }
                if (step == ToolStep.Cut && view.HitCutZone(pointerDown))
                {
                    swipingCut = true; view.PressCutter(); Notify(); return;
                }
                if (step == ToolStep.Connect && view.HitPiece(pointerDown))
                {
                    draggingPiece = true; pieceMoved = false; view.PressPiece(pointerDown); Notify(); return;
                }
                Refuse(step == ToolStep.Pull ? "둥근 매듭을 끌어 줄 길이를 정하시오." : step == ToolStep.Cut ? "절단선을 가로질러 스와이프하시오." : "잘린 줄의 매듭을 점선 구간에 거시오.", pointerDown, true);
            }
            if (MgfPointer.Held)
            {
                if (draggingPull && (MgfPointer.Position - pointerDown).sqrMagnitude >= 144f) SetLength(view.LengthFromScreen(MgfPointer.Position));
                else if (swipingCut) view.DragCutter(MgfPointer.Position);
                else if (draggingPiece)
                {
                    if ((MgfPointer.Position - pointerDown).sqrMagnitude >= 2025f) pieceMoved = true;
                    view.DragPiece(MgfPointer.Position);
                }
            }
            if (MgfPointer.Up)
            {
                if (draggingPull) { draggingPull = false; view.ReleaseHandle(); FinishPull(); }
                else if (swipingCut) { swipingCut = false; view.ReleaseCutter(); FinishCut(MgfPointer.Position); }
                else if (draggingPiece)
                {
                    draggingPiece = false; bool hit = view.ReleasePiece(MgfPointer.Position, st.onboarding && pieceMoved);
                    if (hit) SubmitConnection(); else Refuse("줄 조각을 그물의 점선 구간에 놓으시오.", MgfPointer.Position, true);
                }
            }
        }

        void Notify() => MgfBridge.NotifyChanged();

        // QA 훅은 연습을 건너뛰되 실제 연결 판정 SubmitConnection을 그대로 사용한다.
        public void TestStart() => BeginRun(true);
        void EnsureTestPlaying()
        {
            if (flow == Flow.Reveal) FinishReveal();
            if (flow != Flow.Playing) BeginRun(true);
        }
        public void TestAnswerCorrect()
        {
            EnsureTestPlaying(); st.drawnLength = current.answerCm; st.cutLength = current.answerCm;
            step = ToolStep.Connect; st.toolStep = "connect"; view.SetPulledLength(st.drawnLength, false); SubmitConnection();
        }
        public void TestAnswerWrong()
        {
            EnsureTestPlaying(); int wrong = current.answerCm == GereulRules.MaxLength ? current.answerCm - 1 : current.answerCm + 1;
            st.drawnLength = wrong; st.cutLength = wrong; step = ToolStep.Connect; st.toolStep = "connect";
            view.SetPulledLength(st.drawnLength, false); SubmitConnection();
        }
        public string StateJson() => JsonUtility.ToJson(st);
        public string ProblemBankJson() => MgfJson.Bank(GereulRules.ToMgfBank(bank));
    }
}
