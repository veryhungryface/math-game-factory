using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.FoldlineLab
{
    // 폴드라인 — 학생이 선을 긋고, 코드는 그 선에 대한 반사 결과만 판정한다.
    // 실전 정답 경로는 드래그 직선 또는 함정판의 안전 깃 드래그뿐이며 선택지 버튼은 없다.
    public partial class FoldlineLabGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int sail;
            public int sailTotal = FoldlineRules.Goal;
            public int firstCorrect;
            public int firstAttempted;
            public int attempts;
            public int combo;
            public int scannerStep;
            public int pointerVersion;
            public int creases;
            public float exactCenterSpread;
            public bool onboarding;
            public bool frozen;
            public bool trap;
            public bool inputLocked;
            public string problemId = "";
            public string mode = "";
            public string lastResult = "";
            public string misconceptionId = "";
            public string feedbackText = "";
            public string endReason = "";
            public string inputTargets = "";
            public string diagnosticTarget = "";
        }

        enum Flow { Title, Practice, Drawing, Resolving, End }
        enum DragMode { None, Line, Flag, TitleCord }

        readonly State st = new State();
        readonly List<MgfProblem> bank = new List<MgfProblem>();
        readonly List<FoldLine> acceptedCreases = new List<FoldLine>(2);
        // 학생이 그은 허용선은 조작 흔적으로 남기되, 수학 중심 시각화에는
        // 각 문항의 정확한 목표선만 사용한다. 두 목록을 섞으면 허용오차 경계의
        // 정답선 두 개가 만드는 교점이 외심/내심의 정의와 어긋날 수 있다.
        readonly List<FoldLine> exactCreases = new List<FoldLine>(2);
        List<FoldTask> deck;
        FoldTask task;
        Flow flow = Flow.Title;
        DragMode drag = DragMode.None;
        int index;
        int runSerial = 20261016;
        bool firstAttempt;
        bool practice;
        bool fastMode;
        bool pendingEnd;
        bool pendingClear;
        string pendingReason = "";
        float runClock;
        float idleClock;
        float resolveClock;
        float resolveDuration;
        float hitStop;
        Vector2 downScreen;
        Vector2 dragStartWorld;
        Vector2 dragEndWorld;
        Vector2 flagWorld;
        FoldLine submitted;
        FoldJudge lastJudge;

        void Awake()
        {
            MgfLook.Quality(38f);
            fastMode = (Application.absoluteURL ?? "").Contains("mgfFast=1");
            if (fastMode) Time.timeScale = 5f;
            string rulesError;
            if (!FoldlineRules.Validate(out rulesError)) Debug.LogError("[Foldline] rules invalid: " + rulesError);
            bank.AddRange(FoldlineRules.BuildBank());
            Prewarm();
            BuildWorld();
            BuildUi();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb = new StringBuilder("폴드라인접는선을그어중심을밝혀라태양돛당김끈을아래로끌어시작접은쪽이딱겹치게선을그어접으시오");
            sb.Append("중학교2학년삼각형의성질외심내심수직이등분선각의이등분선이등변삼각형꼭지각밑각밑변꼭짓점포개기안전깃불가능");
            sb.Append("오접힘조율잠금열스캐너첫판단정확완료다시조율최고기록접는중겹쳤다방향위치를옮겨라흰자국교점외접원내접원");
            sb.Append("두밑각의크기가같고접는선이밑변을수직으로반씩나눈다세꼭짓점까지의거리가같다세변까지의거리가같다");
            sb.Append("선이너무짧다돛위에서시작하시오안쪽에만있지않는다손을놓아접으시오0123456789/6+°∠△⊥→·%ABCOI");
            for (int i = 0; i < bank.Count && i < 80; i++) sb.Append(bank[i].prompt).Append(bank[i].answer);
            MgfText.Prewarm(sb.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(.05f, Time.deltaTime);
            ApplyLayoutIfNeeded();
            HandleInput();

            if (hitStop > 0f)
            {
                hitStop -= Time.unscaledDeltaTime;
                UpdateWorld(0f);
                UpdateUi(0f);
                return;
            }

            if (flow == Flow.Practice || flow == Flow.Drawing)
            {
                idleClock += dt;
                if (!practice)
                {
                    runClock += dt;
                    SetScannerProgress(runClock / FoldlineRules.SessionSeconds);
                    if (runClock >= FoldlineRules.SessionSeconds) EndRun(false, "열 스캐너가 돛 끝에 닿았다");
                }
            }
            else if (flow == Flow.Resolving)
            {
                resolveClock += dt;
                StepResolution();
            }

            UpdateWorld(dt);
            UpdateUi(dt);
        }

        void ShowTitle()
        {
            flow = Flow.Title;
            drag = DragMode.None;
            practice = false;
            task = null;
            st.phase = "title";
            st.score = 0; st.lives = FoldlineRules.StartLives; st.level = 1; st.solved = 0;
            st.sail = 0; st.firstCorrect = 0; st.firstAttempted = 0; st.attempts = 0; st.combo = 0;
            st.scannerStep = 0; st.pointerVersion = 0; st.creases = 0;
            st.onboarding = false; st.frozen = true; st.trap = false; st.inputLocked = false;
            st.problemId = ""; st.mode = "title"; st.lastResult = ""; st.misconceptionId = ""; st.feedbackText = ""; st.endReason = "";
            acceptedCreases.Clear(); exactCreases.Clear();
            ResetStageVisuals();
            SetScreen();
            WriteState();
        }

        void BeginFromTitle()
        {
            if (PlayerPrefs.GetInt("foldline_practice", 0) == 1) StartRun();
            else StartPractice();
        }

        void StartPractice()
        {
            practice = true;
            task = FoldlineRules.Practice();
            flow = Flow.Practice;
            firstAttempt = true;
            idleClock = 0f;
            st.phase = "practice"; st.onboarding = true; st.frozen = true; st.trap = false; st.inputLocked = false;
            st.problemId = task.id; st.mode = "symmetry"; st.lastResult = ""; st.misconceptionId = ""; st.feedbackText = "";
            st.level = 1; st.sail = 0; st.creases = 0;
            acceptedCreases.Clear(); exactCreases.Clear();
            LoadTaskVisual(task, true);
            SetGoal("접은 쪽이 딱 겹치게 선을 그어 접으시오");
            ShowGuide(true);
            SetScreen();
            WriteState();
        }

        void StartRun()
        {
            practice = false;
            runSerial++;
            if (runSerial >= FoldlineRules.BankSerialBase + FoldlineRules.BankDeckCount) runSerial = FoldlineRules.BankSerialBase;
            deck = FoldlineRules.RuntimeDeck(runSerial);
            index = 0;
            runClock = 0f;
            st.phase = "playing"; st.score = 0; st.lives = FoldlineRules.StartLives; st.level = 1; st.solved = 0;
            st.sail = 1; st.firstCorrect = 0; st.firstAttempted = 0; st.attempts = 0; st.combo = 0;
            st.scannerStep = 0; st.creases = 0; st.onboarding = false; st.frozen = false;
            st.lastResult = ""; st.misconceptionId = ""; st.feedbackText = ""; st.endReason = ""; st.exactCenterSpread = 0f;
            acceptedCreases.Clear(); exactCreases.Clear();
            BeginTask();
            SetScreen();
            WriteState();
        }

        void BeginTask()
        {
            if (index >= deck.Count)
            {
                bool mastery = st.firstCorrect >= FoldlineRules.RequiredFirstCorrect;
                EndRun(mastery, mastery ? "여섯 돛의 중심 리브 체결 완료" : "첫 시도 정답이 5/6에 미치지 못했다");
                return;
            }
            task = deck[index];
            firstAttempt = true;
            idleClock = 0f;
            flow = Flow.Drawing;
            st.phase = "playing"; st.sail = index + 1; st.level = task.level; st.problemId = task.id;
            st.mode = ModeName(task.kind); st.trap = task.kind == FoldTaskKind.Impossible; st.inputLocked = false;
            st.lastResult = ""; st.misconceptionId = ""; st.feedbackText = "";
            if (index == 0 || deck[index - 1].group != task.group)
            {
                acceptedCreases.Clear();
                exactCreases.Clear();
            }
            st.creases = acceptedCreases.Count;
            LoadTaskVisual(task, false);
            SetGoal(task.prompt);
            ShowGuide(index == 0);
            SetScreen();
            WriteState();
        }

        static string ModeName(FoldTaskKind k)
        {
            switch (k)
            {
                case FoldTaskKind.Symmetry: return "symmetry";
                case FoldTaskKind.VertexPair: return "circumcenter";
                case FoldTaskKind.SidePair: return "incenter";
                default: return "impossible";
            }
        }

        void HandleInput()
        {
            Vector2 pos = MgfPointer.Position;
            if (MgfPointer.Down)
            {
                st.pointerVersion++;
                downScreen = pos;
                SpawnRipple(pos);
                idleClock = 0f;
                if (flow == Flow.Title)
                {
                    drag = DragMode.TitleCord;
                    GrabCord();
                    WriteState();
                    return;
                }
                if (flow == Flow.End)
                {
                    if (HitRestart(pos)) StartRun();
                    else { Refuse("아래의 ‘다시 조율’ 글자를 누르시오."); PulseRestart(); }
                    return;
                }
                if (flow == Flow.Resolving)
                {
                    Refuse("접힌 결과를 확인하는 중이다.");
                    return;
                }
                if (flow != Flow.Practice && flow != Flow.Drawing) return;

                if (task.kind == FoldTaskKind.Impossible && HitSafetyFlag(pos))
                {
                    drag = DragMode.Flag;
                    flagWorld = ScreenToStage(pos);
                    GrabFlag();
                    WriteState();
                    return;
                }
                Vector2 world = ScreenToStage(pos);
                // 연습에서는 화면에 보이는 유령 손의 넓은 이동 띠 전체를 잡을 수 있다.
                // 저프레임 입력에서 정확한 시작 구를 놓쳐도 실제 드래그 길이는 학생이 만든다.
                bool practiceGrabBand = practice && pos.x >= Screen.width * .08f && pos.x <= Screen.width * .92f
                    && pos.y >= Screen.height * .22f && pos.y <= Screen.height * .78f;
                if (!InsideFrame(world) && !practiceGrabBand)
                {
                    Refuse(task.kind == FoldTaskKind.Impossible ? "안전 깃을 돛 위로 끌어 꽂으시오." : "태양돛 위에서 선을 시작하시오.");
                    BouncePins();
                    PointAtTarget();
                    WriteState();
                    return;
                }
                drag = DragMode.Line;
                if (practice)
                {
                    // 유령 손 경로와 실제 hit 영역을 같은 정답선 띠에 맞춘다.
                    float along = Vector2.Dot(world - task.target.point, task.target.direction);
                    dragStartWorld = task.target.point + task.target.direction * Mathf.Clamp(along, -2.7f, 2.7f);
                }
                else dragStartWorld = world;
                dragEndWorld = dragStartWorld;
                BeginLinePreview(dragStartWorld);
                WriteState();
            }
            else if (MgfPointer.Held)
            {
                if (drag == DragMode.Line)
                {
                    dragEndWorld = ScreenToStage(pos);
                    UpdateLinePreview(dragStartWorld, dragEndWorld);
                }
                else if (drag == DragMode.Flag)
                {
                    flagWorld = ScreenToStage(pos);
                    MoveFlag(flagWorld);
                }
                else if (drag == DragMode.TitleCord)
                {
                    PullCord(Mathf.Max(0f, downScreen.y - pos.y));
                }
            }

            if (MgfPointer.Up)
            {
                if (drag == DragMode.TitleCord)
                {
                    float pull = Mathf.Max(0f, downScreen.y - pos.y);
                    ReleaseCord();
                    drag = DragMode.None;
                    if (pull >= 22f || downScreen.y < Screen.height * .62f) BeginFromTitle();
                    else { Refuse("당김끈을 아래로 끌어 조율판을 여시오."); PulseCord(); WriteState(); }
                    return;
                }
                if (drag == DragMode.Line)
                {
                    drag = DragMode.None;
                    FinishLinePreview();
                    float pixels = Vector2.Distance(downScreen, pos);
                    if (pixels < 38f || Vector2.Distance(dragStartWorld, dragEndWorld) < .55f)
                    {
                        Refuse("선을 길게 끌어 그은 뒤 손을 놓으시오.");
                        PointAtTarget();
                        WriteState();
                    }
                    else
                    {
                        // 연습은 조작 학습 구간이다. 충분히 긴 실제 드래그를 하면 화면의 유령선에
                        // 맞춰 접고, 본판부터는 어떤 보정도 하지 않는다.
                        submitted = practice ? task.target : new FoldLine(dragStartWorld, dragEndWorld - dragStartWorld);
                        if (practice) ShowPracticeAssist();
                        SubmitCrease(submitted);
                    }
                }
                else if (drag == DragMode.Flag)
                {
                    drag = DragMode.None;
                    ReleaseFlag();
                    if (FoldlineRules.PointInTriangle(flagWorld, task.a, task.b, task.c)) SubmitFlag();
                    else { Refuse("안전 깃 끝을 삼각형 안에 꽂으시오."); ReturnFlag(); WriteState(); }
                }
            }
        }

        bool InsideFrame(Vector2 p) { return Mathf.Abs(p.x) <= 5.25f && Mathf.Abs(p.y) <= 4.4f; }

        void SubmitCrease(FoldLine line)
        {
            submitted = line;
            if (task.kind == FoldTaskKind.Impossible)
            {
                lastJudge = new FoldJudge { correct = false, misconceptionId = "scalene_symmetry_assumed", angleError = 90f, offsetError = 9f };
                ResolveSubmission(false, "scalene_symmetry_assumed", line);
                return;
            }
            lastJudge = FoldlineRules.Judge(line, task);
            ResolveSubmission(lastJudge.correct, lastJudge.misconceptionId, line);
        }

        void SubmitFlag()
        {
            submitted = null;
            lastJudge = new FoldJudge { correct = task.kind == FoldTaskKind.Impossible, misconceptionId = task.kind == FoldTaskKind.Impossible ? "" : "false_impossible" };
            ResolveSubmission(lastJudge.correct, lastJudge.misconceptionId, null);
        }

        void ResolveSubmission(bool correct, string misconception, FoldLine line)
        {
            if (flow != Flow.Practice && flow != Flow.Drawing) return;
            st.attempts++;
            bool recordedFirstAttempt = !practice && firstAttempt;
            if (recordedFirstAttempt)
            {
                st.firstAttempted++;
                if (correct) st.firstCorrect++;
            }
            firstAttempt = false;
            st.misconceptionId = misconception ?? "";
            st.lastResult = correct ? "correct" : "wrong";
            st.inputLocked = true;
            flow = Flow.Resolving;
            resolveClock = 0f;
            resolveDuration = fastMode ? .11f : (correct ? 1.45f : 1.25f);
            pendingEnd = false; pendingClear = correct;

            if (correct)
            {
                st.combo++;
                int gain = 100 + Mathf.Min(4, st.combo) * 25;
                st.score += gain;
                if (!practice) runClock = Mathf.Max(0f, runClock - 6.5f);
                if (line != null)
                {
                    acceptedCreases.Add(line);
                    exactCreases.Add(task.target);
                }
                st.creases = acceptedCreases.Count;
                if (practice)
                {
                    pendingReason = "practice";
                    ShowCorrectEvidence("두 밑각의 크기가 같다\n접는 선은 밑변을 수직으로 반씩 나눈다");
                }
                else if (task.haloAfter && acceptedCreases.Count >= 2)
                {
                    pendingReason = task.circumHalo ? "circum-halo" : "in-halo";
                    if (task.circumHalo) ShowCorrectEvidence("두 수직이등분선의 교점 O\n세 꼭짓점까지의 거리가 같다");
                    else ShowCorrectEvidence("두 각의 이등분선의 교점 I\n세 변까지의 거리가 같다");
                }
                else
                {
                    pendingReason = "next";
                    ShowCorrectEvidence(task.kind == FoldTaskKind.Impossible
                        ? "세 변의 길이가 모두 달라 선대칭축이 없다"
                        : (task.kind == FoldTaskKind.VertexPair ? "두 꼭짓점에서 같은 거리인 자국이다" : "두 변이 포개지는 각의 이등분선이다"));
                }
                BeginFoldAnimation(line, true, task.haloAfter, task.circumHalo, exactCreases);
                hitStop = fastMode ? 0f : .08f;
                MgfSfx.Play(task.haloAfter ? "win" : "correct", .48f);
            }
            else
            {
                st.combo = 0;
                if (!practice)
                {
                    st.lives--;
                    runClock = Mathf.Min(FoldlineRules.SessionSeconds, runClock + 4f);
                    if (st.lives <= 0) { pendingEnd = true; pendingReason = "오접힘 세 개로 조율 프레임이 잠겼다"; }
                    else if (recordedFirstAttempt && st.firstCorrect + (FoldlineRules.Goal - st.firstAttempted) < FoldlineRules.RequiredFirstCorrect)
                    {
                        pendingEnd = true;
                        pendingReason = "첫 시도 목표 5/6을 더 이상 달성할 수 없다";
                    }
                }
                string reason = FeedbackFor(misconception, lastJudge);
                st.feedbackText = reason;
                ShowWrongEvidence(reason);
                BeginFoldAnimation(line, false, false, false, acceptedCreases);
                MgfSfx.Play("wrong", .42f);
                ShakeWrong(st.lives <= 1 ? .19f : .11f);
            }
            WriteState();
        }

        static string FeedbackFor(string id, FoldJudge j)
        {
            switch (id)
            {
                case "visual_top_vertex": return "화면 위쪽이 아니라 같은 두 변의 끼인각을 찾으시오";
                case "circumcenter_incenter_swapped": return "외심은 세 변의 수직이등분선의 교점이고, 내심은 세 내각의 이등분선의 교점이다";
                case "outside_center_denial": return "둔각삼각형의 외심은 돛 밖에 있을 수 있다";
                case "midpoint_without_perpendicular": return "중점만 지나지 말고 선분과 수직이어야 한다";
                case "offset_only_miss": return "방향은 가깝다. 선을 평행하게 옮겨 두 대상을 정확히 포개시오";
                case "direction_only_miss": return "위치는 가깝다. 선의 방향을 돌려 두 대상을 포개시오";
                case "scalene_symmetry_assumed": return "AB=4 cm, BC=5 cm, CA=3 cm로 같은 길이인 두 변이 없어 선대칭축이 없다";
                default:
                    if (j.angleError > FoldlineRules.AngleTolerance && j.offsetError > FoldlineRules.OffsetTolerance) return "방향과 위치가 모두 어긋났다. 기준 점선을 확인하시오";
                    return "접힌 두 면이 포개지지 않았다. 기준 점선을 확인하시오";
            }
        }

        void StepResolution()
        {
            StepFoldAnimation(resolveClock, resolveDuration, pendingClear);
            if (resolveClock < resolveDuration) return;
            HideEvidence();
            st.inputLocked = false;
            if (pendingEnd) { EndRun(false, pendingReason); return; }
            if (practice)
            {
                if (pendingClear)
                {
                    PlayerPrefs.SetInt("foldline_practice", 1);
                    PlayerPrefs.Save();
                    StartRun();
                }
                else
                {
                    flow = Flow.Practice;
                    st.phase = "practice"; st.inputLocked = false;
                    ResetAfterFold(false);
                    ShowGuide(true);
                    WriteState();
                }
                return;
            }
            if (pendingClear)
            {
                st.solved++;
                index++;
                ResetAfterFold(true);
                BeginTask();
            }
            else
            {
                flow = Flow.Drawing;
                st.phase = "playing"; st.inputLocked = false;
                ResetAfterFold(false);
                WriteState();
            }
        }

        void EndRun(bool clear, string reason)
        {
            if (flow == Flow.End) return;
            flow = Flow.End;
            drag = DragMode.None;
            st.phase = clear ? "clear" : "gameover";
            st.endReason = reason;
            st.inputLocked = true;
            int best = PlayerPrefs.GetInt("foldline_best", 0);
            if (st.firstCorrect > best) PlayerPrefs.SetInt("foldline_best", st.firstCorrect);
            PlayerPrefs.Save();
            ShowEnd(clear, reason);
            MgfSfx.Play(clear ? "win" : "lose", .58f);
            WriteState();
        }

        void WriteState()
        {
            st.inputTargets = BuildInputTargetString();
            st.diagnosticTarget = BuildDiagnosticTargetString();
            MgfBridge.NotifyChanged();
        }

        public void TestStart()
        {
            StartRun();
            MgfBridge.NotifyChanged();
        }

        public void TestAnswerCorrect()
        {
            if (flow != Flow.Drawing && flow != Flow.Practice) StartRun();
            if (task.kind == FoldTaskKind.Impossible) SubmitFlag();
            else SubmitCrease(task.target);
            MgfBridge.NotifyChanged();
        }

        public void TestAnswerWrong()
        {
            if (flow != Flow.Drawing && flow != Flow.Practice) StartRun();
            if (task.kind == FoldTaskKind.Impossible)
            {
                var wrong = FoldlineRules.Through(task.a, (task.b + task.c) * .5f);
                SubmitCrease(wrong);
            }
            else
            {
                SubmitCrease(new FoldLine(task.target.point + task.target.Normal * 1.4f,
                    FoldlineRules.Rot(task.target.direction, 23f)));
            }
            MgfBridge.NotifyChanged();
        }

        public string StateJson()
        {
            // 뷰포트·카메라가 바뀐 뒤에도 실제 현재 좌표를 내보낸다. 시간값은 상태에 넣지 않는다.
            st.inputTargets = BuildInputTargetString();
            st.diagnosticTarget = BuildDiagnosticTargetString();
            return JsonUtility.ToJson(st);
        }
        public string ProblemBankJson() { return MgfJson.Bank(bank); }
    }
}
