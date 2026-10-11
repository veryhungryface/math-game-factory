// 점프 수리 — 상태 머신과 유일한 답 입력(꼭짓점 드래그 후 놓기).
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.JeompeuSuri
{
    public partial class JeompeuSuriGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int firstAttempts;
            public int firstCorrect;
            public int band3FirstCorrect;
            public int combo;
            public int courseIndex;
            public int currentBand;
            public int allowedMask;
            public int draggedVertex = -1;
            public int pointerVersion;
            public int[] vertices = new int[8];
            public int[] handlePx = new int[8]; // 화면 좌상단 원점, A~D
            public int[] gridPx = new int[6]; // (0,0),(1,0),(0,1), 좌상단 원점
            public int screenW;
            public int screenH;
            public bool onboarding;
            public bool frozen;
            public bool masteryPossible;
            public string currentProblem = "";
            public string prompt = "";
            public string targetPredicate = "";
            public string misconceptionId = "";
            public string lastAction = "boot";
        }

        enum Phase { Title, Practice, Playing, Reveal, End }

        readonly State st = new State();
        readonly List<MgfProblem> bank = new List<MgfProblem>();
        readonly Vector2Int[] vertices = new Vector2Int[4];
        List<RepairProblem> deck;
        RepairProblem current;
        System.Random rng;
        Phase phase = Phase.Title;
        int runSerial;
        int courseIndex;
        int dragVertex = -1;
        int keyboardVertex;
        bool dragMoved;
        bool currentFirst;
        bool revealCorrect;
        bool revealPractice;
        string endReason = "";
        Vector2 dragStartScreen;
        Vector2Int dragStartGrid;
        float timeLeft;
        float revealClock;
        float idleClock;
        float hitStop;

        void Awake()
        {
            MgfLook.Quality(30f);
            var colliderTypes = new GameObject("PrimitiveColliderTypes");
            colliderTypes.AddComponent<SphereCollider>(); colliderTypes.AddComponent<BoxCollider>();
            colliderTypes.AddComponent<CapsuleCollider>(); Destroy(colliderTypes);

            List<RepairProblem> generated = JeompeuRules.BuildBank();
            bank.AddRange(JeompeuRules.ToBank(generated));
            BuildWorld();
            BuildUi();
            Prewarm();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb = new StringBuilder("점프수리꼭짓점을끌어맞춰라중학교2학년사각형의성질평행사변형직사각형마름모정사각형두대각선서로다른것을이등분길이같음수직안전끈남은시간첫시도연습실전경기장스프링토끼다시하기코스통과수리연습종료");
            sb.Append("꼭짓점을맞추면토끼가건넌다다른격자점에놓으시오두대각선의중점이겹쳤다서로이등분한다정사각형은직사각형이면서마름모이다");
            for (int i = 0; i < bank.Count; i++) { sb.Append(bank[i].prompt); sb.Append(bank[i].unitConcept); }
            MgfText.Prewarm(sb.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(.05f, Time.deltaTime);
            HandleInput();
            HandleKeyboard();
            if (hitStop > 0f) hitStop -= dt;
            else if (phase == Phase.Practice)
            {
                idleClock += dt;
                if (idleClock >= 8f) { idleClock = 0f; ReplayGuide(true); }
            }
            else if (phase == Phase.Playing)
            {
                timeLeft = Mathf.Max(0f, timeLeft - dt);
                if (timeLeft <= 0f) EndRun("time");
            }
            else if (phase == Phase.Reveal)
            {
                revealClock += dt;
                if (revealClock >= (revealCorrect ? 1.55f : 1.25f)) FinishReveal();
            }
            AnimateWorld(dt);
            UpdateUi(dt);
            MgfLook.FitWidth(cam, 32f, .72f);
        }

        void ShowTitle()
        {
            phase = Phase.Title; current = null; deck = null; courseIndex = 0; dragVertex = -1;
            st.score = 0; st.lives = JeompeuRules.StartLives; st.level = 1; st.solved = 0;
            st.firstAttempts = 0; st.firstCorrect = 0; st.band3FirstCorrect = 0; st.combo = 0;
            st.pointerVersion = 0; st.misconceptionId = ""; st.lastAction = "title";
            endReason = ""; timeLeft = JeompeuRules.CourseSeconds;
            ResetTitleWorld();
            ShowPhaseUi();
            MgfBridge.NotifyChanged();
        }

        void ResetRun()
        {
            runSerial++;
            rng = new System.Random(unchecked(Environment.TickCount ^ runSerial * 104729));
            deck = JeompeuRules.RunDeck(rng);
            courseIndex = 0; dragVertex = -1; keyboardVertex = 0;
            st.score = 0; st.lives = JeompeuRules.StartLives; st.level = 1; st.solved = 0;
            st.firstAttempts = 0; st.firstCorrect = 0; st.band3FirstCorrect = 0; st.combo = 0;
            st.misconceptionId = ""; timeLeft = JeompeuRules.CourseSeconds; endReason = "";
            ResetArenaForRun();
        }

        void StartPractice()
        {
            ResetRun();
            phase = Phase.Practice;
            current = JeompeuRules.Practice();
            currentFirst = true; idleClock = 0f;
            CopyStart();
            BindCurrent();
            ResetBoardVisual(true);
            ShowPhaseUi();
            ReplayGuide(false);
            MgfSfx.Play("whoosh", .28f);
            MgfBridge.NotifyChanged();
        }

        void StartRunDirect()
        {
            ResetRun();
            phase = Phase.Playing;
            LoadProblem();
            ShowPhaseUi();
            MgfBridge.NotifyChanged();
        }

        void BeginRunAfterPractice()
        {
            st.score = 0; st.solved = 0; st.lives = JeompeuRules.StartLives;
            st.firstAttempts = 0; st.firstCorrect = 0; st.band3FirstCorrect = 0; st.combo = 0;
            courseIndex = 0; timeLeft = JeompeuRules.CourseSeconds;
            phase = Phase.Playing;
            ShowToast("실전 시작 · 90초 동안 점프대 10개를 수리하시오", 3f);
            LoadProblem();
            ShowPhaseUi();
        }

        void LoadProblem()
        {
            if (courseIndex >= JeompeuRules.CourseCount)
            {
                EndRun(IsMastery() ? "clear" : "mastery");
                return;
            }
            current = deck[courseIndex];
            currentFirst = true; dragVertex = -1; keyboardVertex = current.answerVertex;
            st.misconceptionId = "";
            CopyStart();
            BindCurrent();
            ResetBoardVisual(false);
            if (courseIndex == 3) ShowToast("직사각형은 서로 이등분하고 대각선 길이도 같다", 3.2f);
            else if (courseIndex == 6) ShowToast("이제 길이 띠 없이 모눈과 대각선을 직접 읽으시오", 3.2f);
            MgfBridge.NotifyChanged();
        }

        void CopyStart()
        {
            for (int i = 0; i < 4; i++) vertices[i] = current.start[i];
        }

        bool IsMastery()
        {
            return st.solved >= JeompeuRules.CourseCount
                && st.firstCorrect >= JeompeuRules.RequiredFirstCorrect
                && st.band3FirstCorrect >= JeompeuRules.RequiredBand3Correct;
        }

        bool MasteryStillPossible()
        {
            int remaining = Mathf.Max(0, JeompeuRules.CourseCount - st.firstAttempts);
            int band3Attempts = Mathf.Clamp(st.firstAttempts - 6, 0, 4);
            int band3Remaining = Mathf.Max(0, 4 - band3Attempts);
            return st.firstCorrect + remaining >= JeompeuRules.RequiredFirstCorrect
                && st.band3FirstCorrect + band3Remaining >= JeompeuRules.RequiredBand3Correct;
        }

        void BindCurrent()
        {
            st.currentProblem = current == null ? "" : current.id;
            st.prompt = current == null ? "" : current.prompt;
            st.targetPredicate = current == null ? "" : JeompeuRules.KindName(current.kind);
            st.currentBand = current == null ? 0 : current.band;
            st.allowedMask = current == null ? 0 : current.allowedMask;
            st.courseIndex = courseIndex;
            for (int i = 0; i < 4; i++) { st.vertices[i * 2] = vertices[i].x; st.vertices[i * 2 + 1] = vertices[i].y; }
            RefreshProblemUi();
        }

        void SubmitRepair()
        {
            if ((phase != Phase.Practice && phase != Phase.Playing) || current == null) return;
            revealPractice = phase == Phase.Practice;
            revealCorrect = JeompeuRules.Matches(vertices, current.kind);
            st.lastAction = revealCorrect ? "repair-correct" : "repair-wrong";

            if (revealPractice)
            {
                if (revealCorrect) { st.score = 100; st.solved = 1; st.misconceptionId = ""; }
                else st.misconceptionId = JeompeuRules.Misconception(vertices, current.kind);
            }
            else
            {
                if (currentFirst)
                {
                    st.firstAttempts++;
                    if (revealCorrect)
                    {
                        st.firstCorrect++;
                        if (current.band == 3) st.band3FirstCorrect++;
                    }
                    currentFirst = false;
                }
                if (revealCorrect)
                {
                    st.combo++; st.solved++;
                    st.score += 100 + Mathf.Min(60, st.combo * 10) + Mathf.RoundToInt(timeLeft * .3f);
                    st.misconceptionId = "";
                }
                else
                {
                    st.combo = 0; st.lives = Mathf.Max(0, st.lives - 1);
                    st.misconceptionId = JeompeuRules.Misconception(vertices, current.kind);
                }
            }
            phase = Phase.Reveal; revealClock = 0f; dragVertex = -1; hitStop = revealCorrect ? .08f : 0f;
            BindCurrent();
            BeginRevealVisual(revealCorrect, JeompeuRules.MissingReason(vertices, current.kind));
            ShowPhaseUi();
            MgfBridge.NotifyChanged();
        }

        void FinishReveal()
        {
            if (phase != Phase.Reveal) return;
            CompleteRevealVisual(revealCorrect);
            if (revealPractice)
            {
                if (revealCorrect) BeginRunAfterPractice();
                else
                {
                    phase = Phase.Practice; CopyStart(); BindCurrent(); ResetBoardVisual(true);
                    ShowPhaseUi(); ReplayGuide(true); MgfBridge.NotifyChanged();
                }
                return;
            }
            if (!revealCorrect)
            {
                if (st.lives <= 0) { EndRun("straps"); return; }
                phase = Phase.Playing;
                ShowToast(JeompeuRules.MissingReason(vertices, current.kind) + " · 같은 점프대를 다시 수리하시오", 3.2f);
                ShowPhaseUi(); MgfBridge.NotifyChanged();
                return;
            }
            courseIndex++;
            phase = Phase.Playing;
            LoadProblem();
            ShowPhaseUi();
        }

        void EndRun(string reason)
        {
            if (phase == Phase.End) return;
            phase = Phase.End; dragVertex = -1; endReason = reason;
            st.lastAction = "end-" + reason;
            ShowEnd(reason);
            ShowPhaseUi();
            MgfSfx.Play(reason == "clear" ? "win" : "lose", .52f);
            MgfBridge.NotifyChanged();
        }

        void HandleInput()
        {
            if (MgfPointer.Down)
            {
                st.pointerVersion++; st.lastAction = "pointer";
                SpawnTapRipple(MgfPointer.Position);
                MgfBridge.NotifyChanged();
                if (phase == Phase.Title) { BeginTitlePress(); MgfSfx.Play("tap", .22f); return; }
                if (phase == Phase.End) { PunchRestart(); return; }
                if (phase == Phase.Reveal) { RefuseInput("토끼가 착지할 때까지 잠깐 기다리시오"); return; }
                if (phase != Phase.Practice && phase != Phase.Playing) return;
                idleClock = 0f;
                int picked = NearestHandle(MgfPointer.Position, out float distance);
                float wide = Mathf.Max(66f, Screen.width * .18f);
                if (picked < 0 || distance > wide)
                {
                    RefuseInput(phase == Phase.Practice ? "맥동하는 D를 끌어 유령 꼭짓점에 놓으시오" : "살구색 꼭짓점을 끌어 다른 격자점에 놓으시오");
                    ReplayGuide(true); return;
                }
                if ((current.allowedMask & (1 << picked)) == 0)
                {
                    RefuseInput("연습에서는 D 꼭짓점만 움직이시오"); PulseHandle(picked); ReplayGuide(true); return;
                }
                dragVertex = picked; keyboardVertex = picked; dragStartScreen = MgfPointer.Position;
                dragStartGrid = vertices[picked]; dragMoved = false; st.draggedVertex = picked;
                BeginVertexDrag(picked); MgfSfx.Play("tap", .18f);
            }

            if (dragVertex >= 0 && MgfPointer.Held)
            {
                dragMoved |= Vector2.Distance(MgfPointer.Position, dragStartScreen) > 10f;
                if (PointerGrid(MgfPointer.Position, out Vector2 raw)) UpdateVertexDrag(dragVertex, raw);
            }

            if (MgfPointer.Up)
            {
                if (phase == Phase.Title) { EndTitlePress(); StartPractice(); return; }
                if (phase == Phase.End) { StartPractice(); return; }
                if (dragVertex < 0) return;
                int movedVertex = dragVertex; dragVertex = -1; st.draggedVertex = -1;
                Vector2Int point = NearestGrid(MgfPointer.Position);
                EndVertexDrag(movedVertex);
                if (!dragMoved || point == dragStartGrid)
                {
                    SetVertexVisual(movedVertex, vertices[movedVertex]);
                    RefuseInput("다른 격자점에 놓으시오"); ReplayGuide(true); return;
                }
                vertices[movedVertex] = point;
                SetVertexVisual(movedVertex, point);
                BindCurrent();
                SubmitRepair();
            }
        }

        void HandleKeyboard()
        {
            if (phase != Phase.Practice && phase != Phase.Playing) return;
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                for (int k = 1; k <= 4; k++)
                {
                    int n = (keyboardVertex + k) & 3;
                    if ((current.allowedMask & (1 << n)) != 0) { keyboardVertex = n; break; }
                }
                PulseHandle(keyboardVertex); st.pointerVersion++; st.lastAction = "keyboard-select"; MgfBridge.NotifyChanged();
            }
            Vector2Int d = Vector2Int.zero;
            if (Input.GetKeyDown(KeyCode.LeftArrow)) d = Vector2Int.left;
            else if (Input.GetKeyDown(KeyCode.RightArrow)) d = Vector2Int.right;
            else if (Input.GetKeyDown(KeyCode.UpArrow)) d = Vector2Int.up;
            else if (Input.GetKeyDown(KeyCode.DownArrow)) d = Vector2Int.down;
            if (d != Vector2Int.zero && (current.allowedMask & (1 << keyboardVertex)) != 0)
            {
                Vector2Int p = vertices[keyboardVertex] + d;
                if (JeompeuRules.InGrid(p))
                {
                    vertices[keyboardVertex] = p; SetVertexVisual(keyboardVertex, p); BindCurrent();
                    st.lastAction = "keyboard-move"; MgfSfx.Play("tap", .12f); MgfBridge.NotifyChanged();
                }
                else RefuseInput("모눈 안쪽에서 꼭짓점을 움직이시오");
            }
            if (Input.GetKeyDown(KeyCode.Space) && (current.allowedMask & (1 << keyboardVertex)) != 0) SubmitRepair();
        }

        void FinishPendingReveal()
        {
            if (phase == Phase.Reveal) { revealClock = 99f; FinishReveal(); }
        }

        public void TestStart() { FinishPendingReveal(); StartRunDirect(); }

        public void TestAnswerCorrect()
        {
            FinishPendingReveal();
            if (phase != Phase.Playing || current == null) StartRunDirect();
            vertices[current.answerVertex] = current.AnswerPoint;
            SetVertexVisual(current.answerVertex, current.AnswerPoint);
            BindCurrent(); SubmitRepair();
        }

        public void TestAnswerWrong()
        {
            FinishPendingReveal();
            if (phase != Phase.Playing || current == null) StartRunDirect();
            int v = current.answerVertex;
            for (int y = 0; y < 7; y++) for (int x = 0; x < 7; x++)
            {
                vertices[v] = new Vector2Int(x, y);
                if (!JeompeuRules.Matches(vertices, current.kind)) { SetVertexVisual(v, vertices[v]); BindCurrent(); SubmitRepair(); return; }
            }
        }

        public string StateJson()
        {
            st.phase = phase == Phase.Title ? "title" : phase == Phase.End ? (endReason == "clear" ? "clear" : "gameover") : "playing";
            st.onboarding = phase == Phase.Practice;
            st.frozen = phase == Phase.Practice || phase == Phase.Reveal || phase == Phase.End;
            st.masteryPossible = MasteryStillPossible();
            st.draggedVertex = dragVertex;
            BindCurrent(); FillScreenProbe();
            return JsonUtility.ToJson(st);
        }

        public string ProblemBankJson() => MgfJson.Bank(bank);
    }
}
