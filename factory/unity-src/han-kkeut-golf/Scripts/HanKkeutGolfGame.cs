// 한 끗 골프 — 타이틀 → 무료 연습 → 8코스(표시된 OA · OA 직접 긋기 · 잘못된 보조선 OB 고치기) → 결과.
// 답 입력은 오직 「A에 고정된 레일의 손잡이를 끌어 돌리고 손을 떼는 것」(또는 같은 레일과 연동된 반원 눈금을 끄는 것)이다.
// 판정은 HanKkeutRules.IsCorrect 의 정수 각도 비교 하나뿐이고, 공 주행 애니메이션은 그 결과를 재생한다.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.HanKkeutGolf
{
    public partial class HanKkeutGolfGame : MonoBehaviour, IMgfGame
    {
        // IL2CPP 스트리핑 뒤에도 코드 프리미티브 콜라이더 타입을 보존한다.
        static readonly Type KeepSphere = typeof(SphereCollider);
        static readonly Type KeepCapsule = typeof(CapsuleCollider);
        static readonly Type KeepBox = typeof(BoxCollider);

        [Serializable]
        public sealed class GameState : MgfState
        {
            public string flow = "title";
            public string roundId = "";
            public int course;            // 1..8 (연습 0)
            public int band;
            public int theta;             // 화면에 표시된 OA 의 각
            public int railAngle;
            public int firstAttempts;
            public int firstCorrect;
            public int firstMisses;
            public bool needRadius;
            public bool radiusDrawn;
            public int submissionSerial;
            public string lastResult = "";
            public string misconceptionId = "";
            public bool repairMode;
            public bool onboarding;
            public bool bridge;
            public int streak;
            public int inputRevision;
            public string lastInputKind = "none";
            public bool layoutLand;
            // 정지한 평면도에서만 갱신되는 화면 좌표(px, 왼쪽 위 원점) — 실제 pointer 봇이 손잡이·O·A 를 찾는 데 쓴다.
            public int aPx, aPy, oPx, oPy, bPx = -1, bPy = -1, handlePx, handlePy, railPx;
            public int dialCx = -1, dialCy = -1, dialR;
        }

        enum Flow { Title, Opening, Transition, Draw, Aim, Rolling, End }

        readonly GameState st = new GameState();
        readonly List<MgfProblem> bank = new List<MgfProblem>(540);
        List<GolfProblem> deck;
        int[] deckStarts;
        GolfProblem current;
        Flow flow = Flow.Title;
        int courseIndex;          // 0 = 연습, 1..8
        bool practice;
        bool practicedOnce;
        bool firstAttempt;
        bool lastCorrect;
        float timeLeft;
        float idle;
        float endClock;
        float flowClock;
        int runSerial;
        string endReason = "";
        readonly int[] mistakeTally = new int[4];

        // 포인터
        enum Grab { None, Rail, Dial, Draw }
        Grab grab;
        Vector2 downPos;
        float dragTravel;
        int angleAtDown;
        bool drawFromO;
        int invalidPracticeInputs;

        void Awake()
        {
            GC.KeepAlive(KeepSphere); GC.KeepAlive(KeepCapsule); GC.KeepAlive(KeepBox);
            MgfLook.Quality(60f);
            runSerial = (int)((DateTime.Now.Ticks / 10000L) % 700000L) * 3 + 11;
            var pool = HanKkeutRules.BuildProblems();
            for (int i = 0; i < pool.Count; i++) bank.Add(pool[i].ToMgf());
            Prewarm();
            BuildWorld();
            BuildUi();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb = new StringBuilder(4000);
            sb.Append("한끗골프레일돌려공을굴려라접선반지름수직원점기준선오른쪽반직선각도오차이내허용코스첫제출통과남은시간초연속")
              .Append("OAB°+−=⊥0123456789·/:%()…!?,. ")
              .Append(HanKkeutRules.Prompt(3, 127, 7, 210));
            sb.Append(UiStrings.All);
            MgfText.Prewarm(sb.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(.05f, Time.unscaledDeltaTime);
            HandlePointer();
            HandleKeyboard();
            flowClock += dt;
            switch (flow)
            {
                case Flow.Opening:
                    if (flowClock >= .45f) { if (practicedOnce) BeginRun(); else BeginPractice(); }
                    break;
                case Flow.Transition:
                    if (CameraSettled() && flowClock >= .3f) EnterInput();
                    break;
                case Flow.Draw:
                case Flow.Aim:
                    if (!practice && !st.bridge)
                    {
                        timeLeft -= dt;
                        if (timeLeft <= 0f) { timeLeft = 0f; EndRun("time"); break; }
                    }
                    idle += dt;
                    if (idle >= 8f) { idle = 0f; BoostGuide(); }
                    break;
                case Flow.Rolling:
                    if (RollFinished()) AfterRoll();
                    break;
                case Flow.End:
                    endClock += dt;
                    break;
            }
            TickView(dt);
        }

        // ───────────────────────── 흐름 ─────────────────────────
        void ResetRun()
        {
            st.score = 0; st.lives = HanKkeutRules.StartBalls; st.level = 1; st.solved = 0;
            st.firstAttempts = 0; st.firstCorrect = 0; st.firstMisses = 0; st.streak = 0;
            st.submissionSerial = 0; st.lastResult = ""; st.misconceptionId = ""; st.repairMode = false;
            st.course = 0; st.band = 1; st.roundId = ""; st.needRadius = false; st.radiusDrawn = false;
            timeLeft = HanKkeutRules.SessionSeconds; endReason = ""; idle = 0f;
            for (int i = 0; i < mistakeTally.Length; i++) mistakeTally[i] = 0;
        }

        void SetFlow(Flow f)
        {
            flow = f; flowClock = 0f;
            st.flow = f.ToString().ToLowerInvariant();
            st.phase = f == Flow.Title ? "title" : f == Flow.End ? st.phase : "playing";
        }

        void ShowTitle()
        {
            ResetRun(); current = null; deck = null; practice = false; st.onboarding = false; st.bridge = false;
            SetFlow(Flow.Title); st.phase = "title";
            ShowTitleVisual(); Notify();
        }

        void StartOpening()
        {
            ResetRun(); SetFlow(Flow.Opening); st.phase = "playing";
            BeginOpeningVisual(); MgfSfx.Play("whoosh", .35f); Notify();
        }

        void BeginPractice()
        {
            practice = true; st.onboarding = true; st.bridge = false;
            courseIndex = 0; st.course = 0; st.level = 1; st.band = 1;
            LoadCourse(HanKkeutRules.Practice(), HanKkeutRules.PracticeStart);
        }

        void BeginRun()
        {
            runSerial++;
            ResetRun();
            deck = HanKkeutRules.BuildDeck(runSerial, out deckStarts);
            practice = false; st.onboarding = false;
            ResetCoursesVisual();
            LoadRound(1);
        }

        void LoadRound(int course)
        {
            if (course > HanKkeutRules.Courses) { EndRun("clear"); return; }
            courseIndex = course; st.course = course;
            var p = deck[course - 1];
            st.band = p.band; st.level = p.band;
            // 단계 첫 진입(3코스·6코스)은 시계를 멈춘 무압박 연결 활동 — 반지름을 실제로 그을 때까지 기다린다.
            st.bridge = (course == 3 || course == 6);
            LoadCourse(p, deckStarts[course - 1]);
        }

        void LoadCourse(GolfProblem p, int startAngle)
        {
            current = p; firstAttempt = true;
            st.roundId = p.id; st.theta = p.theta; st.railAngle = startAngle;
            st.needRadius = p.band >= 2; st.radiusDrawn = !st.needRadius;
            st.lastResult = ""; st.misconceptionId = ""; st.repairMode = false;
            grab = Grab.None; idle = 0f; invalidPracticeInputs = 0;
            LoadCourseVisual(p, courseIndex, practice, startAngle);
            SetFlow(Flow.Transition); Notify();
        }

        void EnterInput()
        {
            SetFlow(st.radiusDrawn ? Flow.Aim : Flow.Draw);
            EnterInputVisual();
            CaptureScreenAnchors();
            BoostGuide(); Notify();
        }

        void CompleteRadius(string kind)
        {
            if (flow != Flow.Draw) return;
            st.radiusDrawn = true; st.lastInputKind = kind; st.inputRevision++;
            if (st.bridge) st.bridge = false; // 연결 활동 끝 → 시계 재개
            RadiusDrawnVisual();
            MgfSfx.Play("pop", .45f);
            SetFlow(Flow.Aim); UpdateBanner(); CaptureScreenAnchors(); BoostGuide(); Notify();
        }

        void Submit(int angle)
        {
            if (flow != Flow.Aim || current == null) return;
            angle = HanKkeutRules.Norm180(angle);
            st.railAngle = angle;
            st.submissionSerial++;
            bool ok = HanKkeutRules.IsCorrect(current, angle);
            lastCorrect = ok;
            string mis = ok ? "" : HanKkeutRules.ClassifyMisconception(current, angle);
            bool counted = !practice && firstAttempt;
            if (counted)
            {
                st.firstAttempts++;
                if (ok) { st.firstCorrect++; st.streak++; }
                else { st.firstMisses++; st.streak = 0; Tally(mis); }
            }
            st.lastResult = ok ? "correct" : "wrong";
            st.misconceptionId = mis;
            if (ok)
            {
                if (!practice)
                {
                    st.solved++;
                    st.score += firstAttempt ? 100 + 20 * Mathf.Min(4, Mathf.Max(0, st.streak - 1)) : 30;
                }
            }
            else if (!practice)
            {
                st.lives = Mathf.Max(0, st.lives - 1);
                st.repairMode = true;
            }
            bool wasFirst = firstAttempt;
            firstAttempt = false;
            grab = Grab.None;
            BeginRollVisual(ok, angle, wasFirst && !practice, mis);
            SetFlow(Flow.Rolling);
            if (ok) MgfSfx.Play("whoosh", .22f);
            // 숙달 목표가 불가능해지는 순간 입력을 잠근다(공 0 · 두 번째 첫 제출 실패).
            if (!practice && !ok && (st.lives <= 0 || st.firstMisses > HanKkeutRules.MaxFirstMisses))
            {
                pendingEnd = st.lives <= 0 ? "balls" : "mastery";
                st.phase = "gameover"; // 입력은 이 순간 잠긴다(주행 연출만 끝까지 재생)
            }
            Notify();
        }

        string pendingEnd = "";

        void AfterRoll()
        {
            if (pendingEnd != "") { var r = pendingEnd; pendingEnd = ""; EndRun(r); return; }
            if (lastCorrect)
            {
                if (practice)
                {
                    practicedOnce = true; practice = false; st.onboarding = false;
                    BeginRun();
                    return;
                }
                CourseDoneVisual(st.repairMode);
                if (courseIndex >= HanKkeutRules.Courses) EndRun("clear");
                else LoadRound(courseIndex + 1);
            }
            else
            {
                // 같은 코스를 직접 수리한다. 첫 제출 기록은 바뀌지 않는다.
                st.lastResult = "";
                SetFlow(Flow.Aim); RepairVisual(); CaptureScreenAnchors(); BoostGuide(); Notify();
            }
        }

        void EndRun(string reason)
        {
            if (flow == Flow.End) return;
            bool clear = reason == "clear" && st.solved == HanKkeutRules.Courses && st.firstCorrect >= HanKkeutRules.MasteryNeeded;
            if (clear) st.score += Mathf.CeilToInt(timeLeft) * 5 + st.lives * 50;
            endReason = clear ? "clear" : reason;
            grab = Grab.None; st.lastInputKind = "locked"; st.bridge = false;
            st.phase = clear ? "clear" : "gameover";
            SetFlow(Flow.End); st.phase = clear ? "clear" : "gameover";
            endClock = 0f;
            int best = PlayerPrefs.GetInt("han-kkeut-golf-best-first", 0);
            bool newBest = st.firstCorrect > best;
            if (newBest) PlayerPrefs.SetInt("han-kkeut-golf-best-first", st.firstCorrect);
            PlayerPrefs.SetString("han-kkeut-golf-review", ReviewGoal());
            PlayerPrefs.Save();
            ShowEndVisual(clear, endReason, newBest);
            MgfSfx.Play(clear ? "win" : "lose", .5f);
            Notify();
        }

        void Tally(string mis)
        {
            if (mis == HanKkeutRules.MisParallel) mistakeTally[0]++;
            else if (mis == HanKkeutRules.MisMirror) mistakeTally[1]++;
            else if (mis == HanKkeutRules.MisOtherRadius) mistakeTally[2]++;
            else mistakeTally[3]++;
        }

        /// <summary>다음 판 복습 목표 하나(이번 판 첫 제출 실패 유형에서).</summary>
        string ReviewGoal()
        {
            int best = -1, n = 0;
            for (int i = 0; i < mistakeTally.Length; i++) if (mistakeTally[i] > n) { n = mistakeTally[i]; best = i; }
            switch (best)
            {
                case 0: return "복습: 레일은 반지름 OA와 평행이 아니라 수직";
                case 1: return "복습: 기준선 반대쪽 같은 각이 아니라 OA의 각+90°";
                case 2: return "복습: 보조선 OB가 아니라 접점의 반지름 OA 먼저 잇기";
                case 3: return "복습: O→A를 먼저 보고 OA의 각+90° 방향으로";
                default: return "";
            }
        }

        // ───────────────────────── 입력 ─────────────────────────
        void HandlePointer()
        {
            if (MgfPointer.Down)
            {
                downPos = MgfPointer.Position; dragTravel = 0f; idle = 0f; st.inputRevision++;
                switch (flow)
                {
                    case Flow.Title:
                        st.lastInputKind = "title_ball"; Ripple(downPos, ""); StartOpening(); return;
                    case Flow.End:
                        if (endClock < .8f) { st.lastInputKind = "busy"; Ripple(downPos, UiStrings.Busy); Notify(); return; }
                        st.lastInputKind = "restart"; Ripple(downPos, ""); StartOpening(); return;
                    case Flow.Opening:
                    case Flow.Transition:
                        st.lastInputKind = "busy"; Ripple(downPos, UiStrings.Moving); Notify(); return;
                    case Flow.Rolling:
                        st.lastInputKind = "busy"; RefuseHandle(UiStrings.WaitBall); Notify(); return;
                    case Flow.Draw:
                        DrawDown(); return;
                    case Flow.Aim:
                        AimDown(); return;
                }
            }

            if (grab != Grab.None && MgfPointer.Held)
            {
                dragTravel = Mathf.Max(dragTravel, Vector2.Distance(MgfPointer.Position, downPos));
                if (grab == Grab.Rail || grab == Grab.Dial)
                {
                    int a = grab == Grab.Rail ? AngleFromRailPointer(MgfPointer.Position) : AngleFromDial(MgfPointer.Position);
                    if (dragTravel >= 4f && a != st.railAngle)
                    {
                        st.railAngle = a; st.inputRevision++; SetRailVisual(a, true); Notify();
                    }
                }
                else if (grab == Grab.Draw) DrawDragVisual(MgfPointer.Position, drawFromO);
            }

            if (MgfPointer.Up && grab != Grab.None)
            {
                var g = grab; grab = Grab.None;
                if (g == Grab.Draw) { DrawUp(); return; }
                ReleaseRailVisual();
                if (dragTravel >= 4f)
                {
                    st.lastInputKind = g == Grab.Dial ? "dial_release" : "rail_release";
                    Submit(st.railAngle);
                }
                else
                {
                    // 손잡이 탭: 비용 없이 경로 시범(판정 없음)
                    st.lastInputKind = "handle_tap";
                    PathDemoVisual(); Toast(practice ? NextPracticeHint() : UiStrings.DragArc); Notify();
                }
            }
        }

        void AimDown()
        {
            var p = MgfPointer.Position;
            if (DialHit(p))
            {
                grab = Grab.Dial; angleAtDown = st.railAngle; st.lastInputKind = "dial_drag";
                GrabRailVisual(); MgfSfx.Play("tap", .18f); Notify(); return;
            }
            if (RailHit(p))
            {
                grab = Grab.Rail; angleAtDown = st.railAngle; st.lastInputKind = "rail_drag";
                GrabRailVisual(); MgfSfx.Play("tap", .18f); Notify(); return;
            }
            st.lastInputKind = "refused";
            Ripple(p, practice ? NextPracticeHint() : UiStrings.GrabHandle); PointAtHandle(); MgfSfx.Play("pop", .12f); Notify();
        }

        string NextPracticeHint()
        {
            invalidPracticeInputs++;
            if (invalidPracticeInputs == 1) return UiStrings.PracticeStep1;
            if (invalidPracticeInputs == 2) return UiStrings.PracticeStep2;
            return UiStrings.PracticeStep3;
        }

        void DrawDown()
        {
            var p = MgfPointer.Position;
            float tol = HitPx();
            if (Vector2.Distance(p, ScreenOf(PointO())) <= tol) { grab = Grab.Draw; drawFromO = true; }
            else if (Vector2.Distance(p, ScreenOf(PointA())) <= tol) { grab = Grab.Draw; drawFromO = false; }
            if (grab == Grab.Draw)
            {
                st.lastInputKind = "radius_drag"; BeginDrawVisual(drawFromO); MgfSfx.Play("tap", .18f); Notify(); return;
            }
            st.lastInputKind = "refused";
            if (RailHit(p)) { RefuseHandle(UiStrings.RadiusFirst); MgfSfx.Play("pop", .14f); }
            else { Ripple(p, UiStrings.StartAtO); PointAtO(); MgfSfx.Play("pop", .12f); }
            Notify();
        }

        void DrawUp()
        {
            var p = MgfPointer.Position;
            float tol = HitPx();
            var target = drawFromO ? PointA() : PointO();
            if (Vector2.Distance(p, ScreenOf(target)) <= tol && dragTravel >= 8f)
            {
                CompleteRadius("radius_release");
                return;
            }
            CancelDrawVisual();
            bool nearB = current != null && current.band == 3 && Vector2.Distance(p, ScreenOf(PointB())) <= tol;
            st.lastInputKind = nearB ? "radius_to_b" : "radius_miss";
            Toast(nearB ? UiStrings.NotB : (drawFromO ? UiStrings.EndAtA : UiStrings.EndAtO));
            MgfSfx.Play("pop", .12f);
            Notify();
        }

        void HandleKeyboard()
        {
            if (flow != Flow.Aim) return;
            int a = st.railAngle;
            if (Input.GetKeyDown(KeyCode.LeftArrow)) a = HanKkeutRules.Norm180(a + 1);
            if (Input.GetKeyDown(KeyCode.RightArrow)) a = HanKkeutRules.Norm180(a - 1);
            if (a != st.railAngle) { st.railAngle = a; st.inputRevision++; st.lastInputKind = "keyboard"; SetRailVisual(a, true); Notify(); }
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) { st.lastInputKind = "keyboard_release"; Submit(st.railAngle); }
        }

        void Notify() => MgfBridge.NotifyChanged();

        // ───────────────────────── 훅 ─────────────────────────
        public void TestStart()
        {
            practicedOnce = true;
            BeginRun();
            FinishTransitionNow();
        }

        void EnsureTestInput()
        {
            if (flow == Flow.Rolling) { FinishRollNow(); AfterRoll(); }
            if (flow == Flow.End || flow == Flow.Title || flow == Flow.Opening || deck == null || practice) { practicedOnce = true; BeginRun(); }
            if (flow == Flow.Transition) FinishTransitionNow();
            if (flow == Flow.Draw) CompleteRadius("test_radius"); // 실제 선긋기 완료와 같은 함수
        }

        public void TestAnswerCorrect() { EnsureTestInput(); if (flow == Flow.Aim) Submit(current.answer); }
        public void TestAnswerWrong() { EnsureTestInput(); if (flow == Flow.Aim) Submit(HanKkeutRules.ParallelValue(current)); }

        void FinishTransitionNow()
        {
            if (flow != Flow.Transition) return;
            SnapCameraNow();
            EnterInput();
        }

        public string StateJson()
        {
            st.layoutLand = MgfBridge.IsLandLayout;
            return JsonUtility.ToJson(st);
        }

        public string ProblemBankJson() => MgfJson.Bank(bank);
    }
}
