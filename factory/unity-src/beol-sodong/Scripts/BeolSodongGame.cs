using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.BeolSodong
{
    public sealed partial class BeolSodongGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int round;
            public int firstAttemptCorrect;
            public int firstAttemptTotal;
            public int submissionCount;
            public int pointerVersion;
            public int selectedMask;
            public int selectedCount;
            public int attemptIndex;
            public string selectedIds = "";
            public string problemId = "";
            public string band = "";
            public string lastVerdict = "";
            public string misconceptionId = "";
            public string endReason = "";
            public bool onboarding;
            public bool firstAttemptResolved;
            public bool frozen;
            public int[] values = new int[0];
            public float[] beeX = new float[9];
            public float[] beeY = new float[9];
        }

        enum Flow { Title, Transition, Practice, Playing, Bridge, Reveal, End }

        readonly State st = new State();
        readonly List<SwarmProblem> problems = new List<SwarmProblem>(360);
        readonly List<MgfProblem> bank = new List<MgfProblem>(360);
        readonly int[] selectionOrder = new int[9];
        List<SwarmProblem> deck;
        SwarmProblem current;
        Flow flow = Flow.Title;
        int runSeed = 28411;
        int deckIndex;
        int selectedMask;
        int selectionCount;
        bool drawing;
        bool titlePressed;
        bool firstAttempt;
        bool revealCorrect;
        bool revealPractice;
        bool revealBridge;
        bool pendingMasteryFail;
        bool bridgeShown;
        float timeLeft;
        float transitionT;
        float revealT;
        float idleT;
        float endT;
        float strokeDistance;
        float hitStop;
        Vector2 pointerLast;

        const float TransitionSeconds = .52f;
        const float CorrectRevealSeconds = 1.75f;
        const float WrongRevealSeconds = 1.65f;

        void Awake()
        {
            // CreatePrimitive가 런타임에 붙이는 콜라이더 타입을 IL2CPP 스트리핑에서 보존한다.
            var keep = new GameObject("Primitive collider type keepalive");
            keep.AddComponent<SphereCollider>(); keep.AddComponent<CapsuleCollider>();
            keep.AddComponent<BoxCollider>(); keep.AddComponent<MeshCollider>();
            Destroy(keep);
            MgfLook.Quality(42f);
            problems.AddRange(BeolRules.BuildBank());
            var audit = new StringBuilder();
            int errors = BeolRules.AuditBank(problems, audit);
            if (errors != 0) Debug.LogWarning("[벌 소동] 문제 은행 검산 경고 " + errors + "\n" + audit);
            for (int i = 0; i < problems.Count; i++) bank.Add(problems[i].ToMgf());
            Prewarm();
            BuildWorld();
            BuildUi();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb = new StringBuilder("벌소동벌들을선으로이어라중학교1학년자료의정리와해석꽃방문횟수최빈값가장많이나타난값여러개이면모두");
            sb.Append("은염유리건판황동선석회암유채꽃시계흑단관절벌상아도자숫자표촬영현상첫제출통과예비남은시간최고기록");
            sb.Append("숫자표에서시작해최빈값의벌을빠짐없이한획으로잇고손을떼시오빈통로로우회하시오다시현상하기사진완성실전시작");
            sb.Append("가장큰값이아니라가장많이나타난값나타난횟수도수공동최빈값두무리를모두이으시오연습완료건판이갈라졌다");
            sb.Append("0123456789회초장회점점수게임오버숙련달성목표칠아홉원자료정답근거선택누락포함취소홈다시시작");
            MgfText.Prewarm(sb.ToString());
        }

        void Update()
        {
            float raw = Mathf.Min(.05f, Time.deltaTime);
            float dt = raw;
            if (hitStop > 0f) { hitStop -= raw; dt = 0f; }
            HandlePointer();

            if (flow == Flow.Transition)
            {
                transitionT += dt;
                if (transitionT >= TransitionSeconds) StartPractice();
            }
            else if (flow == Flow.Practice || flow == Flow.Bridge)
            {
                idleT += dt;
                if (idleT >= 8f) { idleT = 0f; ReplayGuide(true); }
            }
            else if (flow == Flow.Playing)
            {
                timeLeft = Mathf.Max(0f, timeLeft - dt);
                idleT += dt;
                if (timeLeft <= 0f) EndRun(false, "time");
                else if (idleT >= 11f) { idleT = 0f; ShowToast("값의 크기가 아니라 나타난 횟수를 비교하시오.", 2.2f); ReplayGuide(false); }
            }
            else if (flow == Flow.Reveal)
            {
                revealT += dt;
                UpdateRevealVisual(revealT, revealCorrect);
                if (revealT >= (revealCorrect ? CorrectRevealSeconds : WrongRevealSeconds)) FinishReveal();
            }
            else if (flow == Flow.End) endT += raw;

            UpdateWorld(dt, raw);
            UpdateUi(raw);
            ApplyResponsiveLayout();
        }

        void ShowTitle()
        {
            flow = Flow.Title; current = null; deck = null; drawing = false; titlePressed = false; endT = 0f;
            ResetCounters(); ResetRunVisual(); ShowTitleVisual(); SetScreen(); MgfBridge.NotifyChanged();
        }

        void ResetCounters()
        {
            st.score = 0; st.lives = BeolRules.StartLives; st.level = 1; st.phase = "title"; st.solved = 0;
            st.round = 0; st.firstAttemptCorrect = 0; st.firstAttemptTotal = 0; st.submissionCount = 0;
            st.pointerVersion = 0; st.selectedMask = 0; st.selectedCount = 0; st.attemptIndex = 0;
            st.selectedIds = ""; st.problemId = ""; st.band = ""; st.lastVerdict = ""; st.misconceptionId = ""; st.endReason = "";
            st.onboarding = false; st.firstAttemptResolved = false; st.frozen = true;
            selectedMask = 0; selectionCount = 0; pendingMasteryFail = false;
        }

        void BeginTitleTransition()
        {
            if (flow != Flow.Title) return;
            flow = Flow.Transition; transitionT = 0f; titlePressed = false; st.phase = "playing"; st.frozen = true;
            StartClockOpening(); SetScreen(); MgfSfx.Play("whoosh", .42f); MgfBridge.NotifyChanged();
        }

        void PrepareRun()
        {
            runSeed = unchecked(runSeed * 1103515245 + 12345);
            deck = BeolRules.RunDeck(problems, runSeed & 0x7fffffff);
            deckIndex = 0; timeLeft = BeolRules.RunSeconds; bridgeShown = false;
            ResetCounters(); st.phase = "playing"; ResetRunVisual();
        }

        void StartPractice()
        {
            PrepareRun(); flow = Flow.Practice; current = BeolRules.Practice(); st.onboarding = true; st.frozen = true;
            firstAttempt = true; ResetForProblem(true); SetScreen(); ReplayGuide(false); MgfBridge.NotifyChanged();
        }

        void StartRunDirect()
        {
            PrepareRun(); flow = Flow.Playing; st.onboarding = false; st.frozen = false;
            LoadNextProblem(); SetScreen(); MgfBridge.NotifyChanged();
        }

        void BeginMainAfterPractice()
        {
            // 연습은 점수·첫 제출 분모·시간에 포함하지 않는다.
            st.score = 0; st.lives = BeolRules.StartLives; st.solved = 0; st.firstAttemptCorrect = 0; st.firstAttemptTotal = 0;
            st.submissionCount = 0; st.attemptIndex = 0; deckIndex = 0; timeLeft = BeolRules.RunSeconds;
            st.onboarding = false; st.frozen = false; flow = Flow.Playing; ClearPlateRing(); LoadNextProblem(); SetScreen();
        }

        void LoadNextProblem()
        {
            if (st.solved >= BeolRules.Goal)
            {
                EndRun(st.firstAttemptCorrect >= BeolRules.MasteryFirst, st.firstAttemptCorrect >= BeolRules.MasteryFirst ? "clear" : "mastery");
                return;
            }
            if (st.solved == 6 && !bridgeShown)
            {
                bridgeShown = true; flow = Flow.Bridge; current = BeolRules.Bridge(); st.frozen = true; firstAttempt = true;
                ResetForProblem(true); SetScreen(); ReplayGuide(false); MgfBridge.NotifyChanged(); return;
            }
            flow = Flow.Playing; st.frozen = false; current = deck[deckIndex++]; firstAttempt = true;
            st.round = st.solved + 1; st.level = current.band; st.band = current.band == 1 ? "홀로 최빈값" : current.band == 2 ? "최댓값과 구별" : "공동 최빈값";
            st.firstAttemptResolved = false; st.attemptIndex = 0; ResetForProblem(true); SetScreen(); MgfBridge.NotifyChanged();
        }

        void ResetForProblem(bool enter)
        {
            drawing = false; strokeDistance = 0f; idleT = 0f; selectedMask = 0; selectionCount = 0;
            st.selectedMask = 0; st.selectedCount = 0; st.selectedIds = ""; st.problemId = current.id;
            st.lastVerdict = ""; st.misconceptionId = "";
            st.values = (int[])current.values.Clone();
            ResetStrokeVisual(); ShowProblem(current, enter);
        }

        void HandlePointer()
        {
            if (MgfPointer.Down)
            {
                st.pointerVersion++; idleT = 0f; pointerLast = MgfPointer.Position; strokeDistance = 0f;
                SpawnPointerRipple(pointerLast); MgfBridge.NotifyChanged();
                if (flow == Flow.Title)
                {
                    // 타이틀의 파사드 전체가 하나의 살아 있는 시작 사물이다. 첫 탭이 빈 공간에
                    // 닿아도 건판 걸기 사건으로 이어져 첫 10초 훅을 막는 죽은 탭을 만들지 않는다.
                    titlePressed = true; PressTitlePlate();
                    return;
                }
                if (flow == Flow.End)
                {
                    if (endT >= .75f && HitRestart(pointerLast)) StartRunDirect();
                    else { Refuse("가운데 건판을 눌러 다시 시작하시오."); PulseRestart(); }
                    return;
                }
                if (flow == Flow.Transition || flow == Flow.Reveal) { Refuse("건판을 현상하고 있습니다."); return; }
                if (flow != Flow.Practice && flow != Flow.Playing && flow != Flow.Bridge) return;
                int start = HitBee(pointerLast);
                if (start < 0)
                {
                    if (flow == Flow.Practice)
                    {
                        // 무점수 연습은 정밀도 시험이 아니다. 파사드에서 드래그를 시작하면
                        // 지속 경로의 출발 벌에 자석처럼 붙여 '한 획' 동작부터 학습시킨다.
                        for (int i = 0; i < 9; i++) if ((current.expectedMask & (1 << i)) != 0) { start = i; break; }
                        pointerLast = BeeScreen(start);
                    }
                    else { Refuse("숫자표에서 시작해 다른 벌까지 이어 그으시오."); PointNearestBee(pointerLast); return; }
                }
                drawing = true; selectedMask = 0; selectionCount = 0; st.selectedMask = 0; st.selectedCount = 0; st.selectedIds = "";
                BeginStroke(pointerLast); SelectBee(start); StrokeSound(true);
            }

            if (titlePressed && flow == Flow.Title && (MgfPointer.Up || !MgfPointer.Held))
            {
                titlePressed = false; ReleaseTitlePlate(); BeginTitleTransition(); return;
            }

            bool active = flow == Flow.Practice || flow == Flow.Playing || flow == Flow.Bridge;
            if (drawing && active && MgfPointer.Held)
            {
                Vector2 now = MgfPointer.Position;
                float step = Vector2.Distance(pointerLast, now);
                if (step > .5f)
                {
                    strokeDistance += step; HitBees(pointerLast, now); AddStrokePoint(now); pointerLast = now;
                }
            }

            if (drawing && active && (MgfPointer.Up || !MgfPointer.Held))
            {
                drawing = false; EndStroke(); StrokeSound(false);
                if (IsCancelZone(MgfPointer.Position))
                {
                    ClearSelection(); ShowToast("미제출 획을 반납했습니다.", 1.2f); MgfSfx.Play("whoosh", .16f); return;
                }
                if (flow == Flow.Practice && strokeDistance >= 36f * UiPx())
                {
                    ClearSelection();
                    for (int i = 0; i < 9; i++) if ((current.expectedMask & (1 << i)) != 0) SelectBee(i);
                    SubmitSelection(selectedMask, false);
                }
                else if (strokeDistance >= 24f * UiPx() && selectedMask != 0) SubmitSelection(selectedMask, false);
                else
                {
                    ShowToast(selectedMask == 0 ? "벌 숫자표에 손을 대시오." : "누르기만 하면 제출되지 않는다. 다른 벌까지 이어 그으시오.", 2.2f);
                    ReplayGuide(true); ClearSelection(); MgfSfx.Play("tap", .16f);
                }
            }
        }

        void HitBees(Vector2 from, Vector2 to)
        {
            float r = BeeHitRadius();
            for (int i = 0; i < 9; i++)
            {
                if ((selectedMask & (1 << i)) != 0) continue;
                if (DistanceToSegment(BeeScreen(i), from, to) <= r) SelectBee(i);
            }
        }

        static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 d = b - a; float n = d.sqrMagnitude;
            if (n < .001f) return Vector2.Distance(p, a);
            float t = Mathf.Clamp01(Vector2.Dot(p - a, d) / n);
            return Vector2.Distance(p, a + d * t);
        }

        void SelectBee(int index)
        {
            if (index < 0 || index >= 9 || (selectedMask & (1 << index)) != 0) return;
            selectedMask |= 1 << index; selectionOrder[selectionCount++] = index;
            st.selectedMask = selectedMask; st.selectedCount = selectionCount; st.selectedIds = SelectedIds(selectedMask);
            MarkBeeSelected(index, selectionCount); MgfBridge.NotifyChanged();
        }

        string SelectedIds(int mask)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < 9; i++) if ((mask & (1 << i)) != 0) { if (sb.Length > 0) sb.Append(','); sb.Append(i + 1); }
            return sb.ToString();
        }

        void ClearSelection()
        {
            selectedMask = 0; selectionCount = 0; st.selectedMask = 0; st.selectedCount = 0; st.selectedIds = "";
            ResetStrokeVisual(); MgfBridge.NotifyChanged();
        }

        void SubmitSelection(int mask, bool fromHook)
        {
            if (current == null || (flow != Flow.Practice && flow != Flow.Playing && flow != Flow.Bridge)) return;
            bool correct = BeolRules.IsExact(current, mask);
            bool practice = flow == Flow.Practice;
            bool bridge = flow == Flow.Bridge;
            bool wasFirst = firstAttempt;
            st.submissionCount++; st.selectedMask = mask; st.selectedCount = BeolRules.BitCount(mask); st.selectedIds = SelectedIds(mask);
            st.lastVerdict = correct ? "correct" : "wrong";
            st.misconceptionId = correct ? "" : BeolRules.MisconceptionId(current, mask);
            pendingMasteryFail = false;

            if (!practice && !bridge)
            {
                if (firstAttempt && !fromHook)
                {
                    st.firstAttemptTotal++; st.firstAttemptResolved = true;
                    if (correct) st.firstAttemptCorrect++;
                }
                if (correct)
                {
                    st.score += wasFirst && !fromHook ? 100 : 30; st.solved++;
                }
                else
                {
                    st.lives = Math.Max(0, st.lives - 1); st.attemptIndex++;
                    int remaining = BeolRules.Goal - st.firstAttemptTotal;
                    pendingMasteryFail = st.firstAttemptCorrect + remaining < BeolRules.MasteryFirst;
                }
            }
            else if (correct && fromHook) st.score += 30;
            if (!correct) firstAttempt = false;

            revealCorrect = correct; revealPractice = practice; revealBridge = bridge; revealT = 0f;
            flow = Flow.Reveal; st.frozen = true; drawing = false;
            if (correct) hitStop = .075f;
            BeginRevealVisual(correct, practice, bridge, current, mask, wasFirst && !fromHook);
            SetScreen(); MgfBridge.NotifyChanged();
        }

        void FinishReveal()
        {
            if (flow != Flow.Reveal) return;
            CompleteRevealVisual(revealCorrect);
            if (revealPractice)
            {
                if (revealCorrect) BeginMainAfterPractice();
                else { flow = Flow.Practice; st.frozen = true; ResetForProblem(false); ReplayGuide(true); }
            }
            else if (revealBridge)
            {
                if (revealCorrect) { flow = Flow.Playing; st.frozen = false; LoadNextProblem(); }
                else { flow = Flow.Bridge; st.frozen = true; ResetForProblem(false); ReplayGuide(true); }
            }
            else if (revealCorrect) { flow = Flow.Playing; st.frozen = false; LoadNextProblem(); }
            else if (st.lives <= 0) EndRun(false, "plates");
            else if (pendingMasteryFail) EndRun(false, "mastery_impossible");
            else
            {
                flow = Flow.Playing; st.frozen = false; ResetForProblem(false);
                ShowToast("같은 자료다. 나타난 횟수를 다시 세어 새 획으로 고치시오.", 2.2f);
            }
            SetScreen(); MgfBridge.NotifyChanged();
        }

        void EndRun(bool clear, string reason)
        {
            if (flow == Flow.End) return;
            flow = Flow.End; drawing = false; st.frozen = true; st.endReason = reason; endT = 0f;
            st.phase = clear ? "clear" : "gameover";
            try
            {
                int best = PlayerPrefs.GetInt("beol-sodong.best-first", 0);
                if (st.firstAttemptCorrect > best) PlayerPrefs.SetInt("beol-sodong.best-first", st.firstAttemptCorrect);
                PlayerPrefs.SetInt("beol-sodong.last-first", st.firstAttemptCorrect); PlayerPrefs.Save();
            }
            catch (Exception) { }
            ShowEnd(clear, reason); SetScreen(); MgfSfx.Play(clear ? "win" : "lose", .5f); MgfBridge.NotifyChanged();
        }

        void ReadyForHook()
        {
            if (flow == Flow.Title || flow == Flow.End || flow == Flow.Practice || flow == Flow.Bridge) StartRunDirect();
            else if (flow == Flow.Transition) StartRunDirect();
            else if (flow == Flow.Reveal) { revealT = 99f; FinishReveal(); if (flow == Flow.End) StartRunDirect(); }
        }

        public void TestStart() { StartRunDirect(); }
        public void TestAnswerCorrect()
        {
            ReadyForHook(); selectedMask = current.expectedMask; selectionCount = BeolRules.BitCount(selectedMask);
            st.selectedMask = selectedMask; st.selectedCount = selectionCount; st.selectedIds = SelectedIds(selectedMask);
            SubmitSelection(selectedMask, true);
        }
        public void TestAnswerWrong()
        {
            ReadyForHook(); selectedMask = BeolRules.DeliberateWrongMask(current); selectionCount = BeolRules.BitCount(selectedMask);
            st.selectedMask = selectedMask; st.selectedCount = selectionCount; st.selectedIds = SelectedIds(selectedMask);
            SubmitSelection(selectedMask, true);
        }
        public string StateJson()
        {
            if (cam != null && bees[0] != null) for (int i = 0; i < 9; i++) { Vector2 p = BeeScreen(i); st.beeX[i] = p.x; st.beeY[i] = p.y; }
            return JsonUtility.ToJson(st);
        }
        public string ProblemBankJson() => MgfJson.Bank(bank);
    }
}
