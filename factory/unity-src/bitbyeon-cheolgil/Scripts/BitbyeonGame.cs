// 빗변 철길 — 원형 래칫 크랭크로 정수 길이를 만들고 브레이크를 쓸어 잠그는 실제 플레이 상태.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.BitbyeonCheolgil
{
    public partial class BitbyeonGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int firstAttemptTotal;
            public int firstAttemptCorrect;
            public int attemptIndex;
            public int pointerVersion;
            public int currentTick = BitbyeonRules.MinTick;
            public int bridgeIndex;
            public int cargo;
            public int streak;
            public int squareGap;
            public string problemId = "";
            public string currentPrompt = "";
            public string misconceptionId = "";
            public bool onboarding;
        }

        enum Phase { Title, Practice, Playing, Feedback, End }

        readonly State st = new State();
        readonly List<RailProblem> deck = new List<RailProblem>();
        readonly List<MgfProblem> bank = new List<MgfProblem>();
        Phase phase = Phase.Title;
        RailProblem current;
        int roundIndex;
        int currentTick = BitbyeonRules.MinTick;
        bool rotatedThisRound;
        bool firstAttempt;
        bool firstPracticeMistake = true;
        bool repairedOnce;
        bool feedbackCorrect;
        float feedbackTime;
        float roundTime;
        float idleGuide;
        string endReason = "";
        int sessionSerial = -1;

        bool draggingCrank;
        bool draggingBrake;
        float lastPointerAngle;
        float ratchetRemainder;
        Vector2 brakeStart;
        float brakeDrag;

        void Awake()
        {
            MgfLook.Quality(34f);
            deck.AddRange(BitbyeonRules.BuildSession());
            bank.AddRange(BitbyeonRules.BuildBank());
            Prewarm();
            BuildWorld();
            BuildUi();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb = new StringBuilder("빗변철길기계식신호소대각선을맞춰라크랭크릴레이정확한눈금잠금중학교2학년피타고라스정리최고개통압력점검운행종료다시출발회전후브레이크를아래로내리시오직각빗변높이밑변나머지한변제곱의합차안전브레이크부족초과첫시도정답연습완료펀칭티켓노선반");
            foreach (RailProblem p in deck) { sb.Append(p.prompt); sb.Append(p.reveal); }
            MgfText.Prewarm(sb.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(.05f, Time.deltaTime);
            HandleInput();

            if (phase == Phase.Playing)
            {
                idleGuide += dt;
                if (roundIndex >= 3 && rotatedThisRound)
                {
                    roundTime -= dt;
                    if (roundTime <= 0f) SubmitTick(BitbyeonRules.GuaranteedWrong(current), true);
                }
                if (idleGuide > 8f) { idleGuide = 0f; BoostGuide(); }
            }
            else if (phase == Phase.Practice)
            {
                idleGuide += dt;
                if (idleGuide > 8f) { idleGuide = 0f; BoostGuide(); }
            }
            else if (phase == Phase.Feedback)
            {
                feedbackTime += dt;
                if (feedbackTime >= (feedbackCorrect ? 1.55f : 1.28f)) FinishFeedback();
            }

            UpdateWorld(dt);
            UpdateUi(dt);
        }

        void ShowTitle()
        {
            phase = Phase.Title;
            current = deck[0]; roundIndex = 0;
            st.score = 0; st.lives = 3; st.level = 1; st.solved = 0;
            st.firstAttemptTotal = 0; st.firstAttemptCorrect = 0; st.attemptIndex = 0;
            st.pointerVersion = 0; st.bridgeIndex = 0; st.cargo = 0; st.streak = 0; st.squareGap = 0;
            st.problemId = current.id; st.misconceptionId = ""; st.onboarding = false;
            currentTick = BitbyeonRules.MinTick; rotatedThisRound = false; firstAttempt = true;
            draggingCrank = draggingBrake = false;
            firstPracticeMistake = true; repairedOnce = false; endReason = "";
            ResetWorldForRun();
            SetScreen();
            MgfBridge.NotifyChanged();
        }

        void BeginSession(bool onboarding)
        {
            sessionSerial++;
            phase = onboarding ? Phase.Practice : Phase.Playing;
            st.score = 0; st.lives = 3; st.level = 1; st.solved = 0;
            st.firstAttemptTotal = 0; st.firstAttemptCorrect = 0; st.attemptIndex = 0;
            st.bridgeIndex = 0; st.cargo = 0; st.streak = 0; st.squareGap = 0; st.misconceptionId = "";
            st.onboarding = onboarding;
            roundIndex = 0; firstPracticeMistake = true; repairedOnce = false; endReason = "";
            ResetWorldForRun();
            LoadRound(0);
            SetScreen();
            StartGuide();
            MgfBridge.NotifyChanged();
        }

        void LoadRound(int index)
        {
            roundIndex = Mathf.Clamp(index, 0, deck.Count - 1);
            // 난이도 밴드 순서는 유지하되 밴드 안 3문항은 판마다 회전한다.
            // 4→5→… 순환 봇이 고정 덱 위상을 외워 우연 수준을 넘는 퇴화를 막는다.
            int group = roundIndex / 3;
            int local = roundIndex % 3;
            int mapped = group * 3 + (local + sessionSerial + group) % 3;
            current = deck[mapped];
            st.problemId = current.id; st.bridgeIndex = roundIndex; st.level = current.band;
            st.attemptIndex = 0; st.misconceptionId = ""; st.squareGap = 0;
            currentTick = BitbyeonRules.MinTick; st.currentTick = currentTick;
            rotatedThisRound = false; firstAttempt = true; roundTime = roundIndex >= 7 ? 18f : 22f; idleGuide = 0f;
            draggingCrank = draggingBrake = false; ratchetRemainder = 0f; brakeDrag = 0f;
            ResetWorldForProblem();
            RefreshProblemUi();
            MgfBridge.NotifyChanged();
        }

        void HandleInput()
        {
            if ((phase == Phase.Practice || phase == Phase.Playing) && Input.GetKeyDown(KeyCode.LeftArrow)) StepTick(-1);
            if ((phase == Phase.Practice || phase == Phase.Playing) && Input.GetKeyDown(KeyCode.RightArrow)) StepTick(1);
            if ((phase == Phase.Practice || phase == Phase.Playing) && Input.GetKeyDown(KeyCode.Return))
            {
                if (rotatedThisRound) SubmitTick(currentTick, false); else Refuse("먼저 크랭크를 돌려 눈금을 만드시오.");
            }

            if (MgfPointer.Down)
            {
                st.pointerVersion++;
                SpawnRipple(MgfPointer.Position);
                if (phase == Phase.End)
                {
                    MgfSfx.Play("tap", .3f); BeginSession(true); return;
                }
                if (phase == Phase.Feedback)
                {
                    Refuse("신호 릴레이가 작동 중입니다."); MgfBridge.NotifyChanged(); return;
                }
                if (phase == Phase.Title)
                {
                    // 첫 화면은 조작법 시험이 아니다. 크랭크 전체와 화면을 넓은 시작
                    // 표적으로 쓰고 첫 탭에 래칫이 스스로 한 칸 걸린 뒤 즉시 연습으로 간다.
                    // 실제 답 만들기는 다음 화면에서만 원형 드래그로 수행한다.
                    RotateWheel(24f); GrabCrank(true); MgfFx.Punch(crankRt, .12f, .22f);
                    MgfSfx.Play("whoosh", .40f); BeginSession(true);
                    MgfBridge.NotifyChanged(); return;
                }
                if (phase != Phase.Practice && phase != Phase.Playing) return;

                if (HitBrake(MgfPointer.Position))
                {
                    draggingBrake = true; brakeStart = MgfPointer.Position; brakeDrag = 0f;
                    GrabBrake(true); MgfSfx.Play("tap", .20f); MgfBridge.NotifyChanged(); return;
                }
                if (HitCrank(MgfPointer.Position))
                {
                    draggingCrank = true; lastPointerAngle = PointerAngle(MgfPointer.Position); ratchetRemainder = 0f;
                    GrabCrank(true); idleGuide = 0f; MgfSfx.Play("tap", .18f); MgfBridge.NotifyChanged(); return;
                }
                Refuse(rotatedThisRound ? "중앙 브레이크 손잡이를 아래로 쓸어 잠그시오." : "큰 크랭크 테두리를 원을 따라 돌리시오.");
                BoostGuide(); MgfBridge.NotifyChanged();
            }

            if (MgfPointer.Held && draggingCrank)
            {
                float now = PointerAngle(MgfPointer.Position);
                float d = Mathf.DeltaAngle(lastPointerAngle, now);
                lastPointerAngle = now;
                if (Mathf.Abs(d) < 80f)
                {
                    RotateWheel(d); ratchetRemainder += d;
                    const float perTick = 360f / 22f;
                    while (Mathf.Abs(ratchetRemainder) >= perTick)
                    {
                        int dir = ratchetRemainder > 0 ? 1 : -1;
                        ratchetRemainder -= dir * perTick; StepTick(dir);
                    }
                }
            }
            if (MgfPointer.Held && draggingBrake)
            {
                brakeDrag = Mathf.Clamp(MgfPointer.Position.y - brakeStart.y, -100f * MgfText.Canvas.scaleFactor, 12f * MgfText.Canvas.scaleFactor);
                MoveBrake(brakeDrag / Mathf.Max(.01f, MgfText.Canvas.scaleFactor));
            }

            if (MgfPointer.Up)
            {
                if (draggingCrank) { draggingCrank = false; GrabCrank(false); MgfBridge.NotifyChanged(); }
                if (draggingBrake)
                {
                    draggingBrake = false; GrabBrake(false);
                    float minSwipe = -42f * MgfText.Canvas.scaleFactor;
                    if (brakeDrag <= minSwipe)
                    {
                        if (rotatedThisRound) SubmitTick(currentTick, false);
                        else { Refuse("크랭크를 한 칸 이상 돌린 뒤 잠그시오."); BounceBrake(); }
                    }
                    else { Refuse("브레이크를 아래 끝까지 쓸어 내리시오."); BounceBrake(); }
                    MoveBrake(0f); MgfBridge.NotifyChanged();
                }
            }
        }

        void StepTick(int dir)
        {
            if (phase != Phase.Practice && phase != Phase.Playing) return;
            int next = Mathf.Clamp(currentTick + dir, BitbyeonRules.MinTick, BitbyeonRules.MaxTick);
            if (next == currentTick) { Refuse(next == BitbyeonRules.MinTick ? "가장 짧은 눈금입니다." : "가장 긴 눈금입니다."); return; }
            currentTick = next; st.currentTick = currentTick; rotatedThisRound = true; idleGuide = 0f;
            RatchetPulse(dir); MgfSfx.Play("tap", .18f); MgfBridge.NotifyChanged();
        }

        void SubmitTick(int tick, bool timeout)
        {
            if (phase != Phase.Practice && phase != Phase.Playing) return;
            bool correct = BitbyeonRules.IsCorrect(current, tick);
            bool wasFirst = firstAttempt;
            if (firstAttempt)
            {
                st.firstAttemptTotal++;
                if (correct) st.firstAttemptCorrect++;
                firstAttempt = false;
            }
            st.attemptIndex++;
            st.squareGap = BitbyeonRules.SquareGap(current, tick);
            st.misconceptionId = correct ? "" : (timeout ? "no-input-timeout" : BitbyeonRules.Diagnose(current, tick));
            feedbackCorrect = correct; feedbackTime = 0f; phase = Phase.Feedback;

            if (correct)
            {
                st.solved++; st.cargo++; st.streak++;
                st.score += 100 + current.band * 20 + (wasFirst ? 30 : 0);
                if (st.streak >= 3 && st.lives < 3 && !repairedOnce)
                {
                    st.lives++; repairedOnce = true;
                }
            }
            else
            {
                st.streak = 0;
                bool freePracticeMiss = st.onboarding && firstPracticeMistake;
                if (freePracticeMiss) firstPracticeMistake = false;
                else st.lives--;
            }
            BeginFeedbackVisual(correct, wasFirst, tick, timeout);
            MgfBridge.NotifyChanged();
        }

        void FinishFeedback()
        {
            CompleteFeedbackVisual(feedbackCorrect);
            if (!feedbackCorrect)
            {
                if (st.lives <= 0) { EndSession("pressure"); return; }
                // 오답은 릴레이를 버리고 다음으로 넘기는 사건이 아니라 같은 케이블을
                // 다시 계산하는 사건이다. 따라서 한 번 틀려도 교정 후 9/9가 가능하다.
                phase = st.onboarding ? Phase.Practice : Phase.Playing;
                currentTick = BitbyeonRules.MinTick; st.currentTick = currentTick; rotatedThisRound = false;
                roundTime = roundIndex >= 7 ? 18f : 22f; idleGuide = 0f;
                draggingCrank = draggingBrake = false; ratchetRemainder = 0f; brakeDrag = 0f;
                ResetWorldForProblem(); RefreshProblemUi(); StartGuide(); SetScreen(); MgfBridge.NotifyChanged(); return;
            }
            if (roundIndex >= deck.Count - 1)
            {
                if (st.cargo >= deck.Count) EndSession("clear");
                else EndSession("incomplete");
                return;
            }
            if (st.onboarding) st.onboarding = false;
            phase = Phase.Playing;
            LoadRound(roundIndex + 1);
            SetScreen();
        }

        void EndSession(string reason)
        {
            phase = Phase.End; endReason = reason; draggingCrank = draggingBrake = false;
            int best = PlayerPrefs.GetInt("bitbyeon-cheolgil.bestCargo", 0);
            if (st.cargo > best) { best = st.cargo; PlayerPrefs.SetInt("bitbyeon-cheolgil.bestCargo", best); PlayerPrefs.Save(); }
            ShowEnd(reason, best); SetScreen(); MgfSfx.Play(reason == "clear" ? "win" : "lose", .54f); MgfBridge.NotifyChanged();
        }

        public void TestStart()
        {
            if (phase == Phase.Feedback) FinishFeedback();
            BeginSession(false);
        }

        public void TestAnswerCorrect()
        {
            if (phase == Phase.Feedback) FinishFeedback();
            if (phase != Phase.Playing || current == null) BeginSession(false);
            currentTick = current.answer; st.currentTick = currentTick; rotatedThisRound = true; SetCrankToTick(currentTick);
            SubmitTick(currentTick, false);
        }

        public void TestAnswerWrong()
        {
            if (phase == Phase.Feedback) FinishFeedback();
            if (phase != Phase.Playing || current == null) BeginSession(false);
            currentTick = BitbyeonRules.GuaranteedWrong(current); st.currentTick = currentTick; rotatedThisRound = true; SetCrankToTick(currentTick);
            SubmitTick(currentTick, false);
        }

        public string StateJson()
        {
            st.phase = phase == Phase.Title ? "title" : phase == Phase.End ? (endReason == "clear" ? "clear" : "gameover") : "playing";
            st.lives = Math.Max(0, st.lives); st.currentTick = currentTick;
            // 실제 TMP 화면 버퍼를 노출해 문제은행 표본과 화면 발문이 동일한지 실기 검증한다.
            st.currentPrompt = promptText != null ? promptText.text : "";
            st.onboarding = phase == Phase.Practice || (phase == Phase.Feedback && st.onboarding);
            return JsonUtility.ToJson(st);
        }

        public string ProblemBankJson() => MgfJson.Bank(bank);
    }
}
