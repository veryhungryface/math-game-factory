// 박쥐 깨워 — 타이틀, 고정 연습, 한 획 집합 제출, 단계 전환 시범, 숙련 실패 상태.
// 학생이 쓸고(획이 닿은 날개 집합), 코드는 그 집합이 정확한지만 판정한다. 획 도중에는 정오를 알리지 않는다.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.BakjwiKkaewo
{
    public sealed partial class BakjwiKkaewoGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int order;
            public int firstAttemptTotal;
            public int firstAttemptCorrect;
            public int aaFirstAttempted;
            public int aaFirstCorrect;
            public int pointerVersion;
            public int selectedMask;
            public int submissionCount;
            public int[] firstCorrectByBand = new int[3];
            public string band = "";
            public string sceneId = "";
            public string prompt = "";   // 화면에 보이는 기준·날개 자료를 문장화(장면마다 고정, 타이머 없음)
            public string selectedIds = "";
            public string lastVerdict = "";
            public string misconceptionId = "";
            public string endReason = "";
            public bool attempted;
            public bool firstCorrect;
            public bool onboarding;
            public bool frozen;
        }

        enum GamePhase { Title, Transition, Practice, Interlude, Playing, Reveal, End }

        readonly State st = new State();
        readonly List<BatScene> scenes = new List<BatScene>();
        readonly List<MgfProblem> bank = new List<MgfProblem>();
        readonly System.Random rng = new System.Random(Environment.TickCount);
        List<BatScene> deck;
        BatScene current;
        GamePhase gamePhase = GamePhase.Title;
        int deckIndex;
        int selectedMask;
        readonly int[] selectionOrder = new int[4];
        int selectionCount;
        bool drawing;
        bool titlePressed;
        bool firstAttempt;
        bool revealCorrect;
        bool revealPractice;
        bool pendingImpossible;
        float timeLeft;
        float transitionClock;
        float revealClock;
        float interludeClock;
        float idleClock;
        float endClock;
        float strokeDistance;
        float hitStop;
        Vector2 pointerLast;

        const float RevealCorrectSec = 1.75f, RevealWrongSec = 2.1f, InterludeSec = 1.7f, TransitionSec = .45f;

        void Awake()
        {
            // CreatePrimitive 가 붙이는 콜라이더 형식을 빌드 스트리핑에서 지킨다(쓰지는 않는다).
            var keep = new GameObject("ColliderTypes");
            keep.AddComponent<SphereCollider>(); keep.AddComponent<CapsuleCollider>(); keep.AddComponent<BoxCollider>(); keep.AddComponent<MeshCollider>();
            Destroy(keep);

            MgfLook.Quality(60f);
            scenes.AddRange(BakjwiRules.BuildBank());
            var log = new StringBuilder();
            int auditErrors = BakjwiRules.AuditBank(scenes, log);
            if (auditErrors != 0) Debug.LogWarning("[박쥐 깨워] 문제 은행 검산 경고 " + auditErrors + "\n" + log);
            bank.AddRange(BakjwiRules.BuildMgfBank(scenes));
            BuildWorld();
            BuildUi();
            Prewarm();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb = new StringBuilder("박쥐깨워닮은날개만쓸면박쥐가깨어난다중학교2학년2학기도형의닮음최고점기록가운데를눌러깨우기다시");
            sb.Append("기준삼각형과닮은날개를모두찾아한획으로쓸어손을떼시오SSSSASAA닮음대응변대응각끼인각단계연습시범");
            sb.Append("소란깃남은시간첫시도깨운장면재도전세대응변의길이의비가같다두대응각의크기가각각같다같은수를더한길이같은배인지확인하시오");
            sb.Append("두비만같다세대응변의비를모두확인하시오날개가펴진뒤쓸어주시오비행무리숙련달성놓침닮지않음남은각까지구해비교하시오작은변끼리비를맞춰보시오");
            sb.Append("0123456789:=≠−·°△∽∠○×ABCDEFGHIJKLPQR번cm점분초완주목표다시노려라밤이끝났다모두잃었다동굴비행완성두변사이의끼인각이다르다");
            sb.Append("유령붓을따라두날개를이어쓸어보시오빛나는1번에서2번까지한번에쓸고손을떼시오아무곳이나눌러");
            for (int i = 0; i < bank.Count; i++) { sb.Append(bank[i].unitConcept); }
            MgfText.Prewarm(sb.ToString());
        }

        void Update()
        {
            float raw = Mathf.Min(.05f, Time.deltaTime);
            float dt = raw;
            if (hitStop > 0f) { hitStop -= raw; dt = 0f; }   // 정답 확정 순간 짧은 멈춤(hit-stop)
            HandleInput();

            switch (gamePhase)
            {
                case GamePhase.Transition:
                    transitionClock += dt;
                    if (transitionClock >= TransitionSec) StartPracticeNow();
                    break;
                case GamePhase.Practice:
                    idleClock += dt;
                    if (idleClock >= 8f) { idleClock = 0f; ReplayPracticeGuide(true); }
                    break;
                case GamePhase.Interlude:
                    interludeClock += dt;
                    if (interludeClock >= InterludeSec) EndInterlude();
                    break;
                case GamePhase.Playing:
                    timeLeft = Mathf.Max(0f, timeLeft - dt);
                    if (timeLeft <= 0f) { if (drawing) { drawing = false; EndStroke(); } EndRun("time"); }
                    idleClock += dt;
                    if (idleClock >= 12f) { idleClock = 0f; ShowToast("수치와 각을 읽고 닮은 날개만 한 획으로 쓸어 손을 떼시오", 2f, false); }
                    break;
                case GamePhase.Reveal:
                    revealClock += dt;
                    UpdateRevealVisual(revealClock, revealCorrect);
                    if (revealClock >= (revealCorrect ? RevealCorrectSec : RevealWrongSec)) FinishReveal();
                    break;
                case GamePhase.End:
                    endClock += raw;
                    break;
            }

            AnimateWorld(dt, raw);
            UpdateUi(raw);
            ApplyResponsiveLayout();
        }

        // ───────────── 흐름 ─────────────
        void ShowTitle()
        {
            gamePhase = GamePhase.Title;
            current = null; deck = null; deckIndex = 0; drawing = false; titlePressed = false;
            ResetCounters();
            timeLeft = BakjwiRules.RunSeconds;
            ResetVisualRun(); ShowTitleVisual(); SetScreen(); MgfBridge.NotifyChanged();
        }

        void ResetCounters()
        {
            selectedMask = 0; selectionCount = 0;
            st.score = 0; st.lives = BakjwiRules.StartLives; st.level = 1; st.solved = 0;
            st.order = 0; st.firstAttemptTotal = 0; st.firstAttemptCorrect = 0;
            st.aaFirstAttempted = 0; st.aaFirstCorrect = 0; st.submissionCount = 0;
            st.firstCorrectByBand = new int[3]; st.selectedMask = 0; st.selectedIds = "";
            st.band = ""; st.sceneId = ""; st.lastVerdict = ""; st.misconceptionId = ""; st.endReason = "";
            st.attempted = false; st.firstCorrect = false; st.onboarding = false; st.frozen = false;
            pendingImpossible = false;
        }

        void BeginTitleTransition()
        {
            if (gamePhase != GamePhase.Title) return;
            gamePhase = GamePhase.Transition; transitionClock = 0f; titlePressed = false;
            StartSplitReveal(); MgfSfx.Play("whoosh", .45f); SetScreen(); MgfBridge.NotifyChanged();
        }

        void StartPracticeNow()
        {
            ResetRun();
            gamePhase = GamePhase.Practice;
            current = BakjwiRules.Practice();
            firstAttempt = true;
            ResetForScene(true); SetScreen(); ReplayPracticeGuide(false);
            MgfBridge.NotifyChanged();
        }

        void ResetRun()
        {
            deck = BakjwiRules.RunDeck(scenes, rng);
            deckIndex = 0; timeLeft = BakjwiRules.RunSeconds; drawing = false;
            ResetCounters(); ResetVisualRun();
        }

        void StartRunDirect()
        {
            ResetRun();
            gamePhase = GamePhase.Playing;
            LoadNextScene(); SetScreen(); MgfBridge.NotifyChanged();
        }

        void BeginMainAfterPractice()
        {
            ResetCounters(); timeLeft = BakjwiRules.RunSeconds; deckIndex = 0;
            gamePhase = GamePhase.Playing;
            LoadNextScene(); SetScreen();
        }

        void LoadNextScene()
        {
            if (st.solved >= BakjwiRules.Goal)
            {
                bool mastery = st.firstAttemptCorrect >= BakjwiRules.MasteryFirst && st.aaFirstCorrect >= BakjwiRules.MasteryAa;
                EndRun(mastery ? "clear" : "mastery_fail");
                return;
            }
            SimilarityBand prev = current != null && current.id != "practice-fixed" ? current.band : (SimilarityBand)0;
            current = deck[deckIndex++];
            firstAttempt = true;
            st.order = st.solved + 1; st.level = (int)current.band; st.band = current.band.ToString();
            st.attempted = false; st.firstCorrect = false; st.lastVerdict = ""; st.misconceptionId = "";
            ResetForScene(true);
            if (prev != 0 && prev != current.band)
            {
                gamePhase = GamePhase.Interlude; interludeClock = 0f; BeginInterludeVisual(current.band);
            }
            SetScreen(); MgfBridge.NotifyChanged();
        }

        void EndInterlude()
        {
            if (gamePhase != GamePhase.Interlude) return;
            gamePhase = GamePhase.Playing; EndInterludeVisual(); SetScreen(); MgfBridge.NotifyChanged();
        }

        void ResetForScene(bool enter)
        {
            drawing = false; strokeDistance = 0f; idleClock = 0f; selectedMask = 0; selectionCount = 0;
            st.selectedMask = 0; st.selectedIds = "";
            ResetStrokeVisual(); ShowScene(current, enter);
        }

        // ───────────── 입력: 한 획 ─────────────
        void HandleInput()
        {
            if (MgfPointer.Down)
            {
                st.pointerVersion++; pointerLast = MgfPointer.Position; strokeDistance = 0f; idleClock = 0f;
                SpawnPointerRipple(MgfPointer.Position); MgfBridge.NotifyChanged();
                switch (gamePhase)
                {
                    case GamePhase.Title: titlePressed = true; PressTitleBat(); return;
                    case GamePhase.End:
                        if (endClock >= .9f) { StartRunDirect(); MgfSfx.Play("whoosh", .4f); }
                        else NudgeEnd();
                        return;
                    case GamePhase.Transition: case GamePhase.Reveal: case GamePhase.Interlude:
                        RefuseDuringReveal(); return;
                }
                drawing = true; selectedMask = 0; selectionCount = 0; st.selectedMask = 0; st.selectedIds = "";
                BeginStroke(pointerLast);
                HitCandidates(pointerLast, pointerLast);
                StrokeSound(true);
            }

            bool canDraw = gamePhase == GamePhase.Practice || gamePhase == GamePhase.Playing;
            if (titlePressed && gamePhase == GamePhase.Title && (MgfPointer.Up || !MgfPointer.Held))
            {
                titlePressed = false; ReleaseTitleBat(); BeginTitleTransition(); return;
            }
            if (drawing && canDraw && MgfPointer.Held)
            {
                Vector2 now = MgfPointer.Position;
                float step = Vector2.Distance(pointerLast, now);
                if (step > .5f)
                {
                    strokeDistance += step;
                    HitCandidates(pointerLast, now);
                    AddStrokePoint(now);
                    pointerLast = now;
                }
            }
            // 손을 떼거나(pointerup) 포인터를 잃으면(pointercancel·화면 밖) 그때까지 닿은 집합으로 제출
            if (drawing && canDraw && (MgfPointer.Up || !MgfPointer.Held))
            {
                drawing = false; EndStroke(); StrokeSound(false);
                if (strokeDistance >= 20f * UiPx() && selectedMask != 0) SubmitSelection(selectedMask, false);
                else
                {
                    st.lastVerdict = selectedMask != 0 ? "tap" : "empty";
                    if (selectedMask != 0)
                        ShowToast("누르기만 하면 제출되지 않는다\n닮은 날개들을 이어 쓸고 손을 떼시오", 2.4f, false);
                    else
                        ShowToast("빈 바위다 · 날개에 손가락을 대고 쓸어 지나가시오", 2.2f, false);
                    ReplayGuideForTap(); MgfSfx.Play("tap", .2f);
                    selectedMask = 0; selectionCount = 0; st.selectedMask = 0; st.selectedIds = "";
                    ClearTouchMarks(); MgfBridge.NotifyChanged();
                }
            }
        }

        void HitCandidates(Vector2 from, Vector2 to)
        {
            float radius = CandidateHitRadius();
            for (int i = 0; i < BakjwiRules.CandidateCount; i++)
            {
                if ((selectedMask & (1 << i)) != 0) continue;
                if (DistanceToSegment(CandidateScreen(i), from, to) <= radius) SelectCandidate(i);
            }
        }

        static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 d = b - a;
            float n = d.sqrMagnitude;
            if (n < .001f) return Vector2.Distance(p, a);
            float t = Mathf.Clamp01(Vector2.Dot(p - a, d) / n);
            return Vector2.Distance(p, a + d * t);
        }

        void SelectCandidate(int index)
        {
            selectedMask |= 1 << index;
            selectionOrder[selectionCount++] = index;
            st.selectedMask = selectedMask; st.selectedIds = SelectedIds(selectedMask);
            MarkCandidateTouched(index, selectionCount); MgfBridge.NotifyChanged();
        }

        static string SelectedIds(int mask) { return BakjwiRules.MaskAnswer(mask); }

        int SelectionRank(int candidate)
        {
            for (int i = 0; i < selectionCount; i++) if (selectionOrder[i] == candidate) return i;
            return candidate;
        }

        // ───────────── 판정 ─────────────
        void SubmitSelection(int mask, bool fromHook)
        {
            if (current == null || (gamePhase != GamePhase.Practice && gamePhase != GamePhase.Playing)) return;
            bool correct = mask == current.correctMask;
            bool practice = gamePhase == GamePhase.Practice;
            bool wasFirst = firstAttempt;
            st.submissionCount++;
            st.selectedMask = mask; st.selectedIds = SelectedIds(mask);
            st.lastVerdict = correct ? "correct" : "wrong";
            st.misconceptionId = correct ? "" : FirstMisconception(current, mask);
            pendingImpossible = false;

            if (!practice)
            {
                // 첫 시도 기록은 장면마다 한 번만. 훅 강제 제출은 학습 통계에 섞지 않는다.
                if (firstAttempt && !fromHook)
                {
                    st.firstAttemptTotal++; st.attempted = true;
                    if (current.band == SimilarityBand.AA) st.aaFirstAttempted++;
                    if (correct)
                    {
                        st.firstAttemptCorrect++; st.firstCorrect = true;
                        st.firstCorrectByBand[(int)current.band - 1]++;
                        if (current.band == SimilarityBand.AA) st.aaFirstCorrect++;
                    }
                }
                if (correct) { st.score += 100 + (wasFirst && !fromHook ? 50 : 0); st.solved++; }
                else
                {
                    st.lives = Math.Max(0, st.lives - 1);
                    pendingImpossible = MasteryImpossible();
                }
            }
            if (!correct) firstAttempt = false;

            revealCorrect = correct; revealPractice = practice;
            gamePhase = GamePhase.Reveal; revealClock = 0f; drawing = false;
            if (correct) hitStop = .08f;
            BeginRevealVisual(correct, practice, mask, current, wasFirst && !fromHook);
            SetScreen(); MgfBridge.NotifyChanged();
        }

        string FirstMisconception(BatScene p, int mask)
        {
            int extra = mask & ~p.correctMask;
            for (int i = 0; i < 4; i++) if ((extra & (1 << i)) != 0) return p.wings[i].misconceptionId;
            return p.band == SimilarityBand.SSS ? "missing_sss" : p.band == SimilarityBand.SAS ? "missing_sas" : "missing_aa";
        }

        // 남은 장면을 전부 첫 시도로 맞혀도 8/10 또는 AA 3/4 에 못 미치면 즉시 끝난다.
        bool MasteryImpossible()
        {
            int remaining = BakjwiRules.Goal - st.firstAttemptTotal;
            if (st.firstAttemptCorrect + remaining < BakjwiRules.MasteryFirst) return true;
            int aaRemaining = 4 - st.aaFirstAttempted;
            return st.aaFirstCorrect + aaRemaining < BakjwiRules.MasteryAa;
        }

        void FinishReveal()
        {
            if (gamePhase != GamePhase.Reveal) return;
            CompleteRevealVisual();
            if (revealPractice)
            {
                if (revealCorrect) BeginMainAfterPractice();
                else
                {
                    gamePhase = GamePhase.Practice; ResetForScene(false); ReplayPracticeGuide(true, false);
                    ShowToast(BakjwiRules.WrongReason(current, st.selectedMask) + "\n다시: 빛나는 1번에서 2번까지 이어 쓰시오", 3.2f, true);
                }
            }
            else if (revealCorrect) { gamePhase = GamePhase.Playing; LoadNextScene(); }
            else if (st.lives <= 0) EndRun("feathers");
            else if (pendingImpossible) EndRun("mastery_impossible");
            else
            {
                gamePhase = GamePhase.Playing; ResetForScene(false);
                ShowToast("같은 장면이다 · 근거를 떠올려 다시 쓸어 보시오", 1.8f, false);
            }
            SetScreen(); MgfBridge.NotifyChanged();
        }

        void EndRun(string reason)
        {
            if (gamePhase == GamePhase.End) return;
            gamePhase = GamePhase.End; drawing = false; st.endReason = reason; endClock = 0f;
            try
            {
                int best = PlayerPrefs.GetInt("bakjwi-kkaewo.best", 0);
                if (st.score > best) PlayerPrefs.SetInt("bakjwi-kkaewo.best", st.score);
                if (reason == "clear")
                {
                    PlayerPrefs.SetInt("bakjwi-kkaewo.stamp.sss", Mathf.Max(PlayerPrefs.GetInt("bakjwi-kkaewo.stamp.sss", 0), st.firstCorrectByBand[0]));
                    PlayerPrefs.SetInt("bakjwi-kkaewo.stamp.sas", Mathf.Max(PlayerPrefs.GetInt("bakjwi-kkaewo.stamp.sas", 0), st.firstCorrectByBand[1]));
                    PlayerPrefs.SetInt("bakjwi-kkaewo.stamp.aa", Mathf.Max(PlayerPrefs.GetInt("bakjwi-kkaewo.stamp.aa", 0), st.firstCorrectByBand[2]));
                }
                PlayerPrefs.Save();
            }
            catch (Exception) { /* 저장 실패해도 플레이는 계속 */ }
            ShowEnd(reason); SetScreen(); MgfSfx.Play(reason == "clear" ? "win" : "lose", .55f); MgfBridge.NotifyChanged();
        }

        void ReplayGuideForTap()
        {
            PeekNearestBat(MgfPointer.Position);
            if (gamePhase == GamePhase.Practice) ReplayPracticeGuide(true, false);
            else PointAtNearestCandidate(MgfPointer.Position);
        }

        // ───────────── 훅 ─────────────
        void SettleForHook()
        {
            if (gamePhase == GamePhase.Reveal) { revealClock = 99f; FinishReveal(); }
            if (gamePhase == GamePhase.Interlude) EndInterlude();
        }

        public void TestStart() { SettleForHook(); StartRunDirect(); }

        public void TestAnswerCorrect()
        {
            SettleForHook();
            if (gamePhase != GamePhase.Playing || current == null) StartRunDirect();
            if (gamePhase == GamePhase.Interlude) EndInterlude();
            selectedMask = current.correctMask; selectionCount = 0;
            for (int i = 0; i < 4; i++) if ((selectedMask & (1 << i)) != 0) selectionOrder[selectionCount++] = i;
            SubmitSelection(selectedMask, true);
        }

        public void TestAnswerWrong()
        {
            SettleForHook();
            if (gamePhase != GamePhase.Playing || current == null) StartRunDirect();
            if (gamePhase == GamePhase.Interlude) EndInterlude();
            int wrong = 15;   // 네 날개 전부 — 정답 마스크는 1..14 라 언제나 오답
            selectedMask = wrong; selectionCount = 4; for (int i = 0; i < 4; i++) selectionOrder[i] = i;
            SubmitSelection(wrong, true);
        }

        public string StateJson()
        {
            if (gamePhase == GamePhase.Title) st.phase = "title";
            else if (gamePhase == GamePhase.End) st.phase = st.endReason == "clear" ? "won" : "lost";
            else st.phase = "playing";
            st.onboarding = gamePhase == GamePhase.Practice || gamePhase == GamePhase.Transition;
            st.frozen = st.onboarding || gamePhase == GamePhase.Reveal || gamePhase == GamePhase.Interlude;
            st.level = current == null ? 1 : (int)current.band;
            st.sceneId = current == null ? "" : current.id;
            st.prompt = current == null || gamePhase == GamePhase.Title || gamePhase == GamePhase.End ? "" : current.prompt;
            st.selectedMask = selectedMask;
            st.selectedIds = SelectedIds(selectedMask);
            return JsonUtility.ToJson(st);
        }

        public string ProblemBankJson() { return MgfJson.Bank(bank); }
    }
}
