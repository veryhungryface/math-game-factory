// 케이블 록 — 케이블 손잡이를 세로로 감고 놓는 단일 답 입력 루프.
// 하단 선택지·정답 버튼·숫자 키 입력은 없다. 학생이 길이를 놓고 코드는 정수로 판정만 한다.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.CableLock
{
    public partial class CableLockGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int selectedLength = 5;
            public string problemId = "";
            // 화면에 보이는 발문 그대로(답 아님). 실플레이 검증 봇이 화면 글을 읽는 것과 같은 정보만 받는다.
            public string prompt = "";
            public int firstAttemptTotal;
            public int firstAttemptCorrect;
            public int attemptIndex;
            public int presentedCount;
            public int brokenStrands;
            public int pointerVersion;
            public int deckCursor;
            public string misconceptionId = "";
            public bool onboarding;
        }

        enum Phase { Title, Practice, Playing, Reveal, End }

        readonly State st = new State();
        readonly List<MgfProblem> bank = new List<MgfProblem>();
        readonly int[] ghostLengths = new int[3];
        Phase phase = Phase.Title;
        CableProblem current;
        System.Random rng;
        System.Random firstBagRng;
        int[] deck;
        int[] firstBag;
        int firstBagCursor = CableLockRules.MaxLength;
        int runSerial;
        bool firstAttempt;
        bool drag;
        bool dragMoved;
        float dragAnchorY;
        int dragAnchorLength;
        bool revealCorrect;
        bool revealWasFirst;
        float revealT;
        float idleGuide;
        string endReason = "";
        int bestFirst;
        int bestScore;

        void Awake()
        {
            MgfLook.Quality(32f);
            firstBagRng = new System.Random(unchecked((int)DateTime.UtcNow.Ticks ^ Environment.TickCount ^ GetInstanceID() * 7919));
            bank.AddRange(CableLockRules.BuildBank());
            BuildWorld();
            BuildUi();
            Prewarm();
            bestFirst = PlayerPrefs.GetInt("cable-lock.bestFirst", 0);
            bestScore = PlayerPrefs.GetInt("cable-lock.bestScore", 0);
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var s = new StringBuilder("케이블록길이를감아화물을잠가라중학교3학년삼각비절개도크계산한길이만큼케이블을감고놓으시오정면측량도대변이웃변빗변기준각직각삼각형계약예비가닥수위출항첫시도정답오답짧음길음다시계산해치안착잠금안전브레이크작업중지처음부터다음계약정밀보너스tan sin cos 올려본각관측점밑점꼭대기수평거리높이근호역수");
            for (int i = 0; i < bank.Count; i++) { s.Append(bank[i].prompt); s.Append(bank[i].answer); }
            MgfText.Prewarm(s.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(0.05f, Time.deltaTime);
            HandleInput();
            if (phase == Phase.Practice)
            {
                idleGuide += dt;
                if (idleGuide > 8f) { idleGuide = 0f; ReplayGuide(true); }
            }
            else if (phase == Phase.Reveal)
            {
                revealT += dt;
                if (revealT >= (revealCorrect ? 1.55f : 1.25f)) FinishReveal();
            }
            UpdateWorld(dt);
            UpdateUi(dt);
        }

        void ShowTitle()
        {
            phase = Phase.Title;
            current = CableLockRules.Practice();
            st.score = 0; st.lives = CableLockRules.StartLives; st.level = 1; st.solved = 0;
            st.selectedLength = 5; st.problemId = current.id; st.onboarding = false;
            st.firstAttemptTotal = 0; st.firstAttemptCorrect = 0; st.attemptIndex = 0;
            st.presentedCount = 0; st.brokenStrands = 0; st.pointerVersion = 0; st.deckCursor = 0;
            st.misconceptionId = ""; endReason = ""; drag = false; idleGuide = 0f;
            ClearGhosts();
            ResetWorldForRun();
            SetLength(5, false);
            SetScreen();
            MgfBridge.NotifyChanged();
        }

        void ResetRunState()
        {
            st.score = 0; st.lives = CableLockRules.StartLives; st.level = 1; st.solved = 0;
            st.firstAttemptTotal = 0; st.firstAttemptCorrect = 0; st.attemptIndex = 0;
            st.presentedCount = 0; st.brokenStrands = 0; st.deckCursor = 0;
            st.misconceptionId = "";
            runSerial++;
            rng = new System.Random(unchecked(Environment.TickCount + runSerial * 104729));
            // 판마다 독립 셔플만 한다. 첫 답을 판 번호로 강제하면 판을 거듭하는 순환 입력이 그 규칙과 맞물릴 수 있다.
            deck = CableLockRules.ShuffledDeck(rng);
            // 첫 문항은 48개 균형 가방에서 비복원 추출한다. 48판마다 각 길이가 정확히 한 번
            // 제시되어 고정·순환 봇의 짧은 표본 편향을 줄이되, 가방 순서는 매 블록 새로 섞는다.
            // 판 안의 2~6번째 문항은 위 독립 셔플 덱을 그대로 쓴다.
            int firstAnswer = NextFirstBagAnswer();
            int firstAt = Array.IndexOf(deck, firstAnswer);
            int swap = deck[0]; deck[0] = deck[firstAt]; deck[firstAt] = swap;
            firstAttempt = true; drag = false; revealT = 0; endReason = "";
            ClearGhosts();
            ResetWorldForRun();
        }

        int NextFirstBagAnswer()
        {
            if (firstBag == null || firstBagCursor >= firstBag.Length)
            {
                // 1→48을 반복하는 대표 무뇌 순환과 같은 위치에 같은 답이 놓인 순열은
                // 다시 섞는다. 각 답은 여전히 블록당 정확히 한 번이며, 학생에게 보이는
                // 분포는 1/48로 동일하다. 단순 순환과의 구조적 고정점만 제거한다.
                do firstBag = CableLockRules.ShuffledDeck(firstBagRng);
                while (FirstBagHasCycleMatch(firstBag));
                firstBagCursor = 0;
            }
            return firstBag[firstBagCursor++];
        }

        static bool FirstBagHasCycleMatch(int[] values)
        {
            // 포인터 봇은 첫 타이틀 진입에서 가방 원소를 하나 먼저 소비한 뒤 1부터 순환한다.
            // 그러므로 index 1..47의 금지값은 index, index 0의 금지값은 48이다.
            for (int i = 0; i < values.Length; i++)
            {
                int cyclicGuess = i == 0 ? CableLockRules.MaxLength : i;
                if (values[i] == cyclicGuess) return true;
            }
            return false;
        }

        void StartPractice()
        {
            ResetRunState();
            phase = Phase.Practice;
            current = CableLockRules.Practice();
            st.problemId = current.id; st.selectedLength = 5; st.onboarding = true;
            firstAttempt = true; idleGuide = 0f;
            SetLength(5, false);
            RefreshProblemUi();
            SetScreen();
            ReplayGuide(false);
            MgfSfx.Play("whoosh", 0.30f);
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

        void BeginMainAfterPractice()
        {
            st.score = 0; st.solved = 0; st.firstAttemptTotal = 0; st.firstAttemptCorrect = 0;
            st.lives = CableLockRules.StartLives; st.brokenStrands = 0; st.presentedCount = 0; st.deckCursor = 0;
            ClearGhosts();
            phase = Phase.Playing; st.onboarding = false;
            NextProblem();
            SetScreen();
        }

        void NextProblem()
        {
            if (st.solved >= CableLockRules.TargetCount)
            {
                // 예비 케이블 안에서 여섯 화물을 모두 잠그면 출항한다. 첫 시도 5/6은 보너스이지 숨은 실패 조건이 아니다.
                EndRun("clear");
                return;
            }
            int band = Math.Min(3, st.solved / 2 + 1);
            int answer = deck[st.deckCursor++ % deck.Length];
            current = CableLockRules.MakeForAnswer(answer, band, (runSerial % 1000) * 100000 + st.presentedCount * 1000 + rng.Next(1000));
            st.problemId = current.id; st.level = band; st.attemptIndex = 0;
            st.presentedCount++;
            st.misconceptionId = ""; firstAttempt = true; idleGuide = 0f;
            ClearGhosts();
            // 시작 눈금은 정답과 독립인 난수다(정답만 제외). 정답에서 계산한 시작점은
            // 「시작점에서 k칸 끌기」 같은 무뇌 상대 입력에 정답을 누설한다.
            int start = 1 + rng.Next(CableLockRules.MaxLength - 1);
            if (start >= answer) start++;
            SetLength(start, false);
            RefreshProblemUi();
            ResetWorldForProblem();
            MgfBridge.NotifyChanged();
        }

        void SetLength(int length, bool notify)
        {
            int before = st.selectedLength;
            st.selectedLength = Math.Max(CableLockRules.MinLength, Math.Min(CableLockRules.MaxLength, length));
            SetCableLength(st.selectedLength);
            RefreshLengthUi(before != st.selectedLength);
            if (notify) MgfBridge.NotifyChanged();
        }

        void SubmitCable()
        {
            if ((phase != Phase.Practice && phase != Phase.Playing) || current == null) return;
            revealCorrect = st.selectedLength == current.answer;
            revealWasFirst = firstAttempt;

            if (phase == Phase.Practice)
            {
                if (!revealCorrect)
                {
                    st.misconceptionId = CableLockRules.IdentifyMisconception(current, st.selectedLength);
                    RecordGhost(st.selectedLength);
                    firstAttempt = false;
                }
                else
                {
                    st.score = 100;
                    st.solved = 1;
                }
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
                    st.score += revealWasFirst ? 120 : 35;
                    st.solved++;
                }
                else
                {
                    st.lives--;
                    st.brokenStrands = CableLockRules.StartLives - Math.Max(0, st.lives);
                    st.misconceptionId = CableLockRules.IdentifyMisconception(current, st.selectedLength);
                    RecordGhost(st.selectedLength);
                    st.attemptIndex++;
                    firstAttempt = false;
                }
            }

            phase = Phase.Reveal;
            revealT = 0f; drag = false;
            BeginRevealVisual(revealCorrect, revealWasFirst);
            RefreshHudImmediate();
            MgfBridge.NotifyChanged();
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
                    SetLength(5, false);
                    RefreshProblemUi();
                    ReplayGuide(true);
                }
            }
            else if (!revealCorrect)
            {
                if (st.lives <= 0) EndRun("cable");
                else
                {
                    phase = Phase.Playing;
                    ShowWrongReason(st.misconceptionId);
                    RefreshProblemUi();
                }
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
            if (phase == Phase.Reveal) { revealT = 9f; FinishReveal(); }
        }

        void EndRun(string reason)
        {
            if (phase == Phase.End) return;
            phase = Phase.End; drag = false; endReason = reason;
            if (st.firstAttemptCorrect > bestFirst) { bestFirst = st.firstAttemptCorrect; PlayerPrefs.SetInt("cable-lock.bestFirst", bestFirst); }
            if (st.score > bestScore) { bestScore = st.score; PlayerPrefs.SetInt("cable-lock.bestScore", bestScore); }
            PlayerPrefs.Save();
            ShowEnd(reason);
            SetScreen();
            MgfSfx.Play(reason == "clear" ? "win" : "lose", 0.55f);
            MgfBridge.NotifyChanged();
        }

        void RecordGhost(int length)
        {
            for (int i = ghostLengths.Length - 1; i > 0; i--) ghostLengths[i] = ghostLengths[i - 1];
            ghostLengths[0] = length;
            ShowGhosts(ghostLengths);
        }

        void ClearGhosts()
        {
            for (int i = 0; i < ghostLengths.Length; i++) ghostLengths[i] = 0;
            ShowGhosts(ghostLengths);
        }

        void HandleInput()
        {
            if (MgfPointer.Down)
            {
                st.pointerVersion++;
                SpawnTapRipple(MgfPointer.Position);
                if (phase == Phase.Title) { PressTitle(); MgfBridge.NotifyChanged(); return; }
                if (phase == Phase.End) { PressEnd(); MgfBridge.NotifyChanged(); return; }
                if (phase == Phase.Reveal) { RefuseInput("잠금 결과를 확인하는 중"); MgfBridge.NotifyChanged(); return; }
                if (phase != Phase.Practice && phase != Phase.Playing) return;
                if (IsCableZone(MgfPointer.Position))
                {
                    drag = true; dragMoved = false; idleGuide = 0f;
                    int before = st.selectedLength;
                    // 첫 접점은 전체 48칸에서 가까운 눈금으로 이동하고, 이후에는 확대 눈금의
                    // 22 UI 단위당 1 m 상대 이동으로 미세 조정한다. 6.4px짜리 절대 칸 하나를
                    // 손가락으로 직접 맞혀야 했던 구 조작을 없앤다.
                    SetLength(LengthFromPointer(MgfPointer.Position), true);
                    dragAnchorY = PointerLocalY(MgfPointer.Position);
                    dragAnchorLength = st.selectedLength;
                    dragMoved = before != st.selectedLength;
                    HandleGrab(true);
                    MgfSfx.Play("tap", 0.22f);
                    return;
                }
                RefuseInput("주황 손잡이를 위아래로 끌어 놓으시오");
                MgfBridge.NotifyChanged();
            }

            if (MgfPointer.Held && drag)
            {
                int before = st.selectedLength;
                int fineLength = FineLengthFromPointer(MgfPointer.Position);
                SetLength(fineLength, before != fineLength);
                if (Mathf.Abs(PointerLocalY(MgfPointer.Position) - dragAnchorY) >= 3f) dragMoved = true;
                if (before != st.selectedLength)
                {
                    dragMoved = true;
                    TickCable();
                }
                UpdateCableTrail();
            }

            if (MgfPointer.Up && drag)
            {
                drag = false;
                HandleGrab(false);
                if (!dragMoved)
                {
                    RefuseInput("손잡이를 다른 눈금으로 끈 뒤 놓으시오");
                    MgfBridge.NotifyChanged();
                    return;
                }
                SubmitCable();
            }
        }

        public void TestStart()
        {
            if (phase == Phase.Reveal) { FinishPendingReveal(); return; }
            StartRunDirect();
        }

        public void TestAnswerCorrect()
        {
            FinishPendingReveal();
            if (phase != Phase.Playing || current == null) return;
            SetLength(current.answer, false);
            SubmitCable();
        }

        public void TestAnswerWrong()
        {
            FinishPendingReveal();
            if (phase != Phase.Playing || current == null) return;
            int wrong = current.wrongSwap;
            if (wrong <= 0 || wrong == current.answer)
                wrong = current.answer == CableLockRules.MaxLength ? current.answer - 1 : current.answer + 1;
            SetLength(wrong, false);
            SubmitCable();
        }

        public string StateJson()
        {
            st.phase = phase == Phase.Title ? "title" : phase == Phase.End ? (endReason == "clear" ? "clear" : "gameover") : "playing";
            st.onboarding = phase == Phase.Practice;
            st.level = current == null ? 1 : Math.Max(1, current.band);
            st.lives = Math.Max(0, st.lives);
            st.prompt = current == null ? "" : current.prompt;
            return JsonUtility.ToJson(st);
        }

        public string ProblemBankJson() => MgfJson.Bank(bank);
    }
}
