// 그림자 극장 — 한 인형 레일 밀기와 황동 막 손잡이로만 답하는 중심투영 퍼즐.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.GeurimjaGeukjang
{
    public partial class ShadowTheaterGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int selectedD = 3;
            public int act = 1;
            public int combo;
            public int firstAttemptTotal;
            public int firstAttemptCorrect;
            public int lateFirstCorrect;
            public int pointerVersion;
            public int attemptIndex;
            public string problemId = "";
            public string prompt = "";
            public string problemKind = "";
            public string targetRatio = "";
            public string misconceptionId = "";
            public bool onboarding;
            public bool frozen;
        }

        enum GamePhase { Title, Practice, Playing, Reveal, End }

        readonly State st = new State();
        readonly List<MgfProblem> bank = new List<MgfProblem>();
        List<ShadowProblem> deck;
        ShadowProblem current;
        System.Random rng;
        GamePhase gamePhase = GamePhase.Title;
        int runSerial;
        int firstOffset;
        int deckIndex;
        bool firstAttempt;
        bool revealCorrect;
        bool revealPractice;
        bool revealFirst;
        bool puppetDragging;
        bool curtainDragging;
        bool titlePressed;
        bool dragMoved;
        int dragStartD;
        Vector2 dragStart;
        float curtainPull;
        float revealClock;
        float idleGuide;
        float runLeft;
        float titleClock;
        string endReason = "";

        void Awake()
        {
            MgfLook.Quality(34f);
            ShadowRules.ExhaustiveInvariantCheck();
            bank.AddRange(ShadowRules.BuildBank());
            // Keep the nine first-answer positions exactly balanced across fresh runs.
            // This also makes the 200-run blind-policy audit reproducible.
            firstOffset = 0;
            BuildWorld();
            BuildUi();
            PrewarmText();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void PrewarmText()
        {
            var chars = new HashSet<char>();
            string fixedText = "그림자극장인형을밀어비를맞춰중학교2학년도형의닮음등불에서인형까지의거리를정해주문한비의그림자를만드시오빛을켜고막올리기황동손잡이를아래로당겨제출하시오△OAB∽△OCDOAOCcm길이의비넓이의비첫시도퓨즈막앙코르폐막다시공연하기맞았다선택한목표수리완료";
            for (int i = 0; i < fixedText.Length; i++) chars.Add(fixedText[i]);
            for (int i = 0; i < bank.Count; i++)
            {
                string s = bank[i].prompt + bank[i].answer + bank[i].unitConcept;
                for (int j = 0; j < s.Length; j++) chars.Add(s[j]);
            }
            var sb = new StringBuilder(chars.Count);
            foreach (char c in chars) sb.Append(c);
            MgfText.Prewarm(sb.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(.05f, Time.deltaTime);
            titleClock += dt;
            HandleInput();

            if (gamePhase == GamePhase.Practice)
            {
                idleGuide += dt;
                if (idleGuide >= 8f) { idleGuide = 0f; ReplayGuide(true); }
            }
            else if (gamePhase == GamePhase.Playing)
            {
                runLeft = Mathf.Max(0f, runLeft - dt);
                idleGuide += dt;
                if (idleGuide >= 8f) { idleGuide = 0f; ReplayGuide(true); }
                if (runLeft <= 0f) EndRun("time");
            }
            else if (gamePhase == GamePhase.Reveal)
            {
                revealClock += dt;
                if (revealClock >= (revealCorrect ? 1.62f : 1.42f)) FinishReveal();
            }

            UpdateWorld(dt);
            UpdateUi(dt);
        }

        void ShowTitle()
        {
            gamePhase = GamePhase.Title;
            deck = null; current = null; endReason = "";
            puppetDragging = false; curtainDragging = false; titlePressed = false;
            st.score = 0; st.lives = ShadowRules.StartLives; st.level = 1; st.solved = 0;
            st.selectedD = 3; st.act = 1; st.combo = 0; st.firstAttemptTotal = 0;
            st.firstAttemptCorrect = 0; st.lateFirstCorrect = 0; st.attemptIndex = 0;
            st.problemId = ""; st.prompt = ""; st.problemKind = ""; st.targetRatio = "";
            st.misconceptionId = ""; st.onboarding = false; st.frozen = false;
            curtainPull = 0; revealClock = 0; idleGuide = 0;
            ResetWorldForRun();
            SetPhaseVisuals();
            MgfBridge.NotifyChanged();
        }

        void ResetRun()
        {
            runSerial++;
            rng = new System.Random(unchecked(Environment.TickCount ^ runSerial * 104729 ^ firstOffset * 7907));
            deck = ShadowRules.RunDeck(rng, ShadowRules.FirstD(runSerial, firstOffset));
            deckIndex = 0; runLeft = ShadowRules.RunSeconds;
            st.score = 0; st.lives = ShadowRules.StartLives; st.level = 1; st.solved = 0;
            st.act = 1; st.combo = 0; st.firstAttemptTotal = 0; st.firstAttemptCorrect = 0;
            st.lateFirstCorrect = 0; st.attemptIndex = 0; st.misconceptionId = "";
            endReason = ""; revealClock = 0; curtainPull = 0; idleGuide = 0;
            ResetWorldForRun();
        }

        void StartPractice()
        {
            ResetRun();
            gamePhase = GamePhase.Practice;
            st.onboarding = true; st.frozen = true;
            current = ShadowRules.Practice();
            firstAttempt = true;
            SetSelectedD(3, false);
            BindCurrent();
            SetPhaseVisuals();
            ResetWorldForProblem(current);
            RefreshProblemUi();
            ReplayGuide(false);
            ShowToast("인형을 6 cm까지 밀고 막 손잡이를 아래로 당기시오", 3.2f);
            MgfSfx.Play("whoosh", .34f);
            MgfBridge.NotifyChanged();
        }

        void StartRunDirect()
        {
            ResetRun();
            gamePhase = GamePhase.Playing;
            st.onboarding = false; st.frozen = false;
            LoadNextProblem();
            SetPhaseVisuals();
            MgfBridge.NotifyChanged();
        }

        void BeginMainAfterPractice()
        {
            PlayerPrefs.SetInt("geurimja-geukjang.onboardingSeen", 1);
            PlayerPrefs.Save();
            st.score = 0; st.lives = ShadowRules.StartLives; st.solved = 0; st.combo = 0;
            st.firstAttemptTotal = 0; st.firstAttemptCorrect = 0; st.lateFirstCorrect = 0;
            st.attemptIndex = 0; st.misconceptionId = "";
            deckIndex = 0; runLeft = ShadowRules.RunSeconds;
            gamePhase = GamePhase.Playing; st.onboarding = false; st.frozen = false;
            LoadNextProblem();
            SetPhaseVisuals();
            ShowToast("실전 · 계산한 거리로 밀고 막 손잡이를 당기시오", 3f);
        }

        void StartFromTitle()
        {
            if (PlayerPrefs.GetInt("geurimja-geukjang.onboardingSeen", 0) == 1) StartRunDirect();
            else StartPractice();
        }

        void LoadNextProblem()
        {
            if (deck == null || deckIndex >= deck.Count) { EndRun("mastery"); return; }
            current = deck[deckIndex];
            st.act = deckIndex + 1;
            firstAttempt = true;
            int initial = current.answerD == 3 ? 4 : 3;
            SetSelectedD(initial, false);
            BindCurrent();
            ResetWorldForProblem(current);
            RefreshProblemUi();
            idleGuide = 0;
        }

        void BindCurrent()
        {
            st.problemId = current == null ? "" : current.id;
            st.prompt = current == null ? "" : current.prompt;
            st.problemKind = current == null ? "" : current.kind.ToString();
            st.targetRatio = current == null ? "" : current.targetRatio;
            st.level = current == null ? 1 : current.band;
        }

        void SetSelectedD(int value, bool notify)
        {
            value = Math.Max(ShadowRules.MinD, Math.Min(ShadowRules.MaxD, value));
            bool changed = value != st.selectedD;
            st.selectedD = value;
            UpdatePuppetPosition(value, changed);
            RefreshDistanceUi();
            if (changed && notify)
            {
                MgfSfx.Play("tap", .13f);
                MgfBridge.NotifyChanged();
            }
        }

        void SubmitCurtain()
        {
            if ((gamePhase != GamePhase.Practice && gamePhase != GamePhase.Playing) || current == null) return;
            revealPractice = gamePhase == GamePhase.Practice;
            revealCorrect = ShadowRules.IsCorrect(current, st.selectedD);
            revealFirst = firstAttempt;
            if (revealPractice)
            {
                if (revealCorrect) { st.score = 100; st.solved = 1; }
                else st.misconceptionId = ShadowRules.MisconceptionId(current, st.selectedD);
            }
            else
            {
                if (firstAttempt)
                {
                    st.firstAttemptTotal++;
                    if (revealCorrect)
                    {
                        st.firstAttemptCorrect++;
                        if (deckIndex >= 5) st.lateFirstCorrect++;
                    }
                    firstAttempt = false;
                }
                if (revealCorrect)
                {
                    st.combo++;
                    st.solved++;
                    st.score += revealFirst ? 160 + st.combo * 20 : 70;
                }
                else
                {
                    st.combo = 0;
                    st.lives = Math.Max(0, st.lives - 1);
                    st.attemptIndex++;
                    st.misconceptionId = ShadowRules.MisconceptionId(current, st.selectedD);
                }
            }
            gamePhase = GamePhase.Reveal;
            st.frozen = true;
            revealClock = 0;
            curtainPull = 1f;
            BeginRevealVisual(revealCorrect, revealPractice, revealFirst);
            RefreshProblemUi();
            SetPhaseVisuals();
            MgfBridge.NotifyChanged();
        }

        void FinishReveal()
        {
            if (gamePhase != GamePhase.Reveal) return;
            CompleteRevealVisual(revealCorrect);
            curtainPull = 0;
            if (revealPractice)
            {
                if (revealCorrect) BeginMainAfterPractice();
                else
                {
                    gamePhase = GamePhase.Practice;
                    st.frozen = true;
                    SetPhaseVisuals();
                    ResetWorldForProblem(current);
                    RefreshProblemUi();
                    ReplayGuide(true);
                }
            }
            else if (!revealCorrect)
            {
                if (st.lives <= 0) EndRun("fuses");
                else
                {
                    gamePhase = GamePhase.Playing;
                    st.frozen = false;
                    SetPhaseVisuals();
                    ShowWrongReason(st.misconceptionId);
                    RefreshProblemUi();
                }
            }
            else
            {
                deckIndex++;
                if (st.solved >= ShadowRules.TargetActs)
                {
                    bool mastery = st.firstAttemptCorrect >= 6 && st.lateFirstCorrect >= 2;
                    EndRun(mastery ? "clear" : "mastery");
                }
                else
                {
                    gamePhase = GamePhase.Playing;
                    st.frozen = false;
                    LoadNextProblem();
                    SetPhaseVisuals();
                }
            }
            MgfBridge.NotifyChanged();
        }

        void FinishPendingReveal()
        {
            if (gamePhase == GamePhase.Reveal) { revealClock = 99f; FinishReveal(); }
        }

        void EndRun(string reason)
        {
            if (gamePhase == GamePhase.End) return;
            gamePhase = GamePhase.End;
            endReason = reason;
            st.frozen = true;
            puppetDragging = false; curtainDragging = false;
            int best = PlayerPrefs.GetInt("geurimja-geukjang.best", 0);
            if (st.firstAttemptCorrect > best)
            {
                PlayerPrefs.SetInt("geurimja-geukjang.best", st.firstAttemptCorrect);
                PlayerPrefs.Save();
            }
            ShowEnd(reason);
            SetPhaseVisuals();
            if (reason == "clear") BeginFinale();
            MgfSfx.Play(reason == "clear" ? "win" : "lose", .55f);
            MgfBridge.NotifyChanged();
        }

        void HandleInput()
        {
            Vector2 pos = MgfPointer.Position;
            if (MgfPointer.Down)
            {
                st.pointerVersion++;
                SpawnTapRipple(pos);
                if (gamePhase == GamePhase.Title)
                {
                    titlePressed = IsTitleStartZone(pos);
                    if (titlePressed) { BeginTitlePress(); MgfSfx.Play("tap", .2f); }
                    else RefuseInput("아래 황동 조광 레버를 눌러 빛을 켜시오");
                    MgfBridge.NotifyChanged();
                    return;
                }
                if (gamePhase == GamePhase.End)
                {
                    if (IsEndRestartZone(pos)) StartRunDirect();
                    else RefuseInput("아래 ‘다시 공연하기’ 손잡이를 누르시오");
                    MgfBridge.NotifyChanged();
                    return;
                }
                if (gamePhase == GamePhase.Reveal)
                {
                    RefuseInput("종이 막이 홈에 들어갈 때까지 잠깐 기다리시오");
                    MgfBridge.NotifyChanged();
                    return;
                }
                if (gamePhase != GamePhase.Practice && gamePhase != GamePhase.Playing) return;
                idleGuide = 0;
                if (IsCurtainZone(pos))
                {
                    curtainDragging = true; dragStart = pos; curtainPull = 0f;
                    BeginCurtainGrab(); MgfSfx.Play("tap", .16f);
                }
                else if (IsRailZone(pos))
                {
                    puppetDragging = true; dragMoved = false; dragStart = pos; dragStartD = st.selectedD;
                    int before = st.selectedD; SetSelectedD(DFromPointer(pos), true); dragMoved = before != st.selectedD;
                    BeginPuppetGrab();
                }
                else
                {
                    RefuseInput("검은 인형 받침을 밀거나 오른쪽 막 손잡이를 당기시오");
                    ReplayGuide(true);
                }
            }

            if (MgfPointer.Held && titlePressed) UpdateTitlePress(pos);
            if (MgfPointer.Up && titlePressed)
            {
                titlePressed = false; EndTitlePress(); StartFromTitle(); return;
            }

            if (MgfPointer.Held && puppetDragging)
            {
                int before = st.selectedD;
                int next = DFromPointer(pos);
                SetSelectedD(next, before != next);
                if (before != next) { dragMoved = true; UpdatePuppetTrail(); }
            }
            if (MgfPointer.Up && puppetDragging)
            {
                puppetDragging = false; EndPuppetGrab();
                if (!dragMoved)
                {
                    RefuseInput("인형을 다른 눈금까지 민 뒤 놓으시오");
                    ReplayGuide(true);
                }
                MgfBridge.NotifyChanged();
            }

            if (MgfPointer.Held && curtainDragging)
            {
                float need = Mathf.Max(64f, Screen.height * .075f);
                curtainPull = Mathf.Clamp01((dragStart.y - pos.y) / need);
                UpdateCurtainPull(curtainPull);
            }
            if (MgfPointer.Up && curtainDragging)
            {
                curtainDragging = false; EndCurtainGrab();
                if (curtainPull >= .94f) SubmitCurtain();
                else
                {
                    curtainPull = 0; UpdateCurtainPull(0);
                    RefuseInput("황동 막 손잡이를 64 px 이상 아래로 당기시오");
                    MgfSfx.Play("wrong", .16f);
                    MgfBridge.NotifyChanged();
                }
            }
        }

        // ── IMgfGame: 실제 SubmitCurtain 판정 경로를 그대로 탄다.
        public void TestStart() { FinishPendingReveal(); StartRunDirect(); }

        public void TestAnswerCorrect()
        {
            FinishPendingReveal();
            if (gamePhase != GamePhase.Playing || current == null) StartRunDirect();
            SetSelectedD(current.answerD, false);
            SubmitCurtain();
        }

        public void TestAnswerWrong()
        {
            FinishPendingReveal();
            if (gamePhase != GamePhase.Playing || current == null) StartRunDirect();
            int wrong = current.wrongAreaLinear > 0 ? current.wrongAreaLinear :
                        current.wrongDistanceScale > 0 ? current.wrongDistanceScale :
                        current.answerD == ShadowRules.MaxD ? current.answerD - 1 : current.answerD + 1;
            if (wrong == current.answerD) wrong = current.answerD == ShadowRules.MinD ? 3 : 2;
            SetSelectedD(wrong, false);
            SubmitCurtain();
        }

        public string StateJson()
        {
            st.phase = gamePhase == GamePhase.Title ? "title" : gamePhase == GamePhase.End ? (endReason == "clear" ? "clear" : "gameover") : "playing";
            st.onboarding = gamePhase == GamePhase.Practice;
            st.frozen = gamePhase == GamePhase.Practice || gamePhase == GamePhase.Reveal || gamePhase == GamePhase.End;
            st.lives = Math.Max(0, st.lives);
            BindCurrent();
            return JsonUtility.ToJson(st);
        }

        public string ProblemBankJson() => MgfJson.Bank(bank);
    }
}
