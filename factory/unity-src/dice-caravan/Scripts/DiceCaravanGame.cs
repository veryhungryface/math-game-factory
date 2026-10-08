using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.DiceCaravan
{
    public partial class DiceCaravanGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int combo;
            public int firstAttemptTotal;
            public int firstAttemptCorrect;
            public int selectedPins;
            public int pointerVersion;
            public int windKnot;
            public string problemId = "";
            public string misconceptionId = "";
            public bool onboarding;
        }

        enum RunPhase { Title, Practice, Playing, Feedback, End }

        readonly State st = new State();
        readonly List<CaravanProblem> catalog = new List<CaravanProblem>();
        readonly List<MgfProblem> bank = new List<MgfProblem>();
        readonly bool[] selected = new bool[36];
        readonly bool[] feedbackMissing = new bool[36];
        readonly bool[] feedbackExtra = new bool[36];

        RunPhase phase = RunPhase.Title;
        CaravanProblem current;
        int runSerial;
        int missionSerial;
        bool attempted;
        bool feedbackCorrect;
        bool feedbackPractice;
        float feedbackClock;
        float runClock;
        float problemClock;
        float problemLimit;
        float idleGuide;
        bool draggingCord;
        Vector2 cordStart;
        int dragLastPin = -1;
        string endReason = "";

        void Awake()
        {
            MgfLook.Quality(34f);
            catalog.AddRange(DiceCaravanRules.BuildCatalog());
            for (int i = 0; i < catalog.Count; i++) bank.Add(catalog[i].ToMgf());
            Prewarm();
            BuildWorld();
            BuildUi();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb = new StringBuilder("다이스캐러밴핀을엮어달길을열어라중학교2학년경우의수조건에맞는경우를모두핀으로표시하시오서로다른두개의주사위를동시에던질때두눈의수의합곱대표회장부회장십의자리일의자리자연수황동유리성도판돛끈바람등순풍매듭첫시도정답오답항로완주대상이염호를건넜다멈췄다다시출항덮개열기경우를먼저표시하시오");
            for (int i = 0; i < catalog.Count; i++) { sb.Append(catalog[i].prompt); sb.Append(catalog[i].reveal); sb.Append(catalog[i].concept); }
            MgfText.Prewarm(sb.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(.05f, Time.deltaTime);
            HandleInput();

            if (phase == RunPhase.Playing)
            {
                runClock += dt;
                problemClock += dt;
                idleGuide += dt;
                if (runClock >= 90f) EndRun(false, "90초 항해 시간이 끝났다");
                else if (problemClock >= problemLimit) Resolve(false, "timeout");
                else if (idleGuide >= 8f) { idleGuide = 0f; BoostGuide(); }
            }
            else if (phase == RunPhase.Practice)
            {
                // Onboarding is a true frozen gate: no timer, spawning, or penalty advances.
                idleGuide += dt;
                if (idleGuide >= 8f) { idleGuide = 0f; BoostGuide(); }
            }
            else if (phase == RunPhase.Feedback)
            {
                feedbackClock += dt;
                if (feedbackClock >= (feedbackCorrect ? 1.55f : 1.25f)) FinishFeedback();
            }

            UpdateWorld(dt);
            UpdateUi(dt);
        }

        void ShowTitle()
        {
            phase = RunPhase.Title;
            current = DiceCaravanRules.Practice();
            st.score = 0; st.lives = DiceCaravanRules.StartLives; st.level = 1; st.solved = 0;
            st.combo = 0; st.firstAttemptTotal = 0; st.firstAttemptCorrect = 0; st.selectedPins = 0;
            st.pointerVersion = 0; st.windKnot = 0; st.problemId = current.id; st.misconceptionId = "";
            st.onboarding = false; st.phase = "title"; endReason = "";
            ClearSelections(); ResetWorldForRun(); SetScreen(); MgfBridge.NotifyChanged();
        }

        void BeginRun(bool onboarding)
        {
            runSerial++;
            missionSerial = 0;
            phase = onboarding ? RunPhase.Practice : RunPhase.Playing;
            st.score = 0; st.lives = DiceCaravanRules.StartLives; st.level = 1; st.solved = 0;
            st.combo = 0; st.firstAttemptTotal = 0; st.firstAttemptCorrect = 0; st.selectedPins = 0;
            st.windKnot = 0; st.misconceptionId = ""; st.onboarding = onboarding; st.phase = "playing";
            endReason = ""; runClock = 0f; draggingCord = false; dragLastPin = -1;
            ResetWorldForRun();
            if (onboarding) LoadPractice(); else LoadMission();
            SetScreen(); StartGuide(); MgfSfx.Play("whoosh", .32f); MgfBridge.NotifyChanged();
        }

        void LoadPractice()
        {
            current = DiceCaravanRules.Practice();
            st.level = 1; st.problemId = current.id; attempted = false;
            ClearSelections(); RefreshProblemUi(); ResetWorldForProblem();
        }

        void LoadMission()
        {
            current = DiceCaravanRules.Pick(catalog, st.solved, runSerial * 977 + missionSerial * 151 + st.solved * 43);
            missionSerial++;
            st.level = Mathf.Min(DiceCaravanRules.Goal, st.solved + 1); st.problemId = current.id;
            st.misconceptionId = ""; attempted = false; problemClock = 0f; idleGuide = 0f;
            float baseLimit = st.lives >= 3 ? 26f : st.lives == 2 ? 22f : 18f;
            if (st.windKnot > 0) { baseLimit = 26f; st.windKnot = 0; }
            problemLimit = baseLimit;
            ClearSelections(); RefreshProblemUi(); ResetWorldForProblem(); MgfBridge.NotifyChanged();
        }

        void HandleInput()
        {
            bool down = MgfPointer.Down;
            if (down)
            {
                st.pointerVersion++;
                SpawnRipple(MgfPointer.Position);
                MgfBridge.NotifyChanged();
            }

            if (phase == RunPhase.Title)
            {
                if (down)
                {
                    if (HitTitleCover(MgfPointer.Position)) BeginRun(true);
                    else { Refuse("점선을 따라 성도판 덮개를 누르시오."); PointToTitleCover(MgfPointer.Position); }
                }
                return;
            }

            if (phase == RunPhase.End)
            {
                if (down)
                {
                    if (HitRestart(MgfPointer.Position)) BeginRun(true);
                    else { Refuse("빛나는 성도판을 눌러 다시 출항하시오."); PulseRestart(); }
                }
                return;
            }

            if (phase == RunPhase.Feedback)
            {
                if (down) Refuse("돛이 핀 자국을 확인하고 있다.");
                return;
            }

            if (phase != RunPhase.Practice && phase != RunPhase.Playing) return;

            if (down)
            {
                if (HitCord(MgfPointer.Position))
                {
                    draggingCord = true; cordStart = MgfPointer.Position; PullCord(0f); AnticipateCord(); return;
                }
                int pin = HitPin(MgfPointer.Position);
                if (pin >= 0)
                {
                    TogglePin(pin); dragLastPin = pin; return;
                }
                Refuse("성도판의 핀을 누르거나 아래 돛끈을 당기시오.");
                PointToBoard();
            }

            if (MgfPointer.Held)
            {
                if (draggingCord) PullCord(Mathf.Max(0f, cordStart.y - MgfPointer.Position.y));
                else
                {
                    int pin = HitPin(MgfPointer.Position);
                    if (pin >= 0 && pin != dragLastPin) { TogglePin(pin); dragLastPin = pin; }
                }
            }

            if (MgfPointer.Up)
            {
                dragLastPin = -1;
                if (draggingCord)
                {
                    float pulled = Mathf.Max(0f, cordStart.y - MgfPointer.Position.y);
                    draggingCord = false; ReleaseCord();
                    if (pulled >= Mathf.Max(42f, Screen.height * .055f)) Submit();
                    else Refuse("돛끈을 아래로 더 길게 당겨 제출하시오.");
                }
            }
        }

        void TogglePin(int index)
        {
            if (index < 0 || index >= current.answerSet.Length) return;
            selected[index] = !selected[index];
            st.selectedPins += selected[index] ? 1 : -1;
            idleGuide = 0f; st.misconceptionId = "";
            SetPinVisual(index, false); RebuildThreads(); PinPressed(index, selected[index]);
            MgfSfx.Play("tap", selected[index] ? .20f : .11f); MgfBridge.NotifyChanged();
        }

        void Submit()
        {
            if (phase != RunPhase.Practice && phase != RunPhase.Playing) return;
            if (st.selectedPins == 0)
            {
                Refuse("경우를 먼저 표시하시오."); PointToBoard(); LooseCord(); return;
            }
            bool correct = DiceCaravanRules.SetsEqual(selected, current.answerSet);
            Resolve(correct, correct ? "" : DiceCaravanRules.Diagnose(current, selected));
        }

        void Resolve(bool correct, string misconception)
        {
            if (phase != RunPhase.Practice && phase != RunPhase.Playing) return;
            bool practice = phase == RunPhase.Practice;
            feedbackPractice = practice; feedbackCorrect = correct; feedbackClock = 0f;
            phase = RunPhase.Feedback; st.misconceptionId = misconception; draggingCord = false;
            Array.Clear(feedbackMissing, 0, feedbackMissing.Length);
            Array.Clear(feedbackExtra, 0, feedbackExtra.Length);
            for (int i = 0; i < current.answerSet.Length; i++)
            {
                feedbackMissing[i] = current.answerSet[i] && !selected[i];
                feedbackExtra[i] = !current.answerSet[i] && selected[i];
            }

            if (correct)
            {
                if (practice)
                {
                    st.score += 50;
                }
                else
                {
                    if (!attempted) { st.firstAttemptTotal++; st.firstAttemptCorrect++; }
                    st.solved++; st.combo++; st.score += 100 + st.combo * 20;
                    if (st.combo > 0 && st.combo % 2 == 0) st.windKnot = 1;
                }
                st.selectedPins = 0;
                CorrectSailEvent(current.AnswerCount, st.combo); ShowReveal(current.reveal, true);
                MgfSfx.Play("correct", .42f); MgfBridge.NotifyChanged();
            }
            else
            {
                if (!practice)
                {
                    if (!attempted) st.firstAttemptTotal++;
                    attempted = true; st.lives--; st.combo = 0;
                }
                WrongLeakEvent(); ShowReveal(WrongReason(misconception), false);
                MgfSfx.Play("wrong", .34f); MgfBridge.NotifyChanged();
                if (!practice && st.lives <= 0) endReason = "세 개의 바람등이 모두 꺼졌다";
            }
            RefreshAllPins(true); RefreshHud();
        }

        string WrongReason(string id)
        {
            switch (id)
            {
                case "ordered-pair-half": return "두 주사위는 서로 다르므로 (a,b)와 (b,a)를 따로 확인하시오.";
                case "leading-zero": return "0은 십의 자리에 올 수 없다. 두 자리 자연수만 남기시오.";
                case "ordered-duplicate": return "대표 2명은 역할이 같으므로 같은 쌍을 한 번만 세시오.";
                case "sum-product-swapped": return "각각 하나씩 고르면 곱하고, 서로 겹치지 않는 '또는'은 합하시오.";
                case "timeout": return "바람계가 끝났다. 표시한 경우를 다시 확인하시오.";
                default: return "빛나는 행과 열에서 빠진 핀과 더 꽂은 핀을 확인하시오.";
            }
        }

        void FinishFeedback()
        {
            HideReveal();
            if (feedbackCorrect)
            {
                if (feedbackPractice)
                {
                    phase = RunPhase.Playing; st.onboarding = false; st.phase = "playing"; runClock = 0f;
                    LoadMission(); SetScreen(); StartGuide();
                }
                else if (st.solved >= DiceCaravanRules.Goal)
                {
                    bool mastered = st.firstAttemptCorrect >= 4;
                    EndRun(mastered, mastered ? "여섯 장의 성도 돛을 완성했다" : "첫 시도 정답이 4개에 미치지 못했다");
                }
                else { phase = RunPhase.Playing; LoadMission(); }
            }
            else
            {
                if (!feedbackPractice && st.lives <= 0) EndRun(false, endReason);
                else
                {
                    phase = feedbackPractice ? RunPhase.Practice : RunPhase.Playing;
                    st.phase = "playing"; problemClock = 0f; idleGuide = 0f;
                    RefreshAllPins(false); StartGuide(); MgfBridge.NotifyChanged();
                }
            }
        }

        void EndRun(bool win, string reason)
        {
            phase = RunPhase.End; st.phase = win ? "clear" : "gameover"; endReason = reason;
            SetEndScreen(win, reason); MgfSfx.Play(win ? "win" : "lose", .40f); MgfBridge.NotifyChanged();
        }

        void ClearSelections()
        {
            Array.Clear(selected, 0, selected.Length);
            Array.Clear(feedbackMissing, 0, feedbackMissing.Length);
            Array.Clear(feedbackExtra, 0, feedbackExtra.Length);
            st.selectedPins = 0; dragLastPin = -1; draggingCord = false;
        }

        public void TestStart() { BeginRun(false); }

        public void TestAnswerCorrect()
        {
            if (phase != RunPhase.Practice && phase != RunPhase.Playing) BeginRun(false);
            ClearSelections();
            for (int i = 0; i < current.answerSet.Length; i++) { selected[i] = current.answerSet[i]; if (selected[i]) st.selectedPins++; }
            RefreshAllPins(false); RebuildThreads(); Submit();
        }

        public void TestAnswerWrong()
        {
            if (phase != RunPhase.Practice && phase != RunPhase.Playing) BeginRun(false);
            ClearSelections();
            int wrong = -1;
            for (int i = 0; i < current.answerSet.Length; i++) if (!current.answerSet[i]) { wrong = i; break; }
            if (wrong < 0) wrong = 0;
            selected[wrong] = true; st.selectedPins = 1; RefreshAllPins(false); RebuildThreads(); Submit();
        }

        public string StateJson() { return JsonUtility.ToJson(st); }
        public string ProblemBankJson() { return MgfJson.Bank(bank); }
    }
}
