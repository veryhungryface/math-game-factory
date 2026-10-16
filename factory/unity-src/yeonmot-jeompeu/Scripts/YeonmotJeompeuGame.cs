// 연못 점프 — 타이틀, 고정 연습, 실제 개구리 드래그·도약, 숙련/실패 상태.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.YeonmotJeompeu
{
    public partial class YeonmotJeompeuGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int firstAttempted;
            public int firstCorrect;
            public int combo;
            public int pointerVersion;
            public int inputVersion;
            public int encounterId;
            public int sourceActiveVertex;
            public int selectedVertex;
            public int submissionCount;
            public string currentProblemId = "";
            public string misconceptionId = "";
            public string endReason = "";
            // 화면에 표시되는 수학 데이터의 정수 사본. 정답 슬롯은 넣지 않으며 실제 포인터 검증기가 독립 계산한다.
            public string problemKind = "";
            public int[] sourceSides = new int[3];
            public int[] sourceAngles = new int[3];
            public int[] candidateSides = new int[6];
            public int[] candidateAngles = new int[6];
            public int[] positionToVertex = new int[6];
            // 검증용 화면 좌표. 모든 꼭짓점을 공개하되 정답 인덱스는 공개하지 않는다.
            public int[] sourcePx = new int[6];
            public int[] targetPx = new int[12];
            public bool onboarding;
            public bool frozen;
        }

        enum GamePhase { Title, Practice, Playing, Jumping, Reveal, End }

        readonly State st = new State();
        readonly List<MgfProblem> bank = new List<MgfProblem>();
        List<PondProblem> deck;
        PondProblem current;
        GamePhase gamePhase = GamePhase.Title;
        int deckIndex;
        int runSerial;
        bool firstAttempt;
        bool dragging;
        bool dragMoved;
        int selectedSlot = -1;
        bool landingCorrect;
        bool landingWasPractice;
        float timeLeft = YeonmotRules.RunSeconds;
        float phaseClock;
        float idleClock;
        float hitStop;
        bool windowFocused = true;
        Vector2 dragStart;
        Vector3 jumpFrom;
        Vector3 jumpTo;

        void Awake()
        {
            MgfLook.Quality(34f);
            int auditErrors = YeonmotRules.AuditBank();
            if (auditErrors != 0) Debug.LogError("[연못 점프] 문제 은행 전수 검산 실패: " + auditErrors);
            bank.AddRange(YeonmotRules.BuildBank());
            BuildWorld();
            BuildUi();
            Prewarm();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb = new StringBuilder("연못점프대응점으로뛰어라개구리를잡아시작중학교2학년도형의닮음최고기록");
            sb.Append("닮은잎에서대응점을찾으시오개략도실제크기와다름수치로판단SSS닮음SAS닮음AA닮음");
            sb.Append("대응변닮음비끼인각대응각남은시간도약첫시도기회연꽃봉오리꼭짓점에놓으시오");
            sb.Append("세대응변의비가모두같다두대응변의비와그사이각이같다두대응각이각각같다");
            sb.Append("같은수를더한것이아니라비를확인하시오한각만같아서는부족하다위치가아니라대응정보를보시오");
            sb.Append("연꽃둑도착다시건너기숙련기준첫시도6회이상기회소진시간종료이번판은아직숙련전직접끌어착지합니다");
            for (int i = 0; i < bank.Count; i++) { sb.Append(bank[i].prompt); sb.Append(bank[i].answer); }
            MgfText.Prewarm(sb.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(.05f, Time.deltaTime);
            HandleInput();
            if (hitStop > 0f) hitStop -= dt;
            else if (gamePhase == GamePhase.Practice)
            {
                // 연습은 학생의 실제 down→move→up 착지만 통과시킨다.
                // 무입력 시간이 길어져도 정답을 대신 실행하지 않고 안내만 반복한다.
                idleClock += dt;
                if (idleClock >= 8f) { idleClock = 0f; ReplayGuide(true); }
            }
            else if (gamePhase == GamePhase.Playing)
            {
                if (windowFocused) timeLeft = Mathf.Max(0f, timeLeft - dt);
                if (timeLeft <= 0f) EndRun("time");
                idleClock += dt;
                if (idleClock >= 9f) { idleClock = 0f; NudgeFrog(); }
            }
            else if (gamePhase == GamePhase.Jumping)
            {
                phaseClock += dt;
                UpdateJump(Mathf.Clamp01(phaseClock / .62f));
                if (phaseClock >= .62f) ResolveLanding(selectedSlot);
            }
            else if (gamePhase == GamePhase.Reveal)
            {
                phaseClock += dt;
                if (phaseClock >= (landingCorrect ? 1.45f : 1.25f)) FinishReveal();
            }
            AnimateWorld(dt);
            UpdateUi(dt);
            ApplyResponsiveLayout();
        }

        void OnApplicationFocus(bool hasFocus) { windowFocused = hasFocus; }

        void ShowTitle()
        {
            gamePhase = GamePhase.Title; current = null; deck = null; deckIndex = 0;
            st.score = 0; st.lives = YeonmotRules.StartLives; st.level = 1; st.solved = 0;
            st.firstAttempted = 0; st.firstCorrect = 0; st.combo = 0; st.pointerVersion = 0; st.inputVersion = 0;
            st.encounterId = 0; st.sourceActiveVertex = 1; st.selectedVertex = -1; st.submissionCount = 0;
            st.currentProblemId = ""; st.misconceptionId = ""; st.endReason = "";
            st.onboarding = false; st.frozen = false;
            timeLeft = YeonmotRules.RunSeconds; dragging = false; selectedSlot = -1;
            ResetWorldForRun(); ShowTitleVisual(); SetScreen(); MgfBridge.NotifyChanged();
        }

        void ResetRunState()
        {
            runSerial++;
            deck = YeonmotRules.RunDeck(unchecked(Environment.TickCount ^ runSerial * 104729));
            deckIndex = 0; timeLeft = YeonmotRules.RunSeconds;
            st.score = 0; st.lives = YeonmotRules.StartLives; st.level = 1; st.solved = 0;
            st.firstAttempted = 0; st.firstCorrect = 0; st.combo = 0; st.encounterId = 0;
            st.sourceActiveVertex = 1; st.selectedVertex = -1; st.submissionCount = 0;
            st.currentProblemId = ""; st.misconceptionId = ""; st.endReason = "";
            st.onboarding = false; st.frozen = false; selectedSlot = -1; dragging = false;
            ResetWorldForRun();
        }

        void StartPractice()
        {
            ResetRunState();
            current = YeonmotRules.Practice();
            gamePhase = GamePhase.Practice; firstAttempt = true; st.onboarding = true; st.frozen = true;
            BindCurrent(); ResetForProblem(); SetScreen(); ReplayGuide(false);
            MgfSfx.Play("whoosh", .32f); MgfBridge.NotifyChanged();
        }

        void StartRunDirect()
        {
            ResetRunState();
            gamePhase = GamePhase.Playing; st.onboarding = false; st.frozen = false;
            LoadNextProblem(); SetScreen(); MgfBridge.NotifyChanged();
        }

        void BeginMainAfterPractice()
        {
            st.score = 0; st.solved = 0; st.firstAttempted = 0; st.firstCorrect = 0;
            st.combo = 0; st.lives = YeonmotRules.StartLives; st.submissionCount = 0;
            st.misconceptionId = ""; st.onboarding = false; st.frozen = false;
            gamePhase = GamePhase.Playing; LoadNextProblem(); SetScreen();
        }

        void LoadNextProblem()
        {
            if (st.solved >= YeonmotRules.TargetHops)
            {
                EndRun(st.firstCorrect >= YeonmotRules.MasteryFirstCorrect ? "clear" : "not_mastered");
                return;
            }
            current = deck[deckIndex++ % deck.Count]; firstAttempt = true; selectedSlot = -1;
            st.level = current.band; st.misconceptionId = ""; st.selectedVertex = -1;
            st.encounterId++; BindCurrent(); ResetForProblem();
            if (st.solved == 3) ShowBandTransition("SAS 닮음 · 두 변의 비와 끼인각");
            else if (st.solved == 6) ShowBandTransition("AA 닮음 · 두 대응각");
            MgfBridge.NotifyChanged();
        }

        void BindCurrent()
        {
            st.currentProblemId = current == null ? "" : current.id;
            st.sourceActiveVertex = current == null ? 1 : current.sourceActiveVertex;
            if (current == null) return;
            st.problemKind = current.kind == PondKind.Sss ? "SSS" : current.kind == PondKind.Sas ? "SAS" : "AA";
            for (int i = 0; i < 3; i++)
            {
                st.sourceSides[i] = current.sourceSides[i]; st.sourceAngles[i] = current.sourceAngles[i];
                for (int leaf = 0; leaf < 2; leaf++)
                {
                    st.candidateSides[leaf * 3 + i] = current.candidates[leaf].sides[i];
                    st.candidateAngles[leaf * 3 + i] = current.candidates[leaf].angles[i];
                    st.positionToVertex[leaf * 3 + i] = current.candidates[leaf].posToVertex[i];
                }
            }
        }

        void ResetForProblem()
        {
            dragging = false; dragMoved = false; idleClock = 0f; phaseClock = 0f;
            st.selectedVertex = -1;
            SetFrogHome(); ResetLeavesForProblem(current); RefreshProblemUi();
        }

        void BeginJump(int slot, bool instant)
        {
            if (current == null || slot < 0 || slot >= 6 || gamePhase == GamePhase.End) return;
            selectedSlot = slot; st.selectedVertex = slot; st.inputVersion++; st.submissionCount++;
            landingWasPractice = gamePhase == GamePhase.Practice;
            jumpFrom = frogRoot.position; jumpTo = VertexLanding(slot);
            dragging = false; gamePhase = GamePhase.Jumping; phaseClock = 0f;
            BeginJumpVisual(slot); MgfSfx.Play("whoosh", .38f); MgfBridge.NotifyChanged();
            if (instant) { UpdateJump(1f); ResolveLanding(slot); }
        }

        void ResolveLanding(int slot)
        {
            if (gamePhase != GamePhase.Jumping || current == null) return;
            landingCorrect = YeonmotRules.IsCorrect(current, slot);
            bool wasFirst = firstAttempt;
            if (landingWasPractice)
            {
                if (!landingCorrect) { firstAttempt = false; st.misconceptionId = YeonmotRules.MisconceptionId(current, slot); }
            }
            else
            {
                if (firstAttempt)
                {
                    st.firstAttempted++;
                    if (landingCorrect) st.firstCorrect++;
                }
                if (landingCorrect)
                {
                    st.combo++; st.score += firstAttempt ? 100 : 20; st.solved++;
                }
                else
                {
                    st.combo = 0; st.lives--; firstAttempt = false;
                    st.misconceptionId = YeonmotRules.MisconceptionId(current, slot);
                }
            }
            gamePhase = GamePhase.Reveal; phaseClock = 0f; hitStop = landingCorrect ? .085f : .055f;
            BeginRevealVisual(landingCorrect, wasFirst, current, slot);
            SetScreen(); MgfBridge.NotifyChanged();
        }

        void FinishReveal()
        {
            if (gamePhase != GamePhase.Reveal) return;
            CompleteRevealVisual(landingCorrect, selectedSlot);
            if (landingWasPractice)
            {
                if (landingCorrect) BeginMainAfterPractice();
                else { gamePhase = GamePhase.Practice; ResetForProblem(); ShowWrongReason(st.misconceptionId); ReplayGuide(true); }
            }
            else if (!landingCorrect)
            {
                if (st.lives <= 0) EndRun("lives");
                else { gamePhase = GamePhase.Playing; ResetForProblem(); ShowWrongReason(st.misconceptionId); }
            }
            else
            {
                AdvancePond(selectedSlot);
                gamePhase = GamePhase.Playing; LoadNextProblem();
            }
            SetScreen(); MgfBridge.NotifyChanged();
        }

        void EndRun(string reason)
        {
            if (gamePhase == GamePhase.End) return;
            gamePhase = GamePhase.End; dragging = false; st.endReason = reason;
            int best = PlayerPrefs.GetInt("yeonmot-jeompeu.best", 0);
            if (st.score > best) { PlayerPrefs.SetInt("yeonmot-jeompeu.best", st.score); PlayerPrefs.Save(); }
            ShowEnd(reason); SetScreen(); MgfSfx.Play(reason == "clear" ? "win" : "lose", .56f); MgfBridge.NotifyChanged();
        }

        void HandleInput()
        {
            if (MgfPointer.Down)
            {
                // 결과 화면은 명시된 재시작 표식만 받는다. 그 밖의 입력은 종료 상태를 바꾸지 않는다.
                if (gamePhase == GamePhase.End)
                {
                    if (RectTransformUtility.RectangleContainsScreenPoint(restartRt, MgfPointer.Position, null)) StartRunDirect();
                    return;
                }
                st.pointerVersion++; idleClock = 0f; dragStart = MgfPointer.Position;
                SpawnPointerRipple(MgfPointer.Position); MgfBridge.NotifyChanged();
                if (gamePhase == GamePhase.Title) { PressTitleFrog(); StartPractice(); return; }
                if (gamePhase == GamePhase.Jumping || gamePhase == GamePhase.Reveal)
                {
                    Refuse("착지가 끝날 때까지 기다리시오"); return;
                }
                if (gamePhase != GamePhase.Practice && gamePhase != GamePhase.Playing) return;
                if (IsFrogGrab(MgfPointer.Position))
                {
                    dragging = true; dragMoved = false; BeginFrogGrab(); MgfBridge.NotifyChanged();
                }
                else
                {
                    int slot = VertexAtScreen(MgfPointer.Position);
                    if (slot >= 0) InspectVertex(slot); else RefuseWater(MgfPointer.Position);
                }
            }
            if (MgfPointer.Held && dragging && (gamePhase == GamePhase.Practice || gamePhase == GamePhase.Playing))
            {
                dragMoved |= Vector2.Distance(dragStart, MgfPointer.Position) >= 12f;
                UpdateFrogDrag(MgfPointer.Position); st.inputVersion++;
            }
            if (MgfPointer.Up && dragging && (gamePhase == GamePhase.Practice || gamePhase == GamePhase.Playing))
            {
                dragging = false; EndFrogDrag();
                int slot = VertexAtScreen(MgfPointer.Position);
                if (!dragMoved) { Refuse("개구리를 누른 채 꼭짓점까지 끌어 놓으시오"); ReplayGuide(true); SetFrogHome(); }
                else if (slot < 0) { RefuseWater(MgfPointer.Position); SetFrogHome(); }
                else BeginJump(slot, false);
            }
        }

        public void TestStart()
        {
            FlushAnimation(); StartRunDirect();
        }

        public void TestAnswerCorrect()
        {
            FlushAnimation();
            if (gamePhase != GamePhase.Playing || current == null) StartRunDirect();
            BeginJump(current.correctSlot, true);
        }

        public void TestAnswerWrong()
        {
            FlushAnimation();
            if (gamePhase != GamePhase.Playing || current == null) StartRunDirect();
            int wrong = (current.correctSlot + 1) % 6;
            BeginJump(wrong, true);
        }

        void FlushAnimation()
        {
            if (gamePhase == GamePhase.Jumping) { UpdateJump(1f); ResolveLanding(selectedSlot); }
            if (gamePhase == GamePhase.Reveal) { phaseClock = 99f; FinishReveal(); }
        }

        public string StateJson()
        {
            st.phase = gamePhase == GamePhase.Title ? "title" : gamePhase == GamePhase.End ?
                (st.endReason == "clear" ? "clear" : st.endReason == "not_mastered" ? "not_mastered" : "gameover") : "playing";
            st.onboarding = gamePhase == GamePhase.Practice; st.frozen = gamePhase == GamePhase.Practice;
            st.level = current == null ? 1 : Math.Max(1, current.band); st.lives = Math.Max(0, st.lives);
            return JsonUtility.ToJson(st);
        }

        public string ProblemBankJson() => MgfJson.Bank(bank);
    }
}
