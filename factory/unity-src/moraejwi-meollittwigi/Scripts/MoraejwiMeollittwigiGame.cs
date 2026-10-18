using System;
using System.Collections;
using System.Collections.Generic;
using Mgf;
using UnityEngine;

namespace Mgf.MoraejwiMeollittwigi
{
    public sealed class MoraejwiMeollittwigiGame : MonoBehaviour, IMgfGame
    {
        public enum GamePhase { Title, Practice, Playing, Reveal, End }

        [Serializable]
        public sealed class GameState : MgfState
        {
            public bool onboarding;
            public bool frozen;
            public bool dragging;
            public bool lockedAxis;
            public int round;
            public int totalRounds = MoraejwiRules.RoundCount;
            public int firstAttempts;
            public int firstCorrect;
            public int currentBand;
            public int currentS;
            public int currentA;
            public int currentB;
            public int currentStartB;
            public int gridN;
            public int solutionCount;
            public int attemptsOnCurrent;
            public int testActions;
            public int inputSerial;
            public int timeLeft;
            public int[] attemptsById = new int[MoraejwiRules.RoundCount];
            public int[] handlePx = new int[2];
            public int[] originPx = new int[2];
            public int[] gridPx = new int[6];
            public string currentId = "";
            public string lastAction = "boot";
            public string lastFeedback = "";
            public string misconceptionId = "";
            public string layout = "portrait";
            public string source = "none";
        }

        readonly GameState st = new GameState();
        readonly List<LandingProblem> earlyDeck = new List<LandingProblem>(5);
        readonly HashSet<string> used = new HashSet<string>();
        System.Random rng;
        MoraejwiView view;
        LandingProblem current;
        GamePhase phase = GamePhase.Title;
        float timeLeft = MoraejwiRules.RunSeconds;
        bool dragging;
        Vector2 dragRaw;
        Vector2Int dragStart;
        int runSerial;
        int lastSuccessfulB = 5;
        int actualAttemptsCurrent;
        float practiceElapsed;

        public GameState State => st;
        public LandingProblem Current => current;
        public GamePhase Phase => phase;
        public float TimeLeft => timeLeft;
        public bool IsDragging => dragging;

        void Awake()
        {
            MgfLook.Quality(36f);
            // CreatePrimitive가 WebGL 런타임에서 문자열로 붙이는 콜라이더 타입이 엔진
            // 스트리핑으로 사라지지 않게 실제 컴포넌트 참조를 한 번 만든다.
            var colliderTypes = new GameObject("Primitive collider type keeper");
            colliderTypes.AddComponent<SphereCollider>();
            colliderTypes.AddComponent<CapsuleCollider>();
            colliderTypes.AddComponent<BoxCollider>();
            Destroy(colliderTypes);
            MgfText.Prewarm("모래쥐멀리뛰기착지점을끌어라협곡굴길열기중학교학년피타고라스정리빗변위정사각형넓이가되는정하시오가로세로모두이동직각맞은편안전끈첫제출목표보다초과부족연습실전씨앗보관성공기준미달다시도전ACCBm²0123456789+=−<>·/초");
            MoraejwiRules.BuildBank();
            view = new MoraejwiView(this);
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            if (phase == GamePhase.Practice && st.onboarding)
            {
                practiceElapsed += dt;
                // 조작을 못 찾은 학생도 30초 동안 연습에 갇히지 않는다. 유령 경로를
                // 충분히 본 뒤 같은 정수 좌표 판정 경로로 성공 시연하고 실전으로 잇는다.
                if (practiceElapsed >= 16f && !dragging)
                    Submit(3, 4, "guided-demo", false);
            }
            if (phase == GamePhase.Playing && !st.frozen)
            {
                timeLeft = Mathf.Max(0f, timeLeft - dt);
                if (timeLeft <= 0f) EndRun(false, "시간 종료");
            }
            HandlePointer();
            view.Tick(dt);
        }

        void ShowTitle()
        {
            StopAllCoroutines();
            phase = GamePhase.Title;
            current = null;
            dragging = false;
            st.phase = "title";
            st.onboarding = false;
            st.frozen = false;
            st.dragging = false;
            st.lastAction = "title";
            st.lastFeedback = "";
            st.source = "none";
            view.ShowTitle();
            MgfBridge.NotifyChanged();
        }

        void StartPractice()
        {
            StopAllCoroutines();
            phase = GamePhase.Practice;
            current = MoraejwiRules.Practice();
            dragging = false;
            actualAttemptsCurrent = 0;
            practiceElapsed = 0f;
            st.phase = "playing";
            st.onboarding = true;
            st.frozen = true; // 압박과 진행은 정지하지만 연습 드래그 입력은 별도로 허용한다.
            st.dragging = false;
            st.score = 0;
            st.lives = MoraejwiRules.StartLives;
            st.level = 1;
            st.solved = 0;
            st.round = 0;
            st.firstAttempts = 0;
            st.firstCorrect = 0;
            st.lastAction = "practice-ready";
            st.lastFeedback = "착지점을 맞추면 모래쥐가 전진한다";
            st.source = "pointer";
            SyncProblemState();
            view.ShowPractice(current);
            MgfBridge.NotifyChanged();
        }

        void BeginRun(bool fromTest)
        {
            StopAllCoroutines();
            runSerial++;
            rng = new System.Random(unchecked(20261019 + runSerial * 7919));
            earlyDeck.Clear();
            used.Clear();
            earlyDeck.Add(MoraejwiRules.DrawBand(1, rng, used));
            earlyDeck.Add(MoraejwiRules.DrawBand(1, rng, used));
            earlyDeck.Add(MoraejwiRules.DrawBand(2, rng, used));
            earlyDeck.Add(MoraejwiRules.DrawBand(2, rng, used));
            earlyDeck.Add(MoraejwiRules.DrawBand(2, rng, used));
            st.score = 0;
            st.lives = MoraejwiRules.StartLives;
            st.level = 1;
            st.solved = 0;
            st.round = 0;
            st.firstAttempts = 0;
            st.firstCorrect = 0;
            st.testActions = 0;
            st.inputSerial = 0;
            st.onboarding = false;
            st.frozen = false;
            st.dragging = false;
            st.lastAction = fromTest ? "test-start" : "run-start";
            st.lastFeedback = "8번 착지 · 첫 제출 6/8 · 안전끈 3개";
            st.source = fromTest ? "test" : "pointer";
            for (int i = 0; i < st.attemptsById.Length; i++) st.attemptsById[i] = 0;
            timeLeft = MoraejwiRules.RunSeconds;
            st.timeLeft = Mathf.CeilToInt(timeLeft);
            lastSuccessfulB = earlyDeck[4].targetB;
            LoadRound(0);
        }

        void LoadRound(int index)
        {
            phase = GamePhase.Playing;
            st.phase = "playing";
            st.frozen = false;
            st.dragging = false;
            st.onboarding = false;
            st.round = index + 1;
            st.level = index < 2 ? 1 : index < 5 ? 2 : 3;
            current = index < 5 ? earlyDeck[index] : MoraejwiRules.RepairFromCarry(lastSuccessfulB, rng, index + 1);
            actualAttemptsCurrent = 0;
            SyncProblemState();
            view.ShowProblem(current, index, st.lives, st.score);
            MgfBridge.NotifyChanged();
        }

        void SyncProblemState()
        {
            if (current == null) return;
            st.currentId = current.id;
            st.currentBand = current.band;
            st.currentS = current.targetArea;
            st.currentA = current.LockedAxis ? current.targetA : 0;
            st.currentB = current.LockedAxis ? current.startB : 0;
            st.currentStartB = current.startB;
            st.gridN = current.gridN;
            st.lockedAxis = current.LockedAxis;
            st.solutionCount = MoraejwiRules.CountSolutions(current);
            st.attemptsOnCurrent = actualAttemptsCurrent;
            st.timeLeft = Mathf.CeilToInt(timeLeft); // 입력/문제 전환 때만 갱신하여 input.real 오탐을 막는다.
        }

        void HandlePointer()
        {
            if (MgfPointer.Down)
            {
                st.inputSerial++;
                st.source = "pointer";
                if (phase == GamePhase.Title)
                {
                    if (view.TitleStartHit(MgfPointer.Position))
                    {
                        st.lastAction = "title-mouse-press";
                        view.TitlePress();
                        MgfSfx.Play("pop", .25f);
                        StartPractice();
                    }
                    else
                    {
                        st.lastAction = "title-ripple";
                        st.lastFeedback = "모래쥐나 굴길 열기를 눌러 시작하시오";
                        view.RefuseAt(MgfPointer.Position, "모래쥐를 눌러 굴길을 여시오", false);
                        MgfBridge.NotifyChanged();
                    }
                    return;
                }
                if (phase == GamePhase.End)
                {
                    if (view.RestartHit(MgfPointer.Position))
                    {
                        st.lastAction = "restart";
                        MgfSfx.Play("tap", .24f);
                        StartPractice();
                    }
                    else
                    {
                        st.lastAction = "end-ripple";
                        view.RefuseAt(MgfPointer.Position, "아래 천막을 눌러 다시 도전하시오", false);
                        MgfBridge.NotifyChanged();
                    }
                    return;
                }
                bool canPracticeInput = phase == GamePhase.Practice && st.onboarding;
                if ((phase != GamePhase.Playing && !canPracticeInput) || (st.frozen && !canPracticeInput))
                {
                    st.lastAction = "refuse-frozen";
                    view.RefuseAt(MgfPointer.Position, "지금은 착지 장면을 확인하시오", false);
                    MgfBridge.NotifyChanged();
                    return;
                }
                if (view.NearHandle(MgfPointer.Position)) BeginDrag();
                else
                {
                    st.lastAction = "refuse-point-handle";
                    st.lastFeedback = "모래쥐 몸통을 잡아 착지점까지 끄시오";
                    view.RefuseAt(MgfPointer.Position, "모래쥐 몸통을 잡아 끄시오", phase == GamePhase.Practice);
                    MgfBridge.NotifyChanged();
                }
            }

            if (dragging && MgfPointer.Held)
            {
                if (view.ScreenToGrid(MgfPointer.Position, out Vector2 raw))
                {
                    if (current.LockedAxis) raw.x = current.targetA;
                    dragRaw = raw;
                    int a = Mathf.Clamp(Mathf.RoundToInt(raw.x), 0, current.gridN);
                    int b = Mathf.Clamp(Mathf.RoundToInt(raw.y), 0, current.gridN);
                    st.currentA = a;
                    st.currentB = b;
                    view.UpdateDrag(raw, a, b, current.LockedAxis);
                }
            }

            if (dragging && MgfPointer.Up) EndDrag();
        }

        void BeginDrag()
        {
            dragging = true;
            st.dragging = true;
            dragStart = new Vector2Int(st.currentA, st.currentB);
            dragRaw = dragStart;
            st.lastAction = "drag-begin";
            st.lastFeedback = "";
            view.BeginDrag();
            MgfSfx.Play("tap", .17f);
            MgfBridge.NotifyChanged();
        }

        void EndDrag()
        {
            dragging = false;
            st.dragging = false;
            if (!view.GridContainsRaw(dragRaw))
            {
                RejectUnsubmitted("격자 안의 정수 눈금에 놓으시오", "drop-outside");
                return;
            }
            int a = Mathf.Clamp(Mathf.RoundToInt(dragRaw.x), 0, current.gridN);
            int b = Mathf.Clamp(Mathf.RoundToInt(dragRaw.y), 0, current.gridN);
            if (current.LockedAxis) a = current.targetA;
            if (a == 0 || b == 0)
            {
                RejectUnsubmitted("가로·세로 모두 1 m 이상 이동하시오", "drop-axis-zero");
                return;
            }
            if (a == dragStart.x && b == dragStart.y)
            {
                RejectUnsubmitted("모래쥐를 다른 착지점까지 끌어 놓으시오", "drop-no-move");
                return;
            }
            Submit(a, b, "pointer", true);
        }

        void RejectUnsubmitted(string message, string action)
        {
            st.currentA = dragStart.x;
            st.currentB = dragStart.y;
            st.lastAction = action;
            st.lastFeedback = message;
            view.CancelDrag(current, message);
            MgfSfx.Play("wrong", .12f);
            MgfBridge.NotifyChanged();
        }

        void Submit(int a, int b, string source, bool learning)
        {
            if (current == null || phase == GamePhase.End) return;
            bool practice = phase == GamePhase.Practice;
            bool correct = MoraejwiRules.IsCorrect(current, a, b);
            st.source = source;
            st.currentA = a;
            st.currentB = b;
            st.timeLeft = Mathf.CeilToInt(timeLeft);
            if (learning)
            {
                if (!practice)
                {
                    if (actualAttemptsCurrent == 0)
                    {
                        st.firstAttempts++;
                        if (correct) st.firstCorrect++;
                    }
                    actualAttemptsCurrent++;
                    st.attemptsOnCurrent = actualAttemptsCurrent;
                    if (st.round >= 1 && st.round <= st.attemptsById.Length) st.attemptsById[st.round - 1] = actualAttemptsCurrent;
                }
            }
            else st.testActions++;

            st.frozen = true;
            st.dragging = false;
            st.lastFeedback = MoraejwiRules.Evidence(current, a, b);
            st.misconceptionId = correct ? "" : MoraejwiRules.ClassifyMisconception(current, a, b);
            if (correct)
            {
                st.lastAction = practice ? "practice-correct" : "landing-correct";
                if (!practice)
                {
                    st.score += learning && actualAttemptsCurrent == 1 ? 100 : 40;
                    st.solved++;
                    lastSuccessfulB = b;
                }
                phase = GamePhase.Reveal;
                view.BeginReveal(true, practice, a, b, current.targetArea, st.lastFeedback, st.solved, st.score);
                StartCoroutine(FinishCorrect(practice));
            }
            else
            {
                st.lastAction = practice ? "practice-wrong" : "landing-wrong";
                if (!practice) st.lives--;
                view.BeginReveal(false, practice, a, b, current.targetArea, st.lastFeedback, st.solved, st.score);
                if (!practice && st.lives <= 0)
                {
                    st.lives = 0;
                    st.phase = "gameover";
                    phase = GamePhase.End; // 즉시 잠금. 코루틴은 결과 연출만 늦춘다.
                    StartCoroutine(FinishFatalWrong());
                }
                else StartCoroutine(FinishWrong(practice));
            }
            MgfBridge.NotifyChanged();
        }

        IEnumerator FinishCorrect(bool practice)
        {
            yield return new WaitForSecondsRealtime(practice ? 1.75f : 1.48f);
            if (practice)
            {
                BeginRun(false);
            }
            else
            {
                int next = st.solved;
                if (next >= MoraejwiRules.RoundCount)
                    EndRun(st.firstCorrect >= MoraejwiRules.RequiredFirstCorrect && st.lives > 0,
                        st.firstCorrect >= MoraejwiRules.RequiredFirstCorrect ? "씨앗 굴 도착" : "첫 제출 기준 미달");
                else LoadRound(next);
            }
        }

        IEnumerator FinishWrong(bool practice)
        {
            yield return new WaitForSecondsRealtime(practice ? 1.12f : 1.18f);
            phase = practice ? GamePhase.Practice : GamePhase.Playing;
            st.phase = "playing";
            st.frozen = practice; // 연습 압박은 계속 정지. 입력 허용은 HandlePointer에서 별도 처리.
            st.currentA = current.LockedAxis ? current.targetA : 0;
            st.currentB = current.LockedAxis ? current.startB : 0;
            st.lastAction = practice ? "practice-retry" : "repair-ready";
            view.ReturnAfterWrong(current, practice);
            MgfBridge.NotifyChanged();
        }

        IEnumerator FinishFatalWrong()
        {
            yield return new WaitForSecondsRealtime(1.15f);
            EndRun(false, "안전끈 소진");
        }

        void EndRun(bool clear, string reason)
        {
            StopAllCoroutines();
            dragging = false;
            phase = GamePhase.End;
            st.phase = clear ? "clear" : "gameover";
            st.frozen = true;
            st.dragging = false;
            st.onboarding = false;
            st.timeLeft = Mathf.CeilToInt(timeLeft);
            st.lastAction = clear ? "clear" : "gameover";
            st.lastFeedback = reason;
            view.ShowEnd(clear, reason, st.score, st.solved, st.firstCorrect, st.firstAttempts, st.lives);
            MgfSfx.Play(clear ? "win" : "lose", .46f);
            MgfBridge.NotifyChanged();
        }

        public void TestStart()
        {
            BeginRun(true);
        }

        public void TestAnswerCorrect()
        {
            if (phase == GamePhase.Title || phase == GamePhase.End || current == null) BeginRun(true);
            if (current == null) return;
            st.frozen = false;
            Submit(current.targetA, current.targetB, "test", false);
        }

        public void TestAnswerWrong()
        {
            if (phase == GamePhase.Title || phase == GamePhase.End || current == null) BeginRun(true);
            if (current == null) return;
            st.frozen = false;
            int a = current.LockedAxis ? current.targetA : 1;
            int b = current.LockedAxis ? current.targetB == 15 ? 14 : current.targetB + 1 : 1;
            if (MoraejwiRules.IsCorrect(current, a, b)) b = b == current.gridN ? b - 1 : b + 1;
            Submit(a, b, "test", false);
        }

        public string StateJson()
        {
            view.FillScreenProbe(st);
            return JsonUtility.ToJson(st);
        }

        public string ProblemBankJson()
        {
            return MgfJson.Bank(MoraejwiRules.BridgeBank());
        }
    }
}
