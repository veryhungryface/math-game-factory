// 나비 재봉소 — 플레이 상태, 실제 포인터 입력, SSS 작도 판정, 실패/회복 루프.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.NabiJaebongso
{
    public sealed partial class NabiJaebongsoGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int round;
            public int firstAttempts;
            public int firstCorrect;
            public int attemptsOnCurrent;
            public int timeLeft;
            public int compassSourceEdge = -1;
            public int compassCenter = -1;
            public int arcs;
            public int drawnSegments;
            public int inputSerial;
            public float sourceAScreenX;
            public float sourceAScreenY;
            public float sourceBScreenX;
            public float sourceBScreenY;
            public float sourceCScreenX;
            public float sourceCScreenY;
            public float targetLeftScreenX;
            public float targetLeftScreenY;
            public float targetRightScreenX;
            public float targetRightScreenY;
            public float targetPanelBottomScreenY;
            public float promptBottomScreenY;
            public float toolHintTopScreenY;
            public float sourceLabelTopCenterScreenY;
            public float hingeScreenX;
            public float hingeScreenY;
            public float pencilScreenX;
            public float pencilScreenY;
            public float compassButtonScreenX;
            public float compassButtonScreenY;
            public float rulerButtonScreenX;
            public float rulerButtonScreenY;
            public string tool = "compass";
            public string problemId = "";
            public string screenPrompt = "";
            public string misconceptionId = "";
        }

        enum RunPhase { Title, Practice, Playing, Reveal, End, Help }
        enum ToolMode { Compass, Ruler }
        enum DragKind { None, SourceEdge, Hinge, Arc, SegmentLeft, SegmentRight }

        readonly State st = new State();
        readonly List<NabiProblem> problemBank = new List<NabiProblem>(NabiRules.BankSize);
        readonly int[] arcEdge = { -1, -1 };
        readonly bool[] arcPresent = { false, false };
        readonly float[] arcStart = { 0f, 0f };
        readonly float[] arcEnd = { 0f, 0f };
        readonly bool[] arcProvided = { false, false };

        RunPhase phase = RunPhase.Title;
        ToolMode tool = ToolMode.Compass;
        DragKind drag = DragKind.None;
        NabiProblem current;
        List<NabiProblem> deck;
        System.Random rng;
        int runSerial;
        int sourceDragStart = -1;
        bool sourceDragFromCorridor;
        bool sourceDragReachedTarget;
        bool practiceGuideStrong;
        int practiceFailedInputs;
        float sourceDragStartT;
        int selectedSourceEdge = -1;
        int compassCenter = -1;
        bool segmentLeft, segmentRight;
        Vector2 segmentLeftEnd, segmentRightEnd;
        Vector2 segmentPreviewEnd;
        float tracedStartAngle, tracedCurrentAngle;
        bool firstAttempt;
        bool revealCorrect, revealFirst, pendingTerminal;
        float secondsLeft = NabiRules.RunSeconds;
        float revealClock, idleClock, practiceNoProgressClock, titleClock, hitStop;
        string endReason = "";
        Vector2 lastBoardPointer;

        const float HitRadius = .56f;
        const float MinArcSweep = .30f;
        const float MinSegmentLength = .8f;

        void Awake()
        {
            MgfLook.Quality(38f);
            problemBank.AddRange(NabiRules.BuildBank());
            BuildMaterials();
            BuildWorld();
            BuildUi();
            Prewarm();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb = new StringBuilder("나비재봉소원을그려날개를펴라중학교1학년작도와합동고치를열어연습시작");
            sb.Append("같은길이로날개맥을짜라날개를그리면나비가난다두원호빛실셔틀재봉소열기어떻게놀아요컴퍼스폭중심원호교점눈금없는자대응변SSS합동");
            sb.Append("실타래첫제출시간날개완성수선다시재봉도구를고르시오원본변두꼭짓점을이어폭을잡으시오");
            sb.Append("중심손잡이를밑변끝점으로옮기시오연필끝을원둘레로움직이시오교점까지두선을그으시오");
            sb.Append("세대응변의길이가같으므로SSS합동화면좌우가아니라기호순서대로대응을읽으시오");
            for (int i = 0; i < problemBank.Count; i++) { sb.Append(problemBank[i].prompt); sb.Append(problemBank[i].answer); }
            MgfText.Prewarm(sb.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(.05f, Time.deltaTime);
            titleClock += dt;
            UpdateLayoutIfNeeded();
            HandleInput();

            if (hitStop > 0f) hitStop -= dt;
            else if (phase == RunPhase.Practice)
            {
                idleClock += dt;
                practiceNoProgressClock += dt;
                if (!practiceGuideStrong && selectedSourceEdge < 0 && practiceNoProgressClock >= 6f)
                {
                    practiceGuideStrong = true;
                    ReplayGuide(true);
                }
                else if (idleClock >= 8f)
                {
                    idleClock = 0f;
                    ReplayGuide(true);
                }
            }
            else if (phase == RunPhase.Playing)
            {
                secondsLeft -= dt;
                if (secondsLeft <= 0f) { secondsLeft = 0f; EndRun("time"); }
            }
            else if (phase == RunPhase.Reveal)
            {
                revealClock += dt;
                if (revealClock >= (revealCorrect ? 1.65f : 1.9f)) FinishReveal();
            }

            UpdateWorld(dt);
            UpdateUi(dt);
        }

        void ShowTitle()
        {
            phase = RunPhase.Title;
            st.phase = "title";
            st.score = 0;
            st.lives = NabiRules.StartLives;
            st.level = 1;
            st.solved = 0;
            st.round = 0;
            st.firstAttempts = 0;
            st.firstCorrect = 0;
            st.attemptsOnCurrent = 0;
            st.timeLeft = (int)NabiRules.RunSeconds;
            st.problemId = "";
            st.screenPrompt = "";
            st.misconceptionId = "";
            st.inputSerial = 0;
            current = null;
            deck = null;
            endReason = "";
            ResetConstruction();
            ResetWorldForRun();
            ShowTitleVisual();
            SetScreen();
            MgfBridge.NotifyChanged();
        }

        void ResetRun()
        {
            runSerial++;
            rng = new System.Random(unchecked(Environment.TickCount ^ runSerial * 104729));
            deck = NabiRules.RunDeck(problemBank, rng);
            st.score = 0;
            st.lives = NabiRules.StartLives;
            st.level = 1;
            st.solved = 0;
            st.round = 0;
            st.firstAttempts = 0;
            st.firstCorrect = 0;
            st.attemptsOnCurrent = 0;
            st.timeLeft = (int)NabiRules.RunSeconds;
            st.problemId = "";
            st.screenPrompt = "";
            st.misconceptionId = "";
            st.inputSerial = 0;
            secondsLeft = NabiRules.RunSeconds;
            endReason = "";
            pendingTerminal = false;
            ResetConstruction();
            ResetWorldForRun();
        }

        void StartPractice()
        {
            ResetRun();
            phase = RunPhase.Practice;
            st.phase = "practice";
            current = NabiRules.Practice();
            st.problemId = current.id;
            st.screenPrompt = current.prompt;
            st.level = 0;
            st.round = 0;
            firstAttempt = true;
            idleClock = 0f;
            practiceNoProgressClock = 0f;
            practiceGuideStrong = false;
            practiceFailedInputs = 0;
            ResetConstruction();
            // 첫 연습의 D 중심 AC 원호는 명시적인 시범이며 통계에 포함하지 않는다.
            arcPresent[0] = true;
            arcProvided[0] = true;
            arcEdge[0] = current.expectedLeftEdge;
            arcStart[0] = .08f;
            arcEnd[0] = Mathf.PI - .08f;
            SyncConstructionState();
            ResetWorldForProblem(current);
            RefreshProblemUi();
            SetScreen();
            ReplayGuide(false);
            MgfSfx.Play("whoosh", .34f);
            MgfBridge.NotifyChanged();
        }

        void StartRunDirect()
        {
            ResetRun();
            phase = RunPhase.Playing;
            st.phase = "playing";
            NextProblem();
            SetScreen();
            MgfBridge.NotifyChanged();
        }

        void BeginRunAfterPractice()
        {
            st.score = 0;
            displayedScore = 0f;
            st.solved = 0;
            st.round = 0;
            st.firstAttempts = 0;
            st.firstCorrect = 0;
            st.attemptsOnCurrent = 0;
            st.lives = NabiRules.StartLives;
            st.misconceptionId = "";
            secondsLeft = NabiRules.RunSeconds;
            st.timeLeft = (int)secondsLeft;
            phase = RunPhase.Playing;
            st.phase = "playing";
            NextProblem();
            SetScreen();
        }

        void NextProblem()
        {
            if (st.solved >= NabiRules.TargetWings) { EndRun(st.firstCorrect >= NabiRules.NeededFirstCorrect ? "clear" : "mastery"); return; }
            current = deck[st.solved];
            st.round = st.solved + 1;
            st.level = current.band;
            st.problemId = current.id;
            st.screenPrompt = current.prompt;
            st.attemptsOnCurrent = 0;
            st.misconceptionId = "";
            firstAttempt = true;
            ResetConstruction();
            if (current.band == 2 && current.initialWrongCenter >= 0)
            {
                int c = current.initialWrongCenter;
                arcPresent[c] = true;
                arcProvided[c] = true;
                arcEdge[c] = current.initialWrongEdge;
                arcStart[c] = .08f;
                arcEnd[c] = Mathf.PI - .08f;
            }
            SyncConstructionState();
            ResetWorldForProblem(current);
            RefreshProblemUi();
            st.timeLeft = Mathf.CeilToInt(secondsLeft);
            MgfBridge.NotifyChanged();
        }

        void ResetConstruction()
        {
            drag = DragKind.None;
            sourceDragStart = -1;
            sourceDragFromCorridor = false;
            sourceDragReachedTarget = false;
            sourceDragStartT = 0f;
            selectedSourceEdge = -1;
            compassCenter = -1;
            tool = ToolMode.Compass;
            segmentLeft = false;
            segmentRight = false;
            segmentLeftEnd = Vector2.zero;
            segmentRightEnd = Vector2.zero;
            for (int i = 0; i < 2; i++)
            {
                arcPresent[i] = false;
                arcProvided[i] = false;
                arcEdge[i] = -1;
                arcStart[i] = 0f;
                arcEnd[i] = 0f;
            }
            SyncConstructionState();
        }

        void SyncConstructionState()
        {
            st.compassSourceEdge = selectedSourceEdge;
            st.compassCenter = compassCenter;
            st.arcs = (arcPresent[0] ? 1 : 0) + (arcPresent[1] ? 1 : 0);
            st.drawnSegments = (segmentLeft ? 1 : 0) + (segmentRight ? 1 : 0);
            st.tool = tool == ToolMode.Compass ? "compass" : "ruler";
            if (phase == RunPhase.Playing) st.timeLeft = Mathf.CeilToInt(secondsLeft);
        }

        void HandleInput()
        {
            if (!MgfPointer.Down && !MgfPointer.Held && !MgfPointer.Up) return;

            if (phase == RunPhase.Title)
            {
                if (MgfPointer.Down)
                {
                    st.inputSerial++;
                    if (HitHelp(MgfPointer.Position)) ShowHelp();
                    else StartPractice();
                }
                return;
            }
            if (phase == RunPhase.Help)
            {
                if (MgfPointer.Down) { st.inputSerial++; HideHelpAndStart(); }
                return;
            }
            if (phase == RunPhase.End)
            {
                if (MgfPointer.Down) { st.inputSerial++; StartPractice(); }
                return;
            }
            if (phase == RunPhase.Reveal)
            {
                if (MgfPointer.Down) { st.inputSerial++; RefuseInput(MgfPointer.Position, "설명을 확인한 뒤 다시 수선하시오."); }
                return;
            }
            if (phase != RunPhase.Practice && phase != RunPhase.Playing) return;

            Vector2 local;
            bool onBoard = ScreenToBoardLocal(MgfPointer.Position, out local);
            if (onBoard) lastBoardPointer = local;

            if (MgfPointer.Down)
            {
                st.inputSerial++;
                idleClock = 0f;
                if (HitToolButton(MgfPointer.Position, out ToolMode hitTool))
                {
                    tool = hitTool;
                    drag = DragKind.None;
                    MgfSfx.Play("tap", .28f);
                    PunchTool(hitTool);
                    SyncConstructionState();
                    MgfBridge.NotifyChanged();
                    return;
                }
                if (!onBoard)
                {
                    RefuseInput(MgfPointer.Position, "작도 면 안에서 도구를 움직이시오.");
                    return;
                }
                if (tool == ToolMode.Compass) BeginCompassDrag(local);
                else BeginRulerDrag(local);
            }

            if (MgfPointer.Held && onBoard) ContinueDrag(local);
            if (MgfPointer.Up) EndDrag(onBoard ? local : lastBoardPointer, onBoard);
        }

        void BeginCompassDrag(Vector2 p)
        {
            // 폭과 중심을 이미 잡은 뒤에는 화면에 보이는 연필 손잡이가 입력 우선이다.
            // 큰 반지름의 연필 끝이 회전된 원본 꼭짓점과 겹칠 수 있어, 꼭짓점을 먼저
            // 검사하면 사용자가 연필을 잡아도 새 변 선택으로 오인되어 원호를 못 그린다.
            if (selectedSourceEdge >= 0 && compassCenter >= 0 &&
                Vector2.Distance(p, PencilLocal) <= HitRadius * 1.2f)
            {
                Vector2 c = compassCenter == 0 ? TargetLeftLocal : TargetRightLocal;
                drag = DragKind.Arc;
                tracedStartAngle = Mathf.Atan2(p.y - c.y, p.x - c.x);
                tracedCurrentAngle = tracedStartAngle;
                SetArcPreview(compassCenter, selectedSourceEdge, tracedStartAngle, tracedStartAngle + .02f);
                return;
            }
            // 첫 고정 연습은 B→C 선분 자체가 44 CSS px 이상의 입력 레일이다. 꼭짓점의
            // 작은 월드 점을 정확히 찍게 하지 않고 강조된 BC 어디서든 시작할 수 있다.
            // 단, C 방향으로 실제 거리를 따라가야 하므로 탭이나 엉뚱한 드래그는 통과하지 않는다.
            if (phase == RunPhase.Practice && selectedSourceEdge < 0 &&
                PointNearSegment(p, sourcePoints[1], sourcePoints[2], HitRadius * 2.20f, out float practiceT) && practiceT <= .82f)
            {
                sourceDragStart = 1;
                sourceDragFromCorridor = true;
                sourceDragReachedTarget = false;
                sourceDragStartT = practiceT;
                drag = DragKind.SourceEdge;
                SetSourceDragVisual(1, p);
                MgfSfx.Play("tap", .24f);
                return;
            }
            float sourceHit = HitRadius;
            int v = ClosestSourceVertex(p, sourceHit);
            if (v >= 0)
            {
                sourceDragStart = v;
                sourceDragFromCorridor = false;
                sourceDragReachedTarget = false;
                drag = DragKind.SourceEdge;
                SetSourceDragVisual(v, p);
                MgfSfx.Play("tap", .24f);
                return;
            }
            if (selectedSourceEdge >= 0 && Vector2.Distance(p, HingeLocal) <= HitRadius * 1.25f)
            {
                drag = DragKind.Hinge;
                SetHingeVisual(p, true);
                MgfSfx.Play("tap", .25f);
                return;
            }
            if (selectedSourceEdge >= 0 && compassCenter >= 0)
            {
                Vector2 c = compassCenter == 0 ? TargetLeftLocal : TargetRightLocal;
                float r = EdgeWorldLength(selectedSourceEdge);
                float distanceError = Mathf.Abs(Vector2.Distance(p, c) - r);
                if (distanceError <= HitRadius * .9f || Vector2.Distance(p, PencilLocal) <= HitRadius * 1.2f)
                {
                    drag = DragKind.Arc;
                    tracedStartAngle = Mathf.Atan2(p.y - c.y, p.x - c.x);
                    tracedCurrentAngle = tracedStartAngle;
                    SetArcPreview(compassCenter, selectedSourceEdge, tracedStartAngle, tracedStartAngle + .02f);
                    return;
                }
            }
            RefuseInput(MgfPointer.Position, selectedSourceEdge < 0 ? "원본의 두 꼭짓점을 이어 컴퍼스 폭을 잡으시오." :
                compassCenter < 0 ? "도자 중심 손잡이를 밑변 끝점으로 옮기시오." : "연필 끝을 원둘레로 움직이시오.");
        }

        void BeginRulerDrag(Vector2 p)
        {
            if (Vector2.Distance(p, TargetLeftLocal) <= HitRadius * 1.25f)
            {
                drag = DragKind.SegmentLeft;
                segmentPreviewEnd = p;
                SetSegmentPreview(0, TargetLeftLocal, p);
                MgfSfx.Play("tap", .24f);
                return;
            }
            if (Vector2.Distance(p, TargetRightLocal) <= HitRadius * 1.25f)
            {
                drag = DragKind.SegmentRight;
                segmentPreviewEnd = p;
                SetSegmentPreview(1, TargetRightLocal, p);
                MgfSfx.Play("tap", .24f);
                return;
            }
            RefuseInput(MgfPointer.Position, "밑변 끝점에서 원호의 교점까지 자를 그으시오.");
        }

        void ContinueDrag(Vector2 p)
        {
            switch (drag)
            {
                case DragKind.SourceEdge:
                    SetSourceDragVisual(sourceDragStart, p);
                    if (sourceDragFromCorridor &&
                        PointNearSegment(p, sourcePoints[1], sourcePoints[2], HitRadius * 2.35f, out float pathT) &&
                        pathT >= Mathf.Min(.94f, sourceDragStartT + .18f))
                        sourceDragReachedTarget = true;
                    break;
                case DragKind.Hinge:
                    SetHingeVisual(p, true);
                    break;
                case DragKind.Arc:
                    Vector2 c = compassCenter == 0 ? TargetLeftLocal : TargetRightLocal;
                    tracedCurrentAngle = UnwrapAngle(tracedStartAngle, Mathf.Atan2(p.y - c.y, p.x - c.x));
                    SetArcPreview(compassCenter, selectedSourceEdge, tracedStartAngle, tracedCurrentAngle);
                    break;
                case DragKind.SegmentLeft:
                    segmentPreviewEnd = p;
                    SetSegmentPreview(0, TargetLeftLocal, p);
                    break;
                case DragKind.SegmentRight:
                    segmentPreviewEnd = p;
                    SetSegmentPreview(1, TargetRightLocal, p);
                    break;
            }
        }

        void EndDrag(Vector2 p, bool onBoard)
        {
            DragKind ended = drag;
            drag = DragKind.None;
            if (ended == DragKind.SourceEdge)
            {
                float endHit = phase == RunPhase.Practice ? HitRadius * 2.20f : HitRadius * 1.35f;
                int v = ClosestSourceVertex(p, endHit);
                bool corridorFinish = sourceDragFromCorridor && sourceDragReachedTarget &&
                    PointNearSegment(p, sourcePoints[1], sourcePoints[2], HitRadius * 2.35f, out float endT) &&
                    endT >= Mathf.Min(.94f, sourceDragStartT + .18f);
                HideSourceDragVisual();
                if (corridorFinish) v = 2;
                bool validPair = (v >= 0 && v != sourceDragStart) || corridorFinish;
                // 고정 연습은 안내문·시범 경로가 가리키는 B→C 한 변만 받는다. 다른 변을 받아 주면
                // 안내(「B에서 C까지」)와 실제 진행이 어긋나 다음 단계에서 이유 모를 오답이 났다.
                if (validPair && phase == RunPhase.Practice &&
                    NabiRules.EdgeBetween(sourceDragStart, v) != current.expectedRightEdge)
                    RefuseInput(MgfPointer.Position, "연습에서는 원본의 B에서 C까지 이어 BC 폭을 잡으시오.");
                else if (validPair)
                {
                    selectedSourceEdge = NabiRules.EdgeBetween(sourceDragStart, v);
                    compassCenter = -1;
                    practiceGuideStrong = false;
                    practiceFailedInputs = 0;
                    practiceNoProgressClock = 0f;
                    SetCompassWidthVisual(selectedSourceEdge);
                    ShowToast("컴퍼스 폭: " + current.EdgeName(selectedSourceEdge), false);
                    MgfSfx.Play("pop", .35f);
                    AdvancePracticeGuide();
                }
                else RefuseInput(MgfPointer.Position, "서로 다른 두 꼭짓점까지 이어 폭을 잡으시오.");
            }
            else if (ended == DragKind.Hinge)
            {
                int c = Vector2.Distance(p, TargetLeftLocal) <= HitRadius * 1.4f ? 0 :
                    Vector2.Distance(p, TargetRightLocal) <= HitRadius * 1.4f ? 1 : -1;
                if (c >= 0)
                {
                    compassCenter = c;
                    SetHingeVisual(c == 0 ? TargetLeftLocal : TargetRightLocal, false);
                    ShowToast("중심 " + (c == 0 ? current.LeftLabel : current.RightLabel) + " 고정", false);
                    MgfSfx.Play("pop", .33f);
                    AdvancePracticeGuide();
                }
                else
                {
                    SetHingeVisual(HingeHomeLocal, false);
                    RefuseInput(MgfPointer.Position, "중심 손잡이를 밑변의 검은 점 위에 놓으시오.");
                }
            }
            else if (ended == DragKind.Arc)
            {
                float sweep = Mathf.Abs(tracedCurrentAngle - tracedStartAngle);
                if (sweep >= MinArcSweep)
                {
                    arcPresent[compassCenter] = true;
                    arcProvided[compassCenter] = false;
                    arcEdge[compassCenter] = selectedSourceEdge;
                    arcStart[compassCenter] = tracedStartAngle;
                    arcEnd[compassCenter] = tracedCurrentAngle;
                    SetArcVisual(compassCenter, true, selectedSourceEdge, tracedStartAngle, tracedCurrentAngle, false);
                    ShowToast((compassCenter == 0 ? current.LeftLabel : current.RightLabel) + " 중심 원호 기록", false);
                    MgfSfx.Play("whoosh", .27f);
                    AdvancePracticeGuide();
                }
                else
                {
                    RestoreArcVisual(compassCenter);
                    RefuseInput(MgfPointer.Position, "연필 끝을 원둘레로 더 길게 움직이시오.");
                }
            }
            else if (ended == DragKind.SegmentLeft || ended == DragKind.SegmentRight)
            {
                int c = ended == DragKind.SegmentLeft ? 0 : 1;
                Vector2 start = c == 0 ? TargetLeftLocal : TargetRightLocal;
                if (onBoard && Vector2.Distance(start, p) >= MinSegmentLength)
                {
                    if (c == 0) { segmentLeft = true; segmentLeftEnd = p; }
                    else { segmentRight = true; segmentRightEnd = p; }
                    SetSegmentVisual(c, true, p);
                    MgfSfx.Play("whoosh", .22f);
                    AdvancePracticeGuide();
                    if (segmentLeft && segmentRight) SubmitConstruction();
                }
                else
                {
                    RestoreSegmentVisual(c);
                    RefuseInput(MgfPointer.Position, "밑변 끝점에서 교점까지 선을 끝까지 그으시오.");
                }
            }
            SyncConstructionState();
            MgfBridge.NotifyChanged();
        }

        static float UnwrapAngle(float start, float value)
        {
            while (value - start > Mathf.PI) value -= Mathf.PI * 2f;
            while (value - start < -Mathf.PI) value += Mathf.PI * 2f;
            return value;
        }

        static bool PointNearSegment(Vector2 p, Vector2 a, Vector2 b, float radius, out float t)
        {
            Vector2 ab = b - a;
            float denom = ab.sqrMagnitude;
            t = denom <= .0001f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / denom);
            return Vector2.Distance(p, a + ab * t) <= radius;
        }

        void AdvancePracticeGuide()
        {
            if (phase != RunPhase.Practice) return;
            idleClock = 0f;
            practiceNoProgressClock = 0f;
            ReplayGuide(false);
        }

        bool CandidateGeometryOkay(out Vector2 candidate)
        {
            candidate = Vector2.zero;
            if (!segmentLeft || !segmentRight || !arcPresent[0] || !arcPresent[1]) return false;
            float baseLen = Vector2.Distance(TargetLeftLocal, TargetRightLocal);
            float tolerance = baseLen * .02f;
            float leftRadius = EdgeWorldLength(arcEdge[0]);
            float rightRadius = EdgeWorldLength(arcEdge[1]);
            if (!CircleIntersections(leftRadius, rightRadius, out Vector2 first, out Vector2 second)) return false;
            if (CandidateMatchesActualMarks(first, leftRadius, rightRadius, tolerance)) { candidate = first; return true; }
            if (CandidateMatchesActualMarks(second, leftRadius, rightRadius, tolerance)) { candidate = second; return true; }
            return false;
        }

        bool CandidateMatchesActualMarks(Vector2 intersection, float leftRadius, float rightRadius, float tolerance)
        {
            // 두 자 선분은 평균점이 아니라 같은 수학적 교점 하나에 각각 닿아야 한다.
            if (Vector2.Distance(segmentLeftEnd, intersection) > tolerance) return false;
            if (Vector2.Distance(segmentRightEnd, intersection) > tolerance) return false;
            if (Vector2.Distance(segmentLeftEnd, segmentRightEnd) > tolerance * 2f) return false;

            // 각 선의 끝점도 자신이 대응하는 실제 원호 위에 있어야 한다.
            if (Mathf.Abs(Vector2.Distance(segmentLeftEnd, TargetLeftLocal) - leftRadius) > tolerance) return false;
            if (Mathf.Abs(Vector2.Distance(segmentRightEnd, TargetRightLocal) - rightRadius) > tolerance) return false;

            // 원 전체의 가상 교점이 아니라 학생이 실제로 그은 arcStart~arcEnd 구간이
            // 그 교점을 포함해야 한다. 길이 허용오차를 각도 허용오차로 환산한다.
            float leftAngleTolerance = Mathf.Asin(Mathf.Clamp(tolerance / Mathf.Max(.001f, leftRadius), 0f, .45f)) + .015f;
            float rightAngleTolerance = Mathf.Asin(Mathf.Clamp(tolerance / Mathf.Max(.001f, rightRadius), 0f, .45f)) + .015f;
            float leftAngle = Mathf.Atan2(intersection.y - TargetLeftLocal.y, intersection.x - TargetLeftLocal.x);
            float rightAngle = Mathf.Atan2(intersection.y - TargetRightLocal.y, intersection.x - TargetRightLocal.x);
            return AngleInsideRecordedSweep(leftAngle, arcStart[0], arcEnd[0], leftAngleTolerance) &&
                AngleInsideRecordedSweep(rightAngle, arcStart[1], arcEnd[1], rightAngleTolerance);
        }

        static bool AngleInsideRecordedSweep(float angle, float start, float end, float tolerance)
        {
            float unwrappedAngle = UnwrapAngle(start, angle);
            float lo = Mathf.Min(start, end) - tolerance;
            float hi = Mathf.Max(start, end) + tolerance;
            return unwrappedAngle >= lo && unwrappedAngle <= hi;
        }

        void SubmitConstruction()
        {
            if ((phase != RunPhase.Practice && phase != RunPhase.Playing) || current == null) return;
            bool geometry = CandidateGeometryOkay(out Vector2 candidate);
            bool sources = NabiRules.CorrectSources(current, arcEdge[0], arcEdge[1]);
            revealCorrect = geometry && sources;
            revealFirst = firstAttempt;
            pendingTerminal = false;

            if (phase == RunPhase.Practice)
            {
                if (revealCorrect) { st.score = 120; st.solved = 1; }
                else
                {
                    st.misconceptionId = NabiRules.MisconceptionId(current, arcEdge[0], arcEdge[1], segmentLeft && segmentRight, geometry);
                    firstAttempt = false;
                }
            }
            else
            {
                if (firstAttempt)
                {
                    st.firstAttempts++;
                    if (revealCorrect) st.firstCorrect++;
                }
                if (revealCorrect)
                {
                    int combo = Mathf.Max(1, st.firstCorrect - (st.firstAttempts - st.firstCorrect));
                    st.score += revealFirst ? 220 + combo * 25 : 90;
                    st.solved++;
                }
                else
                {
                    st.lives--;
                    st.attemptsOnCurrent++;
                    st.misconceptionId = NabiRules.MisconceptionId(current, arcEdge[0], arcEdge[1], segmentLeft && segmentRight, geometry);
                    firstAttempt = false;
                    int firstMisses = st.firstAttempts - st.firstCorrect;
                    pendingTerminal = st.lives <= 0 || firstMisses >= 2;
                }
            }

            phase = RunPhase.Reveal;
            st.phase = "reveal";
            revealClock = 0f;
            hitStop = revealCorrect ? .085f : 0f;
            BeginRevealVisual(revealCorrect, revealFirst, candidate);
            SetScreen();
            SyncConstructionState();
            MgfBridge.NotifyChanged();
        }

        void FinishReveal()
        {
            if (phase != RunPhase.Reveal) return;
            CompleteRevealVisual(revealCorrect);
            if (current.band == 0)
            {
                if (revealCorrect) BeginRunAfterPractice();
                else
                {
                    phase = RunPhase.Practice;
                    st.phase = "practice";
                    segmentLeft = segmentRight = false;
                    SetSegmentVisual(0, false, Vector2.zero);
                    SetSegmentVisual(1, false, Vector2.zero);
                    RefreshProblemUi();
                    ReplayGuide(true);
                    SetScreen();
                }
            }
            else if (!revealCorrect)
            {
                if (pendingTerminal) EndRun(st.lives <= 0 ? "thread" : "mastery");
                else
                {
                    phase = RunPhase.Playing;
                    st.phase = "playing";
                    segmentLeft = segmentRight = false;
                    SetSegmentVisual(0, false, Vector2.zero);
                    SetSegmentVisual(1, false, Vector2.zero);
                    RefreshProblemUi();
                    SetScreen();
                }
            }
            else if (st.solved >= NabiRules.TargetWings)
            {
                EndRun(st.firstCorrect >= NabiRules.NeededFirstCorrect ? "clear" : "mastery");
            }
            else
            {
                phase = RunPhase.Playing;
                st.phase = "playing";
                NextProblem();
                SetScreen();
            }
            MgfBridge.NotifyChanged();
        }

        void EndRun(string reason)
        {
            if (phase == RunPhase.End) return;
            endReason = reason;
            phase = RunPhase.End;
            st.phase = reason == "clear" ? "clear" : "gameover";
            st.timeLeft = Mathf.Max(0, Mathf.CeilToInt(secondsLeft));
            ShowEndVisual(reason);
            SetScreen();
            MgfSfx.Play(reason == "clear" ? "win" : "lose", .5f);
            MgfBridge.NotifyChanged();
        }

        int ClosestSourceVertex(Vector2 p, float radius)
        {
            int best = -1;
            float dBest = radius;
            for (int i = 0; i < 3; i++)
            {
                float d = Vector2.Distance(p, SourcePointLocal(i));
                if (d <= dBest) { best = i; dBest = d; }
            }
            return best;
        }

        void ShowHelp()
        {
            phase = RunPhase.Help;
            st.phase = "title";
            ShowHelpVisual(true);
            MgfBridge.NotifyChanged();
        }

        void HideHelpAndStart()
        {
            ShowHelpVisual(false);
            StartPractice();
        }

        public void TestStart()
        {
            StartRunDirect();
        }

        public void TestAnswerCorrect()
        {
            if (phase == RunPhase.Title || phase == RunPhase.Help || phase == RunPhase.End) StartRunDirect();
            if (phase == RunPhase.Reveal) { revealClock = 99f; FinishReveal(); }
            if (phase != RunPhase.Playing && phase != RunPhase.Practice) StartRunDirect();
            selectedSourceEdge = current.expectedLeftEdge;
            compassCenter = 0;
            arcPresent[0] = arcPresent[1] = true;
            arcEdge[0] = current.expectedLeftEdge;
            arcEdge[1] = current.expectedRightEdge;
            arcStart[0] = arcStart[1] = .05f;
            arcEnd[0] = arcEnd[1] = Mathf.PI - .05f;
            Vector2 a, b;
            CircleIntersections(EdgeWorldLength(arcEdge[0]), EdgeWorldLength(arcEdge[1]), out a, out b);
            Vector2 target = a.y >= b.y ? a : b;
            segmentLeft = segmentRight = true;
            segmentLeftEnd = segmentRightEnd = target;
            SetArcVisual(0, true, arcEdge[0], arcStart[0], arcEnd[0], false);
            SetArcVisual(1, true, arcEdge[1], arcStart[1], arcEnd[1], false);
            SetSegmentVisual(0, true, target);
            SetSegmentVisual(1, true, target);
            SubmitConstruction();
        }

        public void TestAnswerWrong()
        {
            if (phase == RunPhase.Title || phase == RunPhase.Help || phase == RunPhase.End) StartRunDirect();
            if (phase == RunPhase.Reveal) { revealClock = 99f; FinishReveal(); }
            if (phase != RunPhase.Playing && phase != RunPhase.Practice) StartRunDirect();
            selectedSourceEdge = current.expectedLeftEdge;
            arcPresent[0] = arcPresent[1] = true;
            arcEdge[0] = current.expectedRightEdge;
            arcEdge[1] = current.expectedLeftEdge;
            arcStart[0] = arcStart[1] = .05f;
            arcEnd[0] = arcEnd[1] = Mathf.PI - .05f;
            Vector2 a, b;
            CircleIntersections(EdgeWorldLength(arcEdge[0]), EdgeWorldLength(arcEdge[1]), out a, out b);
            Vector2 target = a.y >= b.y ? a : b;
            segmentLeft = segmentRight = true;
            segmentLeftEnd = segmentRightEnd = target;
            SetArcVisual(0, true, arcEdge[0], arcStart[0], arcEnd[0], false);
            SetArcVisual(1, true, arcEdge[1], arcStart[1], arcEnd[1], false);
            SetSegmentVisual(0, true, target);
            SetSegmentVisual(1, true, target);
            SubmitConstruction();
        }

        public string StateJson()
        {
            SyncConstructionState();
            UpdateStateScreenCoordinates();
            return JsonUtility.ToJson(st);
        }

        public string ProblemBankJson()
        {
            return MgfJson.Bank(NabiRules.ToMgfBank(problemBank));
        }
    }
}
