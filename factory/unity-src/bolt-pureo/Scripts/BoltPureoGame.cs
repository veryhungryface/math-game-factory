// 볼트 풀어 — 상태 머신, 롱프레스 입력, 실제 외심·내심 판정.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.BoltPureo
{
    public partial class BoltPureoGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public string requirement = "";
            public int trayCount;
            public int firstCorrect;
            public int firstAttempted;
            public int firstAccuracy;
            public int attempts;
            public int cartridgeOuter;
            public int cartridgeInner;
            public int pointerVersion;
            public int heldBolt = -1;
            public string evidence = "";
            public string misconceptionId = "";
            public bool onboarding;
            public bool layoutLand;
            public bool holdReady;
            public string hitMap = "";
            public string triangleGeometry = "";
            public string candidateGeometry = "";
        }

        enum GamePhase { Title, Practice, Playing, Reveal, End }

        readonly State st = new State();
        readonly List<MgfProblem> bank = new List<MgfProblem>();
        GamePhase phase = GamePhase.Title;
        BoltProblem current;
        BoltCandidate[] candidates;
        int runSerial;
        int problemIndex;
        bool firstSubmission;
        bool revealCorrect;
        bool revealPractice;
        bool holding;
        int heldIndex = -1;
        float holdStart;
        Vector2 holdStartScreen;
        float sessionLeft = BoltPureoRules.SessionSeconds;
        float revealClock;
        float idleGuide;
        string endReason = "";
        bool holdReady;
        int practiceMisses;
        float practiceAssistClock;

        void Awake()
        {
            MgfLook.Quality(34f);
            if (!BoltPureoRules.ValidateAll(out string validationError))
                Debug.LogError("[볼트 풀어] 문제 풀 검증 실패: " + validationError);
            bank.AddRange(BoltPureoRules.BuildBank());
            Prewarm();
            BuildWorld();
            BuildUi();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb = new StringBuilder("볼트풀어외심내심직각삼각형빗변중점세꼭짓점세변수선수직이등분선각의이등분선판별하시오카트리지임시트레이길게누르고손을떼시오빗속시계탑법랑판태엽도롱뇽모스첫시도정확도다시해체");
            for (int i = 0; i < bank.Count; i++)
            {
                sb.Append(bank[i].prompt); sb.Append(bank[i].answer); sb.Append(bank[i].unitConcept);
            }
            MgfText.Prewarm(sb.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(.05f, Time.unscaledDeltaTime);
            UpdateLayoutIfNeeded();
            HandleInput();

            if (phase == GamePhase.Playing)
            {
                sessionLeft -= dt;
                idleGuide += dt;
                if (sessionLeft <= 0f) EndRun(false, "90초 안에 해체하지 못했다");
                else if (idleGuide >= 8f) { idleGuide = 0f; BoostGuide(); }
            }
            else if (phase == GamePhase.Practice)
            {
                practiceAssistClock += dt;
                idleGuide += dt;
                if (idleGuide >= 8f) { idleGuide = 0f; BoostGuide(); }
                if (!practiceDemo && !holding && practiceAssistClock >= 5f) BeginPracticeDemo(5);
            }
            else if (phase == GamePhase.Reveal)
            {
                revealClock += dt;
                float wait = revealCorrect ? 1.45f : 1.05f;
                if (revealClock >= wait) FinishReveal();
            }
            UpdateWorldVisual(dt);
            UpdateUiVisual(dt);
        }

        void ShowTitle()
        {
            phase = GamePhase.Title;
            current = null; candidates = null; holding = false; heldIndex = -1;
            st.score = 0; st.lives = BoltPureoRules.StartLives; st.level = 1; st.solved = 0; st.phase = "title";
            st.requirement = ""; st.trayCount = 0; st.firstCorrect = 0; st.firstAttempted = 0;
            st.firstAccuracy = 0; st.attempts = 0; st.cartridgeOuter = 0; st.cartridgeInner = 0;
            st.pointerVersion = 0; st.heldBolt = -1; st.evidence = ""; st.misconceptionId = "";
            st.onboarding = false; st.layoutLand = IsLand(); st.holdReady = false;
            st.hitMap = ""; st.triangleGeometry = ""; st.candidateGeometry = ""; endReason = "";
            sessionLeft = BoltPureoRules.SessionSeconds; revealClock = 0; idleGuide = 0;
            holdReady = false; practiceMisses = 0;
            practiceAssistClock = 0f;
            ResetWorldForRun(); ShowTitleVisual(); SetScreen(); MgfBridge.NotifyChanged();
        }

        void ResetRun()
        {
            runSerial++;
            problemIndex = 0;
            sessionLeft = BoltPureoRules.SessionSeconds;
            st.score = 0; st.lives = BoltPureoRules.StartLives; st.level = 1; st.solved = 0;
            st.trayCount = 0; st.firstCorrect = 0; st.firstAttempted = 0; st.firstAccuracy = 0;
            st.attempts = 0; st.cartridgeOuter = 0; st.cartridgeInner = 0; st.pointerVersion = 0;
            st.heldBolt = -1; st.evidence = ""; st.misconceptionId = "";
            holding = false; heldIndex = -1; idleGuide = 0; endReason = "";
            holdReady = false; practiceMisses = 0; st.holdReady = false;
            practiceAssistClock = 0f;
            ResetWorldForRun();
        }

        void StartPractice()
        {
            ResetRun();
            phase = GamePhase.Practice; st.phase = "playing"; st.onboarding = true;
            practiceAssistClock = 0f;
            current = BoltPureoRules.Practice();
            candidates = BoltPureoRules.Candidates(current, 0, 0);
            // 첫 문제는 실제 정수 기하로 계산된 유일한 정답을 6번째 볼트에 고정한다.
            int firstCorrect = FindMatchingCandidate(0);
            if (firstCorrect >= 0 && firstCorrect != 5)
            {
                var t = candidates[5]; candidates[5] = candidates[firstCorrect]; candidates[firstCorrect] = t;
            }
            firstSubmission = true; st.requirement = "외심";
            ApplyRoundVisual(true); SetScreen(); BoostGuide(); MgfSfx.Play("whoosh", .24f); MgfBridge.NotifyChanged();
        }

        void StartRunDirect()
        {
            ResetRun();
            phase = GamePhase.Playing; st.phase = "playing"; st.onboarding = false;
            LoadProblem(); SetScreen(); MgfBridge.NotifyChanged();
        }

        void StartMainAfterPractice()
        {
            // 연습 기록은 본판 숙련도에 섞지 않는다.
            st.score = 0; st.lives = BoltPureoRules.StartLives; st.solved = 0; st.trayCount = 0;
            st.firstCorrect = 0; st.firstAttempted = 0; st.firstAccuracy = 0; st.attempts = 0;
            st.cartridgeOuter = 0; st.cartridgeInner = 0; st.misconceptionId = "";
            problemIndex = 0; sessionLeft = BoltPureoRules.SessionSeconds;
            phase = GamePhase.Playing; st.phase = "playing"; st.onboarding = false;
            LoadProblem(); SetScreen(); MgfBridge.NotifyChanged();
        }

        void LoadProblem()
        {
            current = BoltPureoRules.RunProblem(runSerial, problemIndex);
            candidates = BoltPureoRules.Candidates(current, runSerial, problemIndex);
            firstSubmission = true;
            st.level = current.band;
            st.requirement = current.target == CenterKind.Circumcenter ? "외심" : "내심";
            st.evidence = ""; st.heldBolt = -1;
            ApplyRoundVisual(false); BoostGuide();
        }

        void HandleInput()
        {
            if (MgfPointer.Down)
            {
                st.pointerVersion++;
                idleGuide = 0f;
                practiceAssistClock = 0f;
                PulseAt(MgfPointer.Position);
                if (phase == GamePhase.Title)
                {
                    PressTitleDriver(); StartPractice(); return;
                }
                if (phase == GamePhase.End)
                {
                    if (RestartHit(MgfPointer.Position)) StartPractice();
                    else RefuseAt(MgfPointer.Position, "아래 태엽 손잡이를 눌러 다시 해체하시오.");
                    MgfBridge.NotifyChanged(); return;
                }
                if (phase == GamePhase.Reveal)
                {
                    RefuseAt(MgfPointer.Position, "측정 결과가 끝날 때까지 잠시 기다리시오.");
                    MgfBridge.NotifyChanged(); return;
                }
                // 시범은 입력을 가로막지 않는다. 학생이 손을 대는 즉시 애니메이션만
                // 멈추고 아래의 실제 홀드→같은 볼트 릴리스 경로로 이어 간다.
                if (phase == GamePhase.Practice && practiceDemo)
                {
                    practiceDemo = false;
                    practiceDemoClock = 0f;
                    HideMeasureLines();
                }
                int hit = FindBoltAt(MgfPointer.Position);
                if (hit >= 0)
                {
                    holding = true; heldIndex = hit; holdStart = Time.unscaledTime; holdStartScreen = MgfPointer.Position;
                    holdReady = false; st.holdReady = false;
                    st.heldBolt = hit; st.evidence = candidates[hit].evidenceTitle + " " + candidates[hit].evidenceValues;
                    BeginHoldVisual(hit); MgfSfx.Play("tap", .18f); MgfBridge.NotifyChanged();
                }
                else
                {
                    holding = false; heldIndex = -1; st.heldBolt = -1;
                    RefuseAt(MgfPointer.Position, "구리 볼트를 길게 누르면 측정선이 열린다.");
                    BoostGuide(); MgfBridge.NotifyChanged();
                }
            }

            if (holding && MgfPointer.Held)
            {
                float held = Time.unscaledTime - holdStart;
                UpdateHoldVisual(heldIndex, Mathf.Clamp01(held / .25f));
                if (held >= .25f && !holdReady)
                {
                    holdReady = true; st.holdReady = true;
                    HoldReadyVisual(heldIndex); MgfBridge.NotifyChanged();
                }
                float driftLimit = phase == GamePhase.Practice ? 125f : 86f;
                if ((MgfPointer.Position - holdStartScreen).sqrMagnitude > driftLimit * driftLimit)
                {
                    holding = false; holdReady = false; st.holdReady = false; st.heldBolt = -1;
                    CancelHoldVisual(heldIndex);
                    RefuseAt(MgfPointer.Position, "손가락을 볼트 위에 둔 채 길게 누르시오.");
                    if (phase == GamePhase.Practice) RegisterPracticeMiss();
                    heldIndex = -1; MgfBridge.NotifyChanged();
                }
            }

            if (holding && MgfPointer.Up)
            {
                float held = Time.unscaledTime - holdStart;
                int releaseIndex = FindBoltAt(MgfPointer.Position);
                int submitIndex = heldIndex;
                holding = false; holdReady = false; st.holdReady = false; heldIndex = -1; st.heldBolt = -1;
                EndHoldVisual(submitIndex, held >= .25f && releaseIndex == submitIndex);
                if (held >= .25f && releaseIndex == submitIndex) SubmitBolt(submitIndex);
                else
                {
                    st.evidence = candidates[submitIndex].evidenceTitle + " " + candidates[submitIndex].evidenceValues;
                    RefuseAt(MgfPointer.Position, "측정선을 읽은 뒤 같은 볼트에서 손을 떼시오.");
                    if (phase == GamePhase.Practice) RegisterPracticeMiss();
                    BoostGuide(); MgfBridge.NotifyChanged();
                }
            }
        }

        void SubmitBolt(int index)
        {
            if ((phase != GamePhase.Practice && phase != GamePhase.Playing) || candidates == null) return;
            if (index < 0 || index >= candidates.Length) return;
            var picked = candidates[index];
            bool practice = phase == GamePhase.Practice;
            var geometry = BoltPureoRules.GeometryFor(current);
            bool exactProperty = picked.evidenceMode == CenterKind.Circumcenter
                ? BoltPureoRules.IsCircumcenter(geometry, picked.Point)
                : BoltPureoRules.IsIncenter(geometry, picked.Point);
            bool correct = picked.evidenceMode == current.target && exactProperty;
            if (correct != picked.matches)
                Debug.LogError("[볼트 풀어] 후보 판정과 기하 계산 불일치: " + index);
            st.attempts++;
            st.misconceptionId = correct ? "" : picked.misconceptionId;

            if (practice)
            {
                if (!correct)
                {
                    RegisterPracticeMiss();
                    st.evidence = picked.evidenceTitle + " " + picked.evidenceValues;
                    PracticeWrong(index, ExplainWrong(picked));
                    MgfBridge.NotifyChanged(); return;
                }
                revealPractice = true; revealCorrect = true; revealClock = 0f;
                phase = GamePhase.Reveal;
                st.score = 100; st.solved = 1;
                st.evidence = picked.evidenceTitle + " " + picked.evidenceValues;
                CorrectVisual(index, true, 1); MgfSfx.Play("correct", .52f); SetScreen(); MgfBridge.NotifyChanged();
                return;
            }

            if (firstSubmission)
            {
                st.firstAttempted++;
                if (correct) st.firstCorrect++;
                st.firstAccuracy = st.firstAttempted == 0 ? 0 : st.firstCorrect * 100 / st.firstAttempted;
                firstSubmission = false;
            }

            revealPractice = false; revealCorrect = correct; revealClock = 0f;
            phase = GamePhase.Reveal;
            if (correct)
            {
                st.solved++;
                st.score += 140 + st.level * 25 + st.firstCorrect * 8;
                if (current.target == CenterKind.Circumcenter) st.cartridgeOuter++;
                else st.cartridgeInner++;
                st.evidence = picked.evidenceTitle + " " + picked.evidenceValues;
                CorrectVisual(index, false, st.solved); MgfSfx.Play("correct", .48f);
            }
            else
            {
                st.trayCount++;
                st.lives = Math.Max(0, BoltPureoRules.StartLives - st.trayCount);
                st.evidence = picked.evidenceTitle + " " + picked.evidenceValues;
                WrongVisual(index, st.trayCount, ExplainWrong(picked)); MgfSfx.Play(st.lives == 0 ? "lose" : "wrong", .48f);
                if (st.lives == 0) endReason = "임시 트레이 3칸이 찬 뒤 다른 볼트를 더 풀어 기어가 잠겼다";
            }
            SetScreen(); MgfBridge.NotifyChanged();
        }

        string ExplainWrong(BoltCandidate c)
        {
            switch (c.misconceptionId)
            {
                case "swap_circum_incenter": return st.requirement == "외심"
                    ? "이 선은 변까지의 거리를 재므로 내심의 증거다."
                    : "이 선은 꼭짓점까지의 거리를 재므로 외심의 증거다.";
                case "circumcenter_always_inside": return "둔각삼각형의 외심은 삼각형 밖에 있을 수 있다.";
                case "right_vertex_as_circumcenter": return "직각삼각형의 외심은 직각 꼭짓점이 아니라 빗변의 중점이다.";
                case "rotated_base_midpoint": return "아래쪽 모양이 아니라 같은 두 변과 꼭지각을 확인해야 한다.";
                default: return "세 측정값이 같지 않다. 측정선을 다시 비교하시오.";
            }
        }

        void RegisterPracticeMiss()
        {
            practiceMisses++;
            if (practiceMisses >= 3)
            {
                practiceMisses = 0;
                BeginPracticeDemo(5);
            }
        }

        void FinishReveal()
        {
            if (revealPractice)
            {
                StartMainAfterPractice(); return;
            }
            if (!revealCorrect)
            {
                if (st.lives <= 0) { EndRun(false, endReason); return; }
                phase = GamePhase.Playing; st.phase = "playing";
                RestoreAfterWrong(); SetScreen(); MgfBridge.NotifyChanged(); return;
            }
            if (st.solved >= BoltPureoRules.Goal)
            {
                bool mastered = st.firstCorrect >= BoltPureoRules.RequiredFirstCorrect;
                EndRun(mastered, mastered ? "세 법랑판이 기계 새의 날개가 되었다" : "해체했지만 첫 시도 정답이 5/6에 미치지 못했다");
                return;
            }
            problemIndex++;
            phase = GamePhase.Playing; st.phase = "playing";
            LoadProblem(); SetScreen(); MgfBridge.NotifyChanged();
        }

        void EndRun(bool clear, string reason)
        {
            phase = GamePhase.End; st.phase = clear ? "clear" : "gameover"; st.onboarding = false;
            holding = false; st.heldBolt = -1; endReason = reason;
            EndVisual(clear, reason); SetScreen(); MgfSfx.Play(clear ? "win" : "lose", .58f); MgfBridge.NotifyChanged();
        }

        int FindMatchingCandidate(int start)
        {
            if (candidates == null) return -1;
            for (int n = 0; n < candidates.Length; n++)
            {
                int i = (start + n) % candidates.Length;
                if (candidates[i].matches) return i;
            }
            return -1;
        }

        public void TestStart() { StartRunDirect(); }

        public void TestAnswerCorrect()
        {
            if (phase != GamePhase.Playing) StartRunDirect();
            int i = FindMatchingCandidate(problemIndex * 3);
            SubmitBolt(i);
        }

        public void TestAnswerWrong()
        {
            if (phase != GamePhase.Playing) StartRunDirect();
            int i = -1;
            for (int k = 0; k < candidates.Length; k++) if (!candidates[k].matches) { i = k; break; }
            SubmitBolt(i);
        }

        public string StateJson() => JsonUtility.ToJson(st);
        public string ProblemBankJson() => MgfJson.Bank(bank);
    }
}
