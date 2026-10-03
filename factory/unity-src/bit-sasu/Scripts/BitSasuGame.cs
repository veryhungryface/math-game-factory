// 빛 사수 — 렌즈 손잡이를 드래그하고 놓는 한 발 조준 루프.
// 하단 선택지나 확인 버튼은 없다. 손잡이를 놓는 행위만 정답 제출이다.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.BitSasu
{
    public partial class BitSasuGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int selectedAngle = 25;
            public string ratioNumeratorId = "";
            public string ratioDenominatorId = "";
            public string problemId = "";
            public int firstAttemptTotal;
            public int firstAttemptCorrect;
            public int attemptIndex;
            public int crackedPlates;
            public int presentedCount;
            public int noSubmitCount;
            public int tablePage = 1;
            public string tableFunction = "tan";
            public int shots;
            public int pointerVersion;
            public string misconceptionId = "";
            public bool onboarding;
        }

        readonly State st = new State();
        readonly List<MgfProblem> bank = new List<MgfProblem>();
        enum Phase { Title, Practice, Playing, Shot, End }
        Phase phase = Phase.Title;

        LightProblem current;
        System.Random rng;
        int runSerial;
        float runLeft;
        bool firstAttempt;
        bool shotCorrect;
        bool shotWasFirst;
        float shotT;
        string endReason = "";
        int consecutiveFirst;
        int ratioStart = -1;
        bool handleDrag;
        float dragStartY;
        int dragStartAngle;
        int bestFirst;
        int bestScore;
        float idleGuide;

        void Awake()
        {
            MgfLook.Quality(34f);
            BuildBank();
            BuildWorld();
            BuildUi();
            Prewarm();
            bestFirst = PlayerPrefs.GetInt("bit-sasu.bestFirst", 0);
            bestScore = PlayerPrefs.GetInt("bit-sasu.bestScore", 0);
            ShowTitle();
            MgfBridge.Register(this);
        }

        void BuildBank()
        {
            bank.AddRange(BitSasuRules.BuildBank());
        }

        void Prewarm()
        {
            var sb = new StringBuilder(
                "빛사수등대를돌려비춰라렌즈손잡이를끌어각도를맞추고놓으시오삼각비표목표보호판첫시도정답연습본판표적안개전체높이렌즈높이수평거리대변이웃변빗변기준각직각높이차분자분모몫근삿값구하시오이용하여각의크기를단위로중학교3학년삼각비빛켜기다시조준도착하는중위로빗나감아래로빗나감표의행과선택한두변을확인하시오성공재도전시간종료보호판소진해안점등처음부터잘못잡았어요붉은손잡이를잡으시오");
            for (int i = 0; i < bank.Count; i++) { sb.Append(bank[i].prompt); sb.Append(bank[i].answer); }
            MgfText.Prewarm(sb.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(0.05f, Time.deltaTime);
            HandleInput();

            if (phase == Phase.Playing)
            {
                runLeft -= dt;
                idleGuide += dt;
                if (runLeft <= 0f) EndRun("time");
            }
            else if (phase == Phase.Practice)
            {
                idleGuide += dt;
                if (idleGuide > 8f) ReplayGuide();
            }
            else if (phase == Phase.Shot)
            {
                shotT += dt;
                if (shotT >= 1.28f) FinishShot();
            }

            UpdateWorld(dt);
            UpdateUi(dt);
        }

        void ShowTitle()
        {
            phase = Phase.Title;
            current = BitSasuRules.Practice();
            st.score = 0; st.lives = BitSasuRules.StartLives; st.level = 1; st.solved = 0;
            st.selectedAngle = 25; st.problemId = current.id; st.onboarding = false;
            st.ratioNumeratorId = ""; st.ratioDenominatorId = "";
            endReason = ""; idleGuide = 0; handleDrag = false;
            SetSelectedAngle(25, false);
            SetScreen();
            MgfBridge.NotifyChanged();
        }

        void StartPractice()
        {
            ResetRunState();
            phase = Phase.Practice;
            current = BitSasuRules.Practice();
            st.problemId = current.id;
            st.selectedAngle = 25;
            st.tablePage = 1;
            st.tableFunction = "tan";
            st.onboarding = true;
            firstAttempt = true;
            idleGuide = 0;
            SetSelectedAngle(25, false);
            RefreshProblemUi();
            SetScreen();
            BeginSplitReveal();
            MgfBridge.NotifyChanged();
        }

        void StartRunDirect()
        {
            ResetRunState();
            phase = Phase.Playing;
            st.onboarding = false;
            NextProblem();
            SetScreen();
            MgfBridge.NotifyChanged();
        }

        void ResetRunState()
        {
            st.score = 0;
            st.lives = BitSasuRules.StartLives;
            st.level = 1;
            st.solved = 0;
            st.firstAttemptTotal = 0;
            st.firstAttemptCorrect = 0;
            st.attemptIndex = 0;
            st.crackedPlates = 0;
            st.presentedCount = 0;
            st.noSubmitCount = 0;
            st.shots = 0;
            st.pointerVersion = 0;
            st.misconceptionId = "";
            st.ratioNumeratorId = "";
            st.ratioDenominatorId = "";
            ratioStart = -1;
            runLeft = BitSasuRules.RunSeconds;
            runSerial++;
            rng = new System.Random(unchecked(Environment.TickCount + runSerial * 7919));
            consecutiveFirst = 0;
            firstAttempt = true;
            shotT = 0;
            endReason = "";
            handleDrag = false;
            ResetWorldForRun();
        }

        void NextProblem()
        {
            if (st.solved >= BitSasuRules.TargetCount)
            {
                EndRun(st.firstAttemptCorrect >= 7 && st.lives >= 1 ? "clear" : "mastery");
                return;
            }
            int band = Math.Min(3, st.solved / 3 + 1);
            int angle = BitSasuRules.MinAngle + rng.Next(BitSasuRules.BucketCount);
            TrigKind trig = band == 3 ? TrigKind.Tan : (TrigKind)rng.Next(3);
            current = BitSasuRules.Make(angle, band, trig, rng.Next(1, 100000));
            st.problemId = current.id;
            st.level = band;
            st.attemptIndex = 0;
            st.presentedCount++;
            st.misconceptionId = "";
            st.ratioNumeratorId = "";
            st.ratioDenominatorId = "";
            ratioStart = -1;
            firstAttempt = true;
            idleGuide = 0;
            // 직전 답을 그대로 내는 정책을 막기 위해 시작값은 정답과 다른 결정적 오프셋.
            int start = 25 + ((st.presentedCount * 11) % 38);
            if (start == current.angle) start = start == 74 ? 73 : start + 1;
            SetSelectedAngle(start, false);
            st.tableFunction = BitSasuRules.TrigName(current.trig);
            st.tablePage = current.angle >= 45 ? 1 : 0;
            RefreshProblemUi();
            ResetWorldForProblem();
            MgfBridge.NotifyChanged();
        }

        void SubmitShot()
        {
            if ((phase != Phase.Practice && phase != Phase.Playing) || current == null) return;
            shotCorrect = st.selectedAngle == current.angle;
            shotWasFirst = firstAttempt;
            st.shots++;
            if (phase == Phase.Playing && firstAttempt)
            {
                st.firstAttemptTotal++;
                if (shotCorrect) st.firstAttemptCorrect++;
            }
            if (phase == Phase.Playing && !shotCorrect)
            {
                st.lives--;
                st.crackedPlates = BitSasuRules.StartLives - Math.Max(0, st.lives);
                st.misconceptionId = BitSasuRules.IdentifyMisconception(current, st.selectedAngle);
                firstAttempt = false;
                st.attemptIndex++;
                consecutiveFirst = 0;
            }
            else if (phase == Phase.Playing && shotCorrect)
            {
                st.score += shotWasFirst ? 100 : 25;
                st.solved++;
                if (shotWasFirst) consecutiveFirst++;
                else consecutiveFirst = 0;
            }
            phase = Phase.Shot;
            shotT = 0f;
            handleDrag = false;
            BeginShotVisual(shotCorrect, shotWasFirst);
            RefreshHudImmediate();
            MgfBridge.NotifyChanged();
        }

        void FinishShot()
        {
            if (phase != Phase.Shot) return;
            CompleteShotVisual(shotCorrect);
            if (current != null && current.band == 0)
            {
                if (shotCorrect)
                {
                    phase = Phase.Playing;
                    st.onboarding = false;
                    st.score = 0; st.solved = 0; st.firstAttemptTotal = 0; st.firstAttemptCorrect = 0;
                    st.lives = BitSasuRules.StartLives; st.crackedPlates = 0;
                    runLeft = BitSasuRules.RunSeconds;
                    NextProblem();
                }
                else
                {
                    phase = Phase.Practice;
                    idleGuide = 0;
                    ShowPracticeCorrection();
                }
            }
            else if (!shotCorrect)
            {
                if (st.lives <= 0) EndRun("plates");
                else
                {
                    phase = Phase.Playing;
                    RefreshProblemUi();
                    ShowWrongReason(st.misconceptionId);
                }
            }
            else
            {
                phase = Phase.Playing;
                if (consecutiveFirst > 0 && consecutiveFirst % 3 == 0) LightRailing(consecutiveFirst / 3);
                NextProblem();
            }
            SetScreen();
            MgfBridge.NotifyChanged();
        }

        void FinishPendingShot()
        {
            if (phase == Phase.Shot) { shotT = 2f; FinishShot(); }
        }

        void EndRun(string reason)
        {
            if (phase == Phase.End) return;
            phase = Phase.End;
            handleDrag = false;
            endReason = reason;
            st.noSubmitCount = Math.Max(0, st.presentedCount - st.firstAttemptTotal);
            if (st.firstAttemptCorrect > bestFirst) { bestFirst = st.firstAttemptCorrect; PlayerPrefs.SetInt("bit-sasu.bestFirst", bestFirst); }
            if (st.score > bestScore) { bestScore = st.score; PlayerPrefs.SetInt("bit-sasu.bestScore", bestScore); }
            PlayerPrefs.Save();
            ShowEnd(reason);
            SetScreen();
            MgfSfx.Play(reason == "clear" ? "win" : "lose", 0.55f);
            MgfBridge.NotifyChanged();
        }

        void SetSelectedAngle(int angle, bool notify)
        {
            st.selectedAngle = Math.Max(BitSasuRules.MinAngle, Math.Min(BitSasuRules.MaxAngle, angle));
            SetLensAngle(st.selectedAngle);
            RefreshAngleUi();
            if (notify) MgfBridge.NotifyChanged();
        }

        void HandleInput()
        {
            if (MgfPointer.Down)
            {
                st.pointerVersion++;
                if (phase == Phase.Title)
                {
                    PressTitle();
                    MgfBridge.NotifyChanged();
                    return;
                }
                if (phase == Phase.End)
                {
                    PressEnd();
                    MgfBridge.NotifyChanged();
                    return;
                }
                if (phase == Phase.Shot)
                {
                    RefuseInput("빛이 도착하는 중");
                    MgfBridge.NotifyChanged();
                    return;
                }
                if (phase != Phase.Practice && phase != Phase.Playing) return;

                if (HitTableFunction(MgfPointer.Position))
                {
                    CycleTableFunction();
                    MgfSfx.Play("tap", 0.25f);
                    MgfBridge.NotifyChanged();
                    return;
                }
                if (HitTablePage(MgfPointer.Position))
                {
                    st.tablePage = 1 - st.tablePage;
                    RefreshTable();
                    MgfSfx.Play("tap", 0.25f);
                    MgfBridge.NotifyChanged();
                    return;
                }
                int chip = HitRatioChip(MgfPointer.Position);
                if (chip >= 0)
                {
                    ratioStart = chip;
                    BeginRatioDrag(chip);
                    MgfSfx.Play("tap", 0.22f);
                    MgfBridge.NotifyChanged();
                    return;
                }
                if (IsHandleZone(MgfPointer.Position))
                {
                    handleDrag = true;
                    dragStartY = MgfPointer.Position.y;
                    dragStartAngle = st.selectedAngle;
                    idleGuide = 0;
                    HandleGrab(true);
                    MgfSfx.Play("tap", 0.25f);
                    MgfBridge.NotifyChanged();
                    return;
                }
                RefuseInput("붉은 손잡이를 잡으시오");
                MgfBridge.NotifyChanged();
            }

            if (MgfPointer.Held && handleDrag)
            {
                int a = dragStartAngle + Mathf.RoundToInt((MgfPointer.Position.y - dragStartY) / 4f);
                if (a != st.selectedAngle)
                {
                    int before = st.selectedAngle;
                    SetSelectedAngle(a, true);
                    if (before != st.selectedAngle) TickLens();
                }
                UpdateHandleTrail();
            }

            if (MgfPointer.Up)
            {
                if (ratioStart >= 0)
                {
                    int target = HitRatioChip(MgfPointer.Position);
                    FinishRatioDrag(ratioStart, target);
                    ratioStart = -1;
                    MgfBridge.NotifyChanged();
                    return;
                }
                if (handleDrag)
                {
                    handleDrag = false;
                    HandleGrab(false);
                    SubmitShot();
                }
            }
        }

        void CycleTableFunction()
        {
            st.tableFunction = st.tableFunction == "sin" ? "cos" : st.tableFunction == "cos" ? "tan" : "sin";
            RefreshTable();
        }

        void FinishRatioDrag(int from, int to)
        {
            if (to < 0 || to == from)
            {
                ShowRatioResult(from, from);
                RefuseInput("다른 길이까지 이어 보시오");
                return;
            }
            st.ratioNumeratorId = RatioChipId(from);
            st.ratioDenominatorId = RatioChipId(to);
            ShowRatioResult(from, to);
        }

        public void TestStart()
        {
            // 자동 검증이 실제 pointer 발사의 결과를 기다리지 않고 다음 문항으로
            // 넘길 수 있게 한다. 답 판정은 이미 SubmitShot에서 끝났고 여기서는
            // 발사 연출만 마친다.
            if (phase == Phase.Shot)
            {
                FinishPendingShot();
                return;
            }
            StartRunDirect();
        }

        public void TestAnswerCorrect()
        {
            FinishPendingShot();
            if (phase != Phase.Playing || current == null) return;
            SetSelectedAngle(current.angle, false);
            SubmitShot();
        }

        public void TestAnswerWrong()
        {
            FinishPendingShot();
            if (phase != Phase.Playing || current == null) return;
            int wrong = current.angle == BitSasuRules.MaxAngle ? current.angle - 1 : current.angle + 1;
            SetSelectedAngle(wrong, false);
            SubmitShot();
        }

        public string StateJson()
        {
            st.phase = phase == Phase.Title ? "title" : phase == Phase.End ? (endReason == "clear" ? "clear" : "gameover") : "playing";
            st.onboarding = phase == Phase.Practice;
            st.level = current == null ? 1 : Math.Max(1, current.band);
            st.lives = Math.Max(0, st.lives);
            return JsonUtility.ToJson(st);
        }

        public string ProblemBankJson() => MgfJson.Bank(bank);
    }
}
