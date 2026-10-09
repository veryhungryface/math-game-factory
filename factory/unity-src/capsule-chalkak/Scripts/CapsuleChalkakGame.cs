// 캡슐 찰칵 — 타이틀 / 실제 체인·레버 연습 / 6주문 상태 머신 / 브리지.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.CapsuleChalkak
{
    public partial class CapsuleChalkakGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int order;
            public int selectedCount;
            public int attempts;
            public int firstAttemptTotal;
            public int firstAttemptCorrect;
            public int complementRings = 2;
            public int combo;
            public int pointerVersion;
            public int[] selectedIds = new int[0];
            public int[] tilePx = new int[0];
            public int[] controlPx = new int[0];
            // prompt, goal, board, lever, rawFraction 순서의 [left,top,right,bottom] 실제 픽셀.
            public int[] layoutPx = new int[0];
            public int screenW;
            public int screenH;
            public float leverPullRatio;
            public bool complementMode;
            public bool onboarding;
            public bool frozen;
            public string currentProblem = "";
            public string prompt = "";
            public string[] labels = new string[0];
            public string misconceptionId = "";
        }

        enum GamePhase { Title, Practice, Playing, Reveal, End }
        enum InputMode { None, Chain, Lever, TitleHandle, EndHandle }

        readonly State st = new State();
        readonly List<CapsuleProblem> bank = new List<CapsuleProblem>();
        readonly List<MgfProblem> bridgeBank = new List<MgfProblem>();
        readonly List<int> chain = new List<int>(36);
        readonly System.Random seedRng = new System.Random(20261010);

        GamePhase gamePhase = GamePhase.Title;
        InputMode inputMode;
        CapsuleProblem current;
        List<CapsuleProblem> deck;
        int deckIndex, runSerial;
        ulong selectedMask;
        bool currentFirst, revealCorrect, revealPractice, complementSpentThisOrder;
        float runLeft, orderLeft, revealClock, idleClock, hitStopLeft, inputLockLeft;
        float leverPull;
        Vector2 dragStart;
        string endReason = "";

        void Awake()
        {
            MgfLook.Quality(36f);
            bank.AddRange(CapsuleRules.BuildBank());
            bridgeBank.AddRange(CapsuleRules.BridgeBank(bank));
            BuildWorld();
            BuildUi();
            Prewarm();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb = new StringBuilder("캡슐찰칵물살로경우를엮어확률을띄워라중학교2학년심해관측07관측창아무곳이나눌러잠수정광섬유표본공간수주레버상승주문점수산소첫시도남은시간반전링기약분수영업종료배송완료다시가동");
            sb.Append("조건에맞는경우를한붓으로엮고레버를아래로당기시오연습빨간공파란공전체선택누락조건밖경우포함앞면뒷면주사위서로다른동전임의로공의모양과크기는모두같다");
            for (int i = 0; i < bridgeBank.Count; i++)
            {
                sb.Append(bridgeBank[i].prompt).Append(bridgeBank[i].answer).Append(bridgeBank[i].unitConcept);
                if (bridgeBank[i].choices != null) for (int j = 0; j < bridgeBank[i].choices.Length; j++) sb.Append(bridgeBank[i].choices[j]);
            }
            MgfText.Prewarm(sb.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(.05f, Time.deltaTime);
            ApplyResponsiveLayout();
            HandleInput();

            if (hitStopLeft > 0f) hitStopLeft -= dt;
            else
            {
                if (inputLockLeft > 0f) inputLockLeft -= dt;
                if (gamePhase == GamePhase.Practice)
                {
                    idleClock += dt;
                    if (idleClock >= 8f) { idleClock = 0f; ReplayPracticeGuide(true); }
                }
                else if (gamePhase == GamePhase.Playing)
                {
                    runLeft = Mathf.Max(0f, runLeft - dt);
                    orderLeft = Mathf.Max(0f, orderLeft - dt);
                    idleClock += dt;
                    if (idleClock >= 8f) { idleClock = 0f; ReplayPracticeGuide(true); }
                    if (orderLeft <= 0f) SubmitTimeout();
                    else if (runLeft <= 0f) EndRun(false, "time");
                }
                else if (gamePhase == GamePhase.Reveal)
                {
                    revealClock += dt;
                    if (revealClock >= (revealCorrect ? 1.65f : 1.05f)) FinishReveal();
                }
                AnimateWorld(dt);
            }
            UpdateVisuals(dt);
        }

        void ShowTitle()
        {
            gamePhase = GamePhase.Title;
            inputMode = InputMode.None;
            current = null;
            deck = null;
            chain.Clear(); selectedMask = 0;
            st.score = 0; st.lives = CapsuleRules.StartLives; st.level = 1; st.solved = 0;
            st.order = 0; st.selectedCount = 0; st.attempts = 0; st.firstAttemptTotal = 0;
            st.firstAttemptCorrect = 0; st.complementRings = 2; st.combo = 0;
            st.complementMode = false; st.onboarding = false; st.frozen = false;
            st.currentProblem = ""; st.prompt = ""; st.misconceptionId = "";
            endReason = ""; leverPull = 0f; runLeft = CapsuleRules.RunSeconds; orderLeft = 0f;
            SetScreen();
            Notify();
        }

        void StartPractice()
        {
            runSerial++;
            gamePhase = GamePhase.Practice;
            current = CapsuleRules.Practice();
            deck = null; deckIndex = 0; currentFirst = true;
            st.score = 0; st.lives = CapsuleRules.StartLives; st.level = 1; st.solved = 0; st.order = 0;
            st.attempts = 0; st.firstAttemptTotal = 0; st.firstAttemptCorrect = 0; st.combo = 0;
            st.complementRings = 2; st.complementMode = false; st.onboarding = true; st.frozen = true;
            endReason = ""; runLeft = CapsuleRules.RunSeconds; orderLeft = 999f;
            ResetSelection();
            BindCurrent();
            ResetWorldForProblem();
            SetScreen();
            ReplayPracticeGuide(false);
            ShowToast("표본 칸을 누른 채 지나가고, 레버를 아래로 당기시오", 4.5f);
            MgfSfx.Play("whoosh", .3f);
            Notify();
        }

        void StartRunDirect()
        {
            runSerial++;
            var rng = new System.Random(unchecked(seedRng.Next() ^ Environment.TickCount ^ runSerial * 104729));
            deck = CapsuleRules.RuntimeDeck(rng);
            deckIndex = 0;
            gamePhase = GamePhase.Playing;
            st.score = 0; st.lives = CapsuleRules.StartLives; st.level = 1; st.solved = 0; st.order = 1;
            st.attempts = 0; st.firstAttemptTotal = 0; st.firstAttemptCorrect = 0; st.combo = 0;
            st.complementRings = 2; st.complementMode = false; st.onboarding = false; st.frozen = false;
            st.misconceptionId = "";
            endReason = ""; runLeft = CapsuleRules.RunSeconds;
            LoadNextProblem();
            SetScreen();
            ShowToast("실전 · 조건에 맞는 모든 경우를 정확히 엮으시오", 3.2f);
            Notify();
        }

        void BeginMainAfterPractice()
        {
            StartRunDirect();
            ShowToast("2/4가 1/2 캡슐로 압축되었다 · 이제 실전", 3.4f);
        }

        void LoadNextProblem()
        {
            if (deck == null || deckIndex >= deck.Count)
            {
                EndRun(true, "delivered");
                return;
            }
            current = deck[deckIndex++];
            currentFirst = true;
            complementSpentThisOrder = false;
            st.level = current.band;
            st.order = st.solved + 1;
            st.complementMode = false;
            st.misconceptionId = "";
            orderLeft = Mathf.Max(14f, 25f - st.solved * 1.7f);
            idleClock = 0f;
            ResetSelection();
            BindCurrent();
            ResetWorldForProblem();
            RefreshProblemUi();
        }

        void BindCurrent()
        {
            st.currentProblem = current == null ? "" : current.id;
            st.prompt = current == null ? "" : current.prompt;
            st.labels = current == null ? new string[0] : current.labels;
            st.selectedCount = CapsuleRules.CountBits(selectedMask);
            st.selectedIds = chain.ToArray();
        }

        void ResetSelection()
        {
            selectedMask = 0UL;
            chain.Clear();
            leverPull = 0f;
            inputMode = InputMode.None;
            st.selectedCount = 0;
            st.selectedIds = new int[0];
            ClearChainVisuals();
        }

        void HandleInput()
        {
            if (MgfPointer.Down)
            {
                st.pointerVersion++;
                dragStart = MgfPointer.Position;
                if (gamePhase == GamePhase.Title)
                {
                    // 타이틀의 세계 전체가 관측창이다. 첫눈에 들어오는 가오리·제목·수주를
                    // 눌러도 같은 실제 시작 경로로 들어가며, 보이지 않는 무효 탭을 만들지 않는다.
                    inputMode = InputMode.TitleHandle;
                    PressTitleHandle(true);
                    MgfSfx.Play("tap", .1f);
                    Notify();
                    return;
                }
                if (gamePhase == GamePhase.End)
                {
                    if (InEndHandle(MgfPointer.Position)) { inputMode = InputMode.EndHandle; PressEndHandle(true); }
                    else RefuseAt(MgfPointer.Position, "가동 손잡이를 누르면 다시 시작한다");
                    Notify();
                    return;
                }
                if (gamePhase == GamePhase.Reveal || inputLockLeft > 0f)
                {
                    RefuseAt(MgfPointer.Position, "압축이 끝날 때까지 잠시 기다리시오");
                    return;
                }
                if (InComplementRing(MgfPointer.Position) && current != null && current.allowComplement)
                {
                    ToggleComplement();
                    return;
                }
                if (InLever(MgfPointer.Position))
                {
                    inputMode = InputMode.Lever;
                    SetLeverPull(0f, true);
                    return;
                }
                int tile = TileAt(MgfPointer.Position);
                if (tile >= 0)
                {
                    inputMode = InputMode.Chain;
                    VisitTile(tile);
                    SetChainTail(MgfPointer.Position, true);
                    return;
                }
                RefuseAt(MgfPointer.Position, "표본 칸을 누른 채 지나가시오");
            }

            if (MgfPointer.Held)
            {
                if (inputMode == InputMode.Chain)
                {
                    int tile = TileAt(MgfPointer.Position);
                    if (tile >= 0) VisitTile(tile);
                    SetChainTail(MgfPointer.Position, true);
                }
                else if (inputMode == InputMode.Lever)
                {
                    float pull = Mathf.Max(0f, dragStart.y - MgfPointer.Position.y);
                    SetLeverPull(pull, true);
                }
            }

            if (MgfPointer.Up)
            {
                if (inputMode == InputMode.TitleHandle)
                {
                    PressTitleHandle(false);
                    inputMode = InputMode.None;
                    StartPractice();
                    return;
                }
                if (inputMode == InputMode.EndHandle)
                {
                    PressEndHandle(false);
                    inputMode = InputMode.None;
                    StartPractice();
                    return;
                }
                if (inputMode == InputMode.Chain)
                {
                    SetChainTail(MgfPointer.Position, false);
                    inputMode = InputMode.None;
                    Notify();
                    return;
                }
                if (inputMode == InputMode.Lever)
                {
                    // SetLeverPull은 픽셀 이동량을 0..1로 정규화한다.
                    bool submit = leverPull >= .999f;
                    inputMode = InputMode.None;
                    if (submit) ResolveSubmission(false);
                    else
                    {
                        SetLeverPull(0f, false);
                        RecoilLever(false);
                        ShowToast("눈금 아래까지 당겨야 압축된다", 2f);
                        MgfSfx.Play("wrong", .08f);
                    }
                    Notify();
                }
            }
        }

        void VisitTile(int index)
        {
            if (current == null || index < 0 || index >= current.labels.Length) return;
            int pos = chain.IndexOf(index);
            if (pos >= 0)
            {
                if (pos == chain.Count - 2)
                {
                    int removed = chain[chain.Count - 1];
                    chain.RemoveAt(chain.Count - 1);
                    selectedMask &= ~(1UL << removed);
                    MgfSfx.Play("tap", .055f);
                    PulseTile(removed, false);
                }
                else if (pos != chain.Count - 1)
                {
                    RefuseTile(index);
                    return;
                }
            }
            else
            {
                chain.Add(index);
                selectedMask |= 1UL << index;
                MgfSfx.Play("tap", .065f + Mathf.Min(.12f, chain.Count * .005f));
                PulseTile(index, true);
            }
            BindCurrent();
            RefreshChainVisuals();
            RefreshProblemUi();
            idleClock = 0f;
            Notify();
        }

        void ToggleComplement()
        {
            if (!current.allowComplement) return;
            if (!st.complementMode && st.complementRings <= 0)
            {
                RefuseAt(MgfPointer.Position, "이번 판의 반전 링을 모두 사용했다");
                return;
            }
            st.complementMode = !st.complementMode;
            if (st.complementMode && !complementSpentThisOrder)
            {
                st.complementRings--;
                complementSpentThisOrder = true;
            }
            ResetSelection();
            RotateComplementRing(st.complementMode);
            ShowToast(st.complementMode ? "여사건 모드 · 반대 경우를 직접 엮으시오" : "직접 사건 모드로 돌아왔다", 2.5f);
            MgfSfx.Play("whoosh", .12f);
            Notify();
        }

        void ResolveSubmission(bool fromHook)
        {
            if (current == null || (gamePhase != GamePhase.Playing && gamePhase != GamePhase.Practice)) return;
            bool ok = CapsuleRules.IsExact(current, selectedMask, st.complementMode);
            st.attempts++;
            if (currentFirst && gamePhase == GamePhase.Playing)
            {
                st.firstAttemptTotal++;
                if (ok) st.firstAttemptCorrect++;
                currentFirst = false;
            }
            revealCorrect = ok;
            revealPractice = gamePhase == GamePhase.Practice;
            revealClock = 0f;
            gamePhase = GamePhase.Reveal;
            if (ok)
            {
                int gain = revealPractice ? 20 : 100 + st.combo * 25 + current.band * 10;
                st.score += gain;
                if (!revealPractice) { st.solved++; st.combo++; }
                st.misconceptionId = "";
                hitStopLeft = .075f;
                StartCorrectCompression(current, selectedMask, st.complementMode);
                MgfSfx.Play("correct", .28f);
            }
            else
            {
                st.lives--;
                st.combo = 0;
                ulong required = st.complementMode && current.allowComplement
                    ? CapsuleRules.FullMask(current.labels.Length) & ~current.answerMask : current.answerMask;
                bool includedWrong = (selectedMask & ~required) != 0;
                st.misconceptionId = includedWrong ? "condition_outside_included" : "condition_inside_missing";
                StartWrongRecoil(includedWrong ? "조건 밖 경우가 포함되었다" : "조건 안 경우가 빠졌다");
                MgfSfx.Play("wrong", .18f);
            }
            BindCurrent();
            RefreshProblemUi();
            Notify();
        }

        void SubmitTimeout()
        {
            if (gamePhase != GamePhase.Playing) return;
            // 현재 선택은 그대로 보존하고 같은 판정 경로로 실패한다.
            st.misconceptionId = "time_no_submission";
            ResolveSubmission(false);
        }

        void FinishReveal()
        {
            if (revealCorrect)
            {
                if (revealPractice) { BeginMainAfterPractice(); return; }
                if (st.solved >= CapsuleRules.TargetOrders) { EndRun(true, "delivered"); return; }
                gamePhase = GamePhase.Playing;
                LoadNextProblem();
            }
            else
            {
                if (st.lives <= 0) { EndRun(false, "pressure"); return; }
                gamePhase = revealPractice ? GamePhase.Practice : GamePhase.Playing;
                // 오답 뒤에는 선택 집합을 남겨 학생이 되짚어 고칠 수 있게 한다.
                orderLeft = Mathf.Max(orderLeft, 9f);
                SetLeverPull(0f, false);
                RefreshProblemUi();
                ShowToast(st.misconceptionId == "condition_outside_included" ? "조건 밖 경우 포함 · 광섬유를 되짚어 빼시오" : "조건 안 경우 누락 · 표본공간을 다시 훑으시오", 3f);
            }
            SetScreen();
            Notify();
        }

        void EndRun(bool clear, string reason)
        {
            gamePhase = GamePhase.End;
            endReason = reason;
            st.phase = clear ? "clear" : "lost";
            st.level = Math.Max(1, st.level);
            st.frozen = true;
            inputMode = InputMode.None;
            SetScreen();
            ShowEnd(clear);
            MgfSfx.Play(clear ? "win" : "lose", .35f);
            Notify();
        }

        void RefuseAt(Vector2 screen, string reason)
        {
            SpawnRipple(screen);
            ShowToast(reason, 1.8f);
            PointAtActiveTarget();
            MgfSfx.Play("wrong", .045f);
            inputLockLeft = .06f;
            Notify();
        }

        void Notify()
        {
            st.phase = gamePhase == GamePhase.Title ? "title"
                : gamePhase == GamePhase.Practice ? "practice"
                : gamePhase == GamePhase.Playing ? "playing"
                : gamePhase == GamePhase.Reveal ? "reveal"
                : endReason == "delivered" ? "clear" : "lost";
            MgfBridge.NotifyChanged();
        }

        public void TestStart() => StartRunDirect();

        public void TestAnswerCorrect()
        {
            if (gamePhase != GamePhase.Playing) StartRunDirect();
            selectedMask = st.complementMode && current.allowComplement
                ? CapsuleRules.FullMask(current.labels.Length) & ~current.answerMask : current.answerMask;
            chain.Clear();
            for (int i = 0; i < current.labels.Length; i++) if ((selectedMask & (1UL << i)) != 0) chain.Add(i);
            BindCurrent(); RefreshChainVisuals(); ResolveSubmission(true);
        }

        public void TestAnswerWrong()
        {
            if (gamePhase != GamePhase.Playing) StartRunDirect();
            ulong required = st.complementMode && current.allowComplement
                ? CapsuleRules.FullMask(current.labels.Length) & ~current.answerMask : current.answerMask;
            selectedMask = required ^ 1UL;
            selectedMask &= CapsuleRules.FullMask(current.labels.Length);
            chain.Clear();
            for (int i = 0; i < current.labels.Length; i++) if ((selectedMask & (1UL << i)) != 0) chain.Add(i);
            BindCurrent(); RefreshChainVisuals(); ResolveSubmission(true);
        }

        public string StateJson() => JsonUtility.ToJson(st);
        public string ProblemBankJson() => MgfJson.Bank(bridgeBank);
    }
}
