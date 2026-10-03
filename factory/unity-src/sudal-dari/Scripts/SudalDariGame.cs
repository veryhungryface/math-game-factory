// 다리 놓는 수달 — 상태 머신과 유일한 답 입력(판자 손잡이 드래그 후 놓기).
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.SudalDari
{
    public partial class SudalDariGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int selectedLength = 1;
            public int bridgeIndex;
            public int combo;
            public int firstAttemptTotal;
            public int firstAttemptCorrect;
            public int attemptIndex;
            public int pointerVersion;
            public string problemId = "";
            public string prompt = "";
            public string misconceptionId = "";
            public bool onboarding;
            public bool lifeRecovered;
        }

        enum Phase { Title, Practice, Playing, Reveal, End }

        readonly State st = new State();
        readonly List<MgfProblem> bank = new List<MgfProblem>();
        Phase phase = Phase.Title;
        List<BridgeProblem> deck;
        BridgeProblem current;
        System.Random rng;
        int runSerial;
        bool firstAttempt;
        bool dragging;
        bool dragMoved;
        bool titleDragging;
        bool revealCorrect;
        bool revealFirst;
        bool timedOut;
        float revealClock;
        float tideLeft;
        float idleGuide;
        string endReason = "";

        void Awake()
        {
            MgfLook.Quality(34f);
            bank.AddRange(SudalRules.BuildBank());
            BuildWorld();
            BuildUi();
            Prewarm();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb = new StringBuilder("다리놓는수달물의넓이로다리를잇다판자끝을끌어길이를정하시오중학교2학년피타고라스정리직각삼각형빗변나머지한변가장긴변구명튜브밀물정답오답짧음길음다시계산물레방아마을입구완성게임종료처음부터연습둑버드나무갈대도토리");
            for (int i = 0; i < bank.Count; i++) { sb.Append(bank[i].prompt); sb.Append(bank[i].answer); }
            MgfText.Prewarm(sb.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(.05f, Time.deltaTime);
            HandleInput();

            if (phase == Phase.Practice)
            {
                idleGuide += dt;
                if (idleGuide >= 8f) { idleGuide = 0; ReplayGuide(true); }
            }
            else if (phase == Phase.Playing && current != null && current.band >= 2 && !dragging)
            {
                tideLeft -= dt;
                if (tideLeft <= 0f) TimeoutBridge();
            }
            else if (phase == Phase.Reveal)
            {
                revealClock += dt;
                if (revealClock >= (revealCorrect ? 2.1f : 1.75f)) FinishReveal();
            }

            UpdateWorld(dt);
            UpdateUi(dt);
        }

        void ShowTitle()
        {
            phase = Phase.Title;
            st.score = 0; st.lives = SudalRules.StartLives; st.level = 1; st.solved = 0;
            st.selectedLength = 4; st.bridgeIndex = 0; st.combo = 0;
            st.firstAttemptTotal = 0; st.firstAttemptCorrect = 0; st.attemptIndex = 0;
            st.pointerVersion = 0; st.problemId = ""; st.prompt = ""; st.misconceptionId = "";
            st.onboarding = false; st.lifeRecovered = false;
            endReason = ""; current = null; deck = null; dragging = false; titleDragging = false;
            ResetWorldForRun();
            SetSelected(4, false);
            SetScreen();
            MgfBridge.NotifyChanged();
        }

        void ResetRun()
        {
            runSerial++;
            rng = new System.Random(unchecked(Environment.TickCount ^ runSerial * 104729));
            deck = SudalRules.RunDeck(rng);
            st.score = 0; st.lives = SudalRules.StartLives; st.level = 1; st.solved = 0;
            st.bridgeIndex = 0; st.combo = 0; st.firstAttemptTotal = 0; st.firstAttemptCorrect = 0;
            st.attemptIndex = 0; st.misconceptionId = ""; st.lifeRecovered = false;
            endReason = ""; dragging = false; revealClock = 0; timedOut = false;
            ResetWorldForRun();
        }

        void StartPractice()
        {
            ResetRun();
            phase = Phase.Practice;
            current = SudalRules.Practice();
            st.onboarding = true; st.problemId = current.id; st.prompt = current.prompt;
            st.bridgeIndex = 0; firstAttempt = true; idleGuide = 0; tideLeft = SudalRules.TideSeconds;
            SetSelected(2, false);
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
            phase = Phase.Playing;
            st.onboarding = false;
            NextProblem();
            SetScreen();
            MgfBridge.NotifyChanged();
        }

        void BeginMainAfterPractice()
        {
            st.score = 0; st.solved = 0; st.bridgeIndex = 0; st.combo = 0;
            st.firstAttemptTotal = 0; st.firstAttemptCorrect = 0; st.attemptIndex = 0;
            st.lives = SudalRules.StartLives; st.misconceptionId = ""; st.onboarding = false;
            phase = Phase.Playing;
            NextProblem();
            SetScreen();
        }

        void NextProblem()
        {
            if (st.solved >= SudalRules.TargetBridges)
            {
                EndRun("clear");
                return;
            }
            st.bridgeIndex = st.solved;
            current = deck[st.bridgeIndex];
            st.problemId = current.id; st.prompt = current.prompt; st.level = current.band;
            st.attemptIndex = 0; st.misconceptionId = ""; firstAttempt = true; timedOut = false;
            tideLeft = SudalRules.TideSeconds;
            int start = current.kind == BridgeKind.CorrectFrame ? current.startLongest : 1 + rng.Next(SudalRules.MaxLength);
            if (start == current.answer) start = start == SudalRules.MaxLength ? start - 1 : start + 1;
            SetSelected(start, false);
            ResetWorldForProblem(current);
            RefreshProblemUi();
            MgfBridge.NotifyChanged();
        }

        void ResetSameProblem()
        {
            phase = Phase.Playing;
            timedOut = false;
            tideLeft = SudalRules.TideSeconds;
            int start = 1 + rng.Next(SudalRules.MaxLength);
            if (start == current.answer) start = start == SudalRules.MaxLength ? start - 1 : start + 1;
            SetSelected(start, false);
            ResetWorldForProblem(current);
            RefreshProblemUi();
        }

        void SetSelected(int length, bool notify)
        {
            int before = st.selectedLength;
            st.selectedLength = Math.Max(SudalRules.MinLength, Math.Min(SudalRules.MaxLength, length));
            SetPlankLength(st.selectedLength);
            RefreshLengthUi(before != st.selectedLength);
            if (notify) MgfBridge.NotifyChanged();
        }

        void SubmitBridge()
        {
            if ((phase != Phase.Practice && phase != Phase.Playing) || current == null) return;
            revealCorrect = st.selectedLength == current.answer;
            revealFirst = firstAttempt;

            if (phase == Phase.Practice)
            {
                if (revealCorrect) { st.score = 100; st.solved = 1; }
                else { st.misconceptionId = SudalRules.MisconceptionId(current, st.selectedLength); firstAttempt = false; }
            }
            else
            {
                if (firstAttempt)
                {
                    st.firstAttemptTotal++;
                    if (revealCorrect) st.firstAttemptCorrect++;
                }
                if (revealCorrect)
                {
                    st.combo++;
                    st.score += revealFirst ? 140 + st.combo * 10 : 55;
                    st.solved++;
                    if (st.combo >= 3 && !st.lifeRecovered && st.lives < SudalRules.StartLives)
                    {
                        st.lives++; st.lifeRecovered = true;
                    }
                }
                else
                {
                    st.combo = 0;
                    st.lives--;
                    st.attemptIndex++;
                    st.misconceptionId = timedOut ? "tide_timeout" : SudalRules.MisconceptionId(current, st.selectedLength);
                    firstAttempt = false;
                }
            }

            phase = Phase.Reveal;
            revealClock = 0; dragging = false;
            BeginRevealVisual(revealCorrect, revealFirst, timedOut);
            RefreshHudImmediate();
            MgfBridge.NotifyChanged();
        }

        void TimeoutBridge()
        {
            if (phase != Phase.Playing || current == null) return;
            timedOut = true;
            int wrong = current.answer == 1 ? 2 : 1;
            SetSelected(wrong, false);
            SubmitBridge();
        }

        void FinishReveal()
        {
            if (phase != Phase.Reveal) return;
            CompleteRevealVisual(revealCorrect);
            if (current != null && current.band == 0)
            {
                if (revealCorrect) BeginMainAfterPractice();
                else
                {
                    phase = Phase.Practice;
                    SetSelected(2, false);
                    ResetWorldForProblem(current);
                    RefreshProblemUi();
                    ReplayGuide(true);
                }
            }
            else if (!revealCorrect)
            {
                if (st.lives <= 0) EndRun("river");
                else { ResetSameProblem(); ShowWrongReason(st.misconceptionId); }
            }
            else
            {
                phase = Phase.Playing;
                NextProblem();
            }
            SetScreen();
            MgfBridge.NotifyChanged();
        }

        void FinishPendingReveal()
        {
            if (phase == Phase.Reveal) { revealClock = 99; FinishReveal(); }
        }

        void EndRun(string reason)
        {
            if (phase == Phase.End) return;
            phase = Phase.End; dragging = false; endReason = reason;
            int best = PlayerPrefs.GetInt("sudal-dari.best", 0);
            if (st.score > best) { PlayerPrefs.SetInt("sudal-dari.best", st.score); PlayerPrefs.Save(); }
            ShowEnd(reason);
            SetScreen();
            MgfSfx.Play(reason == "clear" ? "win" : "lose", .58f);
            MgfBridge.NotifyChanged();
        }

        void HandleInput()
        {
            if (MgfPointer.Down)
            {
                st.pointerVersion++;
                SpawnTapRipple(MgfPointer.Position);
                if (phase == Phase.Title)
                {
                    if (IsTitleHandleZone(MgfPointer.Position))
                    {
                        titleDragging = true; dragMoved = false; BeginTitleGrab(); MgfSfx.Play("tap", .22f);
                    }
                    else RefuseInput("모루가 내민 판자 끝을 잡아당기시오");
                    MgfBridge.NotifyChanged();
                    return;
                }
                if (phase == Phase.End) { ShowTitle(); return; }
                if (phase == Phase.Reveal) { RefuseInput("물이 차오르는 동안 잠깐 기다리시오"); MgfBridge.NotifyChanged(); return; }
                if (phase != Phase.Practice && phase != Phase.Playing) return;
                if (IsTrackZone(MgfPointer.Position))
                {
                    dragging = true; dragMoved = false; idleGuide = 0;
                    int before = st.selectedLength;
                    SetSelected(LengthFromPointer(MgfPointer.Position), true);
                    dragMoved = before != st.selectedLength;
                    HandleGrab(true);
                    MgfSfx.Play("tap", .22f);
                }
                else
                {
                    RefuseInput("주황 손잡이를 끌어 판자 길이를 정하시오");
                    MgfBridge.NotifyChanged();
                }
            }

            if (MgfPointer.Held && titleDragging)
            {
                dragMoved = true;
                UpdateTitleGrab(MgfPointer.Position);
            }
            if (MgfPointer.Up && titleDragging)
            {
                titleDragging = false; EndTitleGrab();
                // A short press makes Moru pull the plank for the player; a
                // longer gesture preserves the tactile drag.  Both still use
                // the in-world plank rather than a detached menu button.
                StartPractice();
                return;
            }

            if (MgfPointer.Held && dragging)
            {
                int before = st.selectedLength;
                int next = LengthFromPointer(MgfPointer.Position);
                SetSelected(next, before != next);
                if (before != next) { dragMoved = true; TickPlank(); }
                UpdatePlankTrail();
            }
            if (MgfPointer.Up && dragging)
            {
                dragging = false; HandleGrab(false);
                if (!dragMoved) { RefuseInput("손잡이를 다른 눈금까지 끈 뒤 놓으시오"); MgfBridge.NotifyChanged(); return; }
                SubmitBridge();
            }
        }

        public void TestStart()
        {
            FinishPendingReveal();
            StartRunDirect();
        }

        public void TestAnswerCorrect()
        {
            FinishPendingReveal();
            if (phase != Phase.Playing || current == null) return;
            SetSelected(current.answer, false);
            SubmitBridge();
        }

        public void TestAnswerWrong()
        {
            FinishPendingReveal();
            if (phase != Phase.Playing || current == null) return;
            int wrong = current.wrongSum > 0 ? current.wrongSum : current.wrongAddSquares > 0 ? current.wrongAddSquares : current.answer == 25 ? 24 : current.answer + 1;
            if (wrong == current.answer) wrong = current.answer == 1 ? 2 : 1;
            SetSelected(wrong, false);
            SubmitBridge();
        }

        public string StateJson()
        {
            st.phase = phase == Phase.Title ? "title" : phase == Phase.End ? (endReason == "clear" ? "clear" : "gameover") : "playing";
            st.onboarding = phase == Phase.Practice;
            st.level = current == null ? 1 : Math.Max(1, current.band);
            st.lives = Math.Max(0, st.lives);
            st.problemId = current == null ? "" : current.id;
            st.prompt = current == null ? "" : current.prompt;
            return JsonUtility.ToJson(st);
        }

        public string ProblemBankJson() => MgfJson.Bank(bank);
    }
}
