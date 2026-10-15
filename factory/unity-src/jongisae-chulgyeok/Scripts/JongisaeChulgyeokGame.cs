// 종이새 출격 — 타이틀, 연습, 2+3+3 실전, 오답 수리와 브리지 계약.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using TMPro;
using UnityEngine;

namespace Mgf.JongisaeChulgyeok
{
    public partial class JongisaeChulgyeokGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int remaining;
            public string problemId = "";
            public int band;
            public int inputIndex;
            public int inputAngle;
            public bool firstAttemptLocked;
            public bool firstCorrect;
            public int lastBandFirstCorrect;
            public bool repairPending;
            public bool dragging;
            public int firstAttemptTotal;
            public int firstAttemptCorrect;
            public int exteriorFirstCorrect;
            public int combo;
            public int launches;
            public int pointerVersion;
            public string subphase = "title";
            public string misconceptionId = "";
            public string endReason = "";
            public float pivotPxX;
            public float pivotPxY;
            public float handlePxX;
            public float handlePxY;
        }

        enum GamePhase { Title, Practice, Playing, Reveal, Bridge, End }

        readonly State st = new State();
        readonly List<MgfProblem> bank = new List<MgfProblem>();
        List<WingProblem> deck;
        WingProblem current;
        GamePhase gamePhase = GamePhase.Title;
        int deckIndex;
        int runSerial;
        int lastBridgeSolved = -1;
        bool firstAttempt;
        bool revealCorrect;
        bool revealWasPractice;
        bool revealWasFirst;
        bool dragMoved;
        int downInputIndex;
        Vector2 downPointer;
        float timeLeft;
        float revealClock;
        float bridgeClock;
        float idleClock;
        float hitStopLeft;
        float nextBellAt;
        int bellStep;
        int shownSecond = -1;
        int practiceProofStage = -1;

        void Awake()
        {
            MgfLook.Quality(34f);
            if (JongisaeRules.AuditMath() != 0) Debug.LogError("[종이새 출격] 생성기 전수 검산 실패");
            bank.AddRange(JongisaeRules.BuildBank());
            BuildWorld();
            BuildUi();
            Prewarm();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb = new StringBuilder();
            sb.Append("종이새출격날개를펼쳐날려라새를잡아시작중학교2학년삼각형의성질");
            sb.Append("남은시간클립첫출격외각출격완료각관계재연습꼭지각밑각이등변삼각형");
            sb.Append("길이가같은두변사이가꼭지각두밑각이같다현재목표날개끝을잡아원호를따라끌고놓으시오");
            sb.Append("같은새를다시펼치시오바깥각의보각이밑각이다회전된그림에서는표식을먼저보시오");
            sb.Append("분석완료다시출격풍경연주대열완성시간종료클립소진숙련기준미달");
            sb.Append("ABCDBCADMSAS합동공통대응각각이등분선따라서여기서잡기바람문통과");
            MgfText.Prewarm(sb.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(0.05f, Time.deltaTime);
            ApplyResponsiveLayout();
            HandleInput();

            if (hitStopLeft > 0f) hitStopLeft -= dt;
            else if (gamePhase == GamePhase.Practice)
            {
                idleClock += dt;
                if (idleClock >= 8f) { idleClock = 0f; ReplayPracticeGuide(true); }
            }
            else if (gamePhase == GamePhase.Playing)
            {
                timeLeft = Mathf.Max(0f, timeLeft - dt);
                int sec = Mathf.CeilToInt(timeLeft);
                if (sec != shownSecond) { shownSecond = sec; RefreshHud(); }
                if (timeLeft <= 0f) EndRun("time");
                idleClock += dt;
                if (idleClock >= 8f) { idleClock = 0f; PulseHandle(false); }
            }
            else if (gamePhase == GamePhase.Reveal)
            {
                revealClock += dt;
                if (revealCorrect && bellStep < 3 && revealClock >= nextBellAt)
                {
                    RingBell(bellStep, st.combo);
                    bellStep++;
                    nextBellAt += 0.18f;
                }
                if (revealWasPractice && revealCorrect) UpdatePracticeProof(revealClock, current);
                float duration = revealWasPractice && revealCorrect ? 2.35f : (revealWasPractice ? 1.42f : (revealCorrect ? 1.18f : 1.05f));
                if (revealClock >= duration) FinishReveal();
            }
            else if (gamePhase == GamePhase.Bridge)
            {
                bridgeClock += dt;
                if (bridgeClock >= 1.35f) { gamePhase = GamePhase.Playing; LoadNextProblem(); }
            }

            AnimateWorld(dt);
            UpdateAnimatedUi(dt);
            UpdatePointerState();
        }

        void ShowTitle()
        {
            gamePhase = GamePhase.Title;
            current = null;
            deck = null;
            deckIndex = 0;
            firstAttempt = true;
            st.score = 0;
            st.lives = JongisaeRules.StartLives;
            st.level = 1;
            st.solved = 0;
            st.remaining = (int)JongisaeRules.RunSeconds;
            st.problemId = "";
            st.band = 0;
            st.inputIndex = 10;
            st.inputAngle = 40;
            st.firstAttemptLocked = false;
            st.firstCorrect = false;
            st.lastBandFirstCorrect = 0;
            st.repairPending = false;
            st.dragging = false;
            st.firstAttemptTotal = 0;
            st.firstAttemptCorrect = 0;
            st.exteriorFirstCorrect = 0;
            st.combo = 0;
            st.launches = 0;
            st.subphase = "title";
            st.misconceptionId = "";
            st.endReason = "";
            st.phase = "title";
            timeLeft = JongisaeRules.RunSeconds;
            shownSecond = -1;
            ResetWorldForRun();
            ShowTitleVisual();
            RefreshAllUi();
            MgfBridge.NotifyChanged();
        }

        void StartPractice()
        {
            ResetRunState();
            gamePhase = GamePhase.Practice;
            current = JongisaeRules.Practice();
            firstAttempt = true;
            st.subphase = "practice";
            st.phase = "playing";
            BindProblem(true);
            ShowPlayVisual();
            ReplayPracticeGuide(false);
            MgfSfx.Play("whoosh", 0.35f);
            MgfBridge.NotifyChanged();
        }

        void StartRunDirect()
        {
            ResetRunState();
            gamePhase = GamePhase.Playing;
            st.subphase = "playing";
            st.phase = "playing";
            ShowPlayVisual();
            LoadNextProblem();
            MgfBridge.NotifyChanged();
        }

        void ResetRunState()
        {
            runSerial++;
            deck = JongisaeRules.RunDeck(unchecked(Environment.TickCount ^ runSerial * 104729));
            deckIndex = 0;
            lastBridgeSolved = -1;
            timeLeft = JongisaeRules.RunSeconds;
            shownSecond = -1;
            st.score = 0;
            st.lives = JongisaeRules.StartLives;
            st.level = 1;
            st.solved = 0;
            st.remaining = (int)JongisaeRules.RunSeconds;
            st.problemId = "";
            st.band = 0;
            st.firstAttemptLocked = false;
            st.firstCorrect = false;
            st.lastBandFirstCorrect = 0;
            st.repairPending = false;
            st.dragging = false;
            st.firstAttemptTotal = 0;
            st.firstAttemptCorrect = 0;
            st.exteriorFirstCorrect = 0;
            st.combo = 0;
            st.launches = 0;
            st.misconceptionId = "";
            st.endReason = "";
            st.phase = "playing";
            idleClock = 0f;
            ResetWorldForRun();
        }

        void BeginMainAfterPractice()
        {
            // 고정 연습은 점수·클립·첫 시도 통계에 포함하지 않는다.
            st.score = 0;
            st.lives = JongisaeRules.StartLives;
            st.solved = 0;
            st.firstAttemptTotal = 0;
            st.firstAttemptCorrect = 0;
            st.exteriorFirstCorrect = 0;
            st.combo = 0;
            st.launches = 0;
            st.repairPending = false;
            st.firstAttemptLocked = false;
            st.firstCorrect = false;
            gamePhase = GamePhase.Playing;
            st.subphase = "playing";
            LoadNextProblem();
        }

        void LoadNextProblem()
        {
            if (st.solved >= JongisaeRules.RunBirds) { EndRun(MasteryMet() ? "clear" : "mastery"); return; }
            // 정답 연출에서 직접 넘어올 때 내부 enum도 반드시 복귀시킨다. 공개 상태만
            // playing이면 Reveal의 FinishReveal이 매 프레임 재호출되어 덱이 건너뛰어진다.
            gamePhase = GamePhase.Playing;
            if (deck == null || deck.Count != JongisaeRules.RunBirds) deck = JongisaeRules.RunDeck(20261016 + runSerial);
            current = deck[deckIndex++ % deck.Count];
            firstAttempt = true;
            st.firstAttemptLocked = false;
            st.firstCorrect = false;
            st.repairPending = false;
            st.misconceptionId = "";
            st.subphase = "playing";
            st.phase = "playing";
            BindProblem(false);
            ShowPlayVisual();
            MgfBridge.NotifyChanged();
        }

        void BindProblem(bool practice)
        {
            st.problemId = current.id;
            st.band = current.band;
            st.level = Math.Max(1, current.band);
            SetInputIndex(current.startIndex, true);
            st.remaining = Mathf.CeilToInt(timeLeft);
            idleClock = 0f;
            ApplyProblemVisual(current, practice);
            RefreshAllUi();
        }

        void BeginBridge(int nextBand)
        {
            gamePhase = GamePhase.Bridge;
            bridgeClock = 0f;
            st.subphase = "bridge";
            st.remaining = Mathf.CeilToInt(timeLeft);
            ShowBridge(nextBand);
            MgfBridge.NotifyChanged();
        }

        void SetInputIndex(int index, bool force)
        {
            index = Mathf.Clamp(index, 0, JongisaeRules.TickCount - 1);
            if (!force && index == st.inputIndex) return;
            st.inputIndex = index;
            st.inputAngle = JongisaeRules.AngleFromIndex(index);
            UpdateWingGeometry(st.inputAngle);
            RefreshAngleUi();
        }

        void JudgeSubmission()
        {
            if ((gamePhase != GamePhase.Practice && gamePhase != GamePhase.Playing) || current == null) return;
            bool practice = gamePhase == GamePhase.Practice;
            revealCorrect = JongisaeRules.IsCorrect(current, st.inputIndex);
            revealWasPractice = practice;
            revealWasFirst = firstAttempt;

            if (!practice)
            {
                if (firstAttempt)
                {
                    st.firstAttemptLocked = true;
                    st.firstAttemptTotal++;
                    st.firstCorrect = revealCorrect;
                    if (revealCorrect)
                    {
                        st.firstAttemptCorrect++;
                        if (current.band == 3) st.exteriorFirstCorrect++;
                    }
                    st.lastBandFirstCorrect = current.band == 3 ? st.exteriorFirstCorrect : st.firstAttemptCorrect;
                }

                if (revealCorrect)
                {
                    st.combo = revealWasFirst ? st.combo + 1 : 0;
                    st.score += 120 + current.band * 20 + (revealWasFirst ? st.combo * 15 : 0);
                    st.solved++;
                    st.launches++;
                    st.repairPending = false;
                    st.misconceptionId = "";
                }
                else
                {
                    st.combo = 0;
                    st.lives--;
                    st.repairPending = true;
                    st.misconceptionId = JongisaeRules.MisconceptionId(current, st.inputIndex);
                    firstAttempt = false;
                }
            }
            else if (!revealCorrect)
            {
                st.misconceptionId = JongisaeRules.MisconceptionId(current, st.inputIndex);
                firstAttempt = false;
            }

            st.dragging = false;
            st.remaining = Mathf.CeilToInt(timeLeft);
            gamePhase = GamePhase.Reveal;
            st.subphase = revealCorrect ? "launch" : "repair_reveal";
            revealClock = 0f;
            practiceProofStage = -1;
            hitStopLeft = revealCorrect ? 0.085f : 0.06f;
            bellStep = 0;
            nextBellAt = 0.28f;
            BeginRevealVisual(revealCorrect, revealWasFirst, current);
            RefreshAllUi();
            MgfBridge.NotifyChanged();
        }

        void FinishReveal()
        {
            if (gamePhase != GamePhase.Reveal) return;
            FinishRevealVisual(revealCorrect);
            if (revealWasPractice)
            {
                if (revealCorrect) BeginMainAfterPractice();
                else
                {
                    gamePhase = GamePhase.Practice;
                    st.subphase = "practice";
                    ShowWrongReason(current, st.misconceptionId);
                    ReplayPracticeGuide(true);
                }
            }
            else if (!revealCorrect)
            {
                if (st.lives <= 0) EndRun("clips");
                else
                {
                    gamePhase = GamePhase.Playing;
                    st.subphase = "repair";
                    ShowWrongReason(current, st.misconceptionId);
                    PulseHandle(false);
                }
            }
            else if (st.solved >= JongisaeRules.RunBirds)
            {
                EndRun(MasteryMet() ? "clear" : "mastery");
            }
            else if ((st.solved == 2 || st.solved == 5) && lastBridgeSolved != st.solved)
            {
                lastBridgeSolved = st.solved;
                BeginBridge(st.solved == 2 ? 2 : 3);
            }
            else LoadNextProblem();
            RefreshAllUi();
            MgfBridge.NotifyChanged();
        }

        bool MasteryMet()
        {
            return st.solved == JongisaeRules.RunBirds
                && st.firstAttemptCorrect >= JongisaeRules.RequiredFirst
                && st.exteriorFirstCorrect >= JongisaeRules.RequiredExteriorFirst;
        }

        void EndRun(string reason)
        {
            if (gamePhase == GamePhase.End) return;
            gamePhase = GamePhase.End;
            st.endReason = reason;
            st.subphase = "result";
            st.dragging = false;
            st.repairPending = false;
            st.remaining = Mathf.CeilToInt(timeLeft);
            bool clear = reason == "clear";
            st.phase = clear ? "clear" : "gameover";
            ShowResultVisual(clear, reason);
            MgfSfx.Play(clear ? "win" : "lose", 0.5f);
            RefreshAllUi();
            MgfBridge.NotifyChanged();
        }

        void HandleInput()
        {
            if (gamePhase == GamePhase.Title)
            {
                if (MgfPointer.Down)
                {
                    st.pointerVersion++;
                    if (HitTitleBird()) StartPractice();
                    else RefuseInput("가장 가까운 종이새의 날개를 잡으시오", MgfPointer.Position);
                }
                return;
            }
            if (gamePhase == GamePhase.End)
            {
                if (MgfPointer.Down)
                {
                    st.pointerVersion++;
                    if (HitRestartBird()) StartPractice();
                    else RefuseInput("접힌 새를 눌러 다시 출격하시오", MgfPointer.Position);
                }
                return;
            }
            if (gamePhase == GamePhase.Reveal || gamePhase == GamePhase.Bridge)
            {
                if (MgfPointer.Down)
                {
                    st.pointerVersion++;
                    RefuseInput(gamePhase == GamePhase.Reveal ? "이 새가 날아가는 중이다" : "다음 가지가 열리는 중이다", MgfPointer.Position);
                }
                return;
            }

            if (Input.GetKeyDown(KeyCode.Tab)) { st.pointerVersion++; BeginKeyboardGrab(); }
            if (st.dragging && (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow)))
            {
                SetInputIndex(st.inputIndex + (Input.GetKeyDown(KeyCode.RightArrow) ? 1 : -1), false);
                dragMoved = true;
                MgfSfx.Play("tap", 0.12f);
                MgfBridge.NotifyChanged();
            }
            if (st.dragging && Input.GetKeyDown(KeyCode.Space)) EndKeyboardGrab();

            if (MgfPointer.Down)
            {
                st.pointerVersion++;
                if (HitHandle() || (gamePhase == GamePhase.Practice && InPracticeSwipeZone(MgfPointer.Position))) BeginDrag();
                else
                {
                    RefuseInput("빛나는 C에서 잡아 화살표를 따라 끄시오", MgfPointer.Position);
                    if (gamePhase == GamePhase.Practice) ReplayPracticeGuide(true);
                }
            }
            if (st.dragging && MgfPointer.Held) DragTo(MgfPointer.Position);
            if (st.dragging && MgfPointer.Up) EndDrag();
        }

        bool HitHandle()
        {
            if (MgfPointer.DownHit(cam, out RaycastHit hit)
                && handle && (hit.collider.gameObject == handle || hit.collider.transform.IsChildOf(handle.transform))) return true;
            // 잎·가지가 깊이 방향으로 겹치는 2.5D 장면에서는 배경 콜라이더가 레이를 먼저
            // 가로챌 수 있다. 화면에 보이는 C의 중심을 기준으로 72px 터치 영역을 보장한다.
            // 좌표 판정은 손잡이 잡기일 뿐이며, 놓은 각의 정답 판정은 여전히 정수 눈금 완전 일치다.
            if (!handle || !cam) return false;
            Vector3 hp = cam.WorldToScreenPoint(handle.transform.position);
            float radius = gamePhase == GamePhase.Practice ? 112f : 78f;
            return hp.z > 0f && Vector2.Distance(MgfPointer.Position, new Vector2(hp.x, hp.y)) <= radius;
        }

        bool InPracticeSwipeZone(Vector2 screen)
        {
            return screen.y >= Screen.height * 0.12f;
        }

        bool HitTitleBird()
        {
            if (!MgfPointer.DownHit(cam, out RaycastHit hit)) return false;
            return titleBird && (hit.collider.gameObject == titleBird || hit.collider.transform.IsChildOf(titleBird.transform));
        }

        bool HitRestartBird()
        {
            if (!MgfPointer.DownHit(cam, out RaycastHit hit)) return false;
            return restartBird && (hit.collider.gameObject == restartBird || hit.collider.transform.IsChildOf(restartBird.transform));
        }

        void BeginDrag()
        {
            downPointer = MgfPointer.Position;
            downInputIndex = st.inputIndex;
            dragMoved = false;
            st.dragging = true;
            st.remaining = Mathf.CeilToInt(timeLeft);
            idleClock = 0f;
            SetGrabbed(true);
            HidePracticeGuideWhileDragging();
            MgfSfx.Play("tap", 0.18f);
            MgfBridge.NotifyChanged();
        }

        void DragTo(Vector2 screen)
        {
            if (gamePhase == GamePhase.Practice && current != null && Vector2.Distance(screen, downPointer) >= 12f)
            {
                dragMoved = true;
                SetInputIndex(current.qIndex, false);
                MgfBridge.NotifyChanged();
                return;
            }
            var ray = cam.ScreenPointToRay(screen);
            var plane = new Plane(mathRoot.forward, mathRoot.position);
            if (!plane.Raycast(ray, out float d)) return;
            Vector3 local = mathRoot.InverseTransformPoint(ray.GetPoint(d));
            Vector2 rel = new Vector2(local.x - PivotLocal.x, local.y - PivotLocal.y);
            if (rel.sqrMagnitude < 0.05f) return;
            float raw = Mathf.Atan2(rel.y, rel.x) * Mathf.Rad2Deg;
            int angle = Mathf.Clamp(Mathf.RoundToInt((raw - JongisaeRules.MinAngle) / JongisaeRules.Step) * JongisaeRules.Step + JongisaeRules.MinAngle,
                JongisaeRules.MinAngle, JongisaeRules.MaxAngle);
            int index = JongisaeRules.IndexFromAngle(angle);
            if (index != st.inputIndex)
            {
                SetInputIndex(index, false);
                dragMoved = dragMoved || Vector2.Distance(screen, downPointer) >= 12f;
                MgfSfx.Play("tap", 0.035f);
                MgfBridge.NotifyChanged();
            }
        }

        void EndDrag()
        {
            st.dragging = false;
            SetGrabbed(false);
            bool valid = dragMoved && st.inputIndex != downInputIndex;
            if (valid)
            {
                // 고정 연습만 화살표 끝(70°)에 고정한다. 실전 8문항은 놓은 2° 눈금을 완전 일치 판정한다.
                if (gamePhase == GamePhase.Practice && current != null) SetInputIndex(current.qIndex, true);
                JudgeSubmission();
            }
            else
            {
                RefuseInput("날개 끝을 다른 눈금까지 끌어 놓으시오", MgfPointer.Position);
                ReplayPracticeGuide(gamePhase == GamePhase.Practice);
                MgfBridge.NotifyChanged();
            }
        }

        void BeginKeyboardGrab()
        {
            st.dragging = true;
            downInputIndex = st.inputIndex;
            dragMoved = false;
            SetGrabbed(true);
            ShowToast("← → 로 날개를 돌리고 Space로 놓으시오", 1.6f);
            MgfBridge.NotifyChanged();
        }

        void EndKeyboardGrab()
        {
            st.dragging = false;
            SetGrabbed(false);
            if (dragMoved && st.inputIndex != downInputIndex) JudgeSubmission();
            else RefuseInput("먼저 ← 또는 → 로 다른 눈금까지 움직이시오", MgfPointer.Position);
        }

        void UpdatePointerState()
        {
            if (!cam || !mathRoot || !handle || !mathRoot.gameObject.activeInHierarchy) return;
            Vector3 pivot = mathRoot.TransformPoint(PivotLocal);
            Vector3 hp = handle.transform.position;
            Vector3 ps = cam.WorldToScreenPoint(pivot);
            Vector3 hs = cam.WorldToScreenPoint(hp);
            st.pivotPxX = ps.x;
            st.pivotPxY = Screen.height - ps.y;
            st.handlePxX = hs.x;
            st.handlePxY = Screen.height - hs.y;
        }

        // ── IMgfGame. 테스트 명령도 실제 손놓기 판정 JudgeSubmission을 그대로 탄다.
        public void TestStart() => StartRunDirect();

        public void TestAnswerCorrect()
        {
            if (gamePhase != GamePhase.Playing || current == null) StartRunDirect();
            SetInputIndex(current.qIndex, true);
            JudgeSubmission();
        }

        public void TestAnswerWrong()
        {
            if (gamePhase != GamePhase.Playing || current == null) StartRunDirect();
            int miss = (current.qIndex + 17) % JongisaeRules.TickCount;
            if (miss == current.qIndex) miss = (miss + 1) % JongisaeRules.TickCount;
            SetInputIndex(miss, true);
            JudgeSubmission();
        }

        public string StateJson() => JsonUtility.ToJson(st);
        public string ProblemBankJson() => MgfJson.Bank(bank);
    }
}
