// 컷라인 — 실제 포인터 입력과 상태 머신.
// 답 입력은 검은 스트립을 정수 길이까지 끈 뒤 주황 칼날을 아래로 스와이프하는 경로 하나뿐이다.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.Keotlain
{
    public partial class KeotlainGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int selectedLength;
            public int cartridgeRemaining;
            public int cartridgesLeft;
            public int selectedOrder;
            public int pieceIndex;
            public int orderPieces;
            public int firstAttemptTotal;
            public int firstAttemptCorrect;
            public int cuts;
            public int pointerVersion;
            public string orderId = "";
            public string prompt = "";
            public string misconceptionId = "";
            public bool onboarding;
        }

        sealed class ActiveOrder
        {
            public CutProblem problem;
            public int piece;
            public bool clean = true;
            public bool firstRecorded;
        }

        enum Phase { Title, Practice, Playing, Reveal, End }

        readonly State st = new State();
        readonly List<MgfProblem> bank = new List<MgfProblem>();
        readonly List<ActiveOrder> active = new List<ActiveOrder>(3);
        List<CutProblem> deck;
        CutProblem practice;
        Phase phase = Phase.Title;
        int runSerial;
        int nextDeckIndex;
        int selectedSlot;
        int cartridgesUsed;
        float timeLeft;
        float revealClock;
        float idleGuide;
        bool revealCorrect;
        bool revealPractice;
        bool draggingStrip;
        bool draggingBlade;
        bool practiceStripTouched;
        bool practiceAssistCut;
        float practiceAssistClock;
        Vector2 pointerDown;
        int scoreBeforeCut;
        string endReason = "";

        ActiveOrder Current { get { return active.Count == 0 ? null : active[Mathf.Clamp(selectedSlot, 0, active.Count - 1)]; } }

        void Awake()
        {
            MgfLook.Quality(38f);
            var catalog = KeotlainRules.BuildCatalog();
            for (int i = 0; i < catalog.Count; i++) bank.Add(catalog[i].ToMgf());
            practice = KeotlainRules.Practice();
            BuildWorld();
            BuildUi();
            Prewarm();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb = new StringBuilder("컷라인길이를당겨램프를세워라중학교2학년피타고라스정리도시옥상스케이트파크버팀목직각빗변카트리지안전테이프스트립칼날손잡이아래로쓸어절단하시오남은길이폐자재교체정답오답다시시작완주잠시직각의대변제곱합차");
            for (int i = 0; i < bank.Count; i++) { sb.Append(bank[i].prompt); sb.Append(bank[i].answer); sb.Append(bank[i].unitConcept); }
            MgfText.Prewarm(sb.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(.05f, Time.deltaTime);
            HandleInput();

            if (phase == Phase.Practice)
            {
                if (practiceAssistCut)
                {
                    practiceAssistClock += dt;
                    MoveBladeVisual(Mathf.Clamp01((practiceAssistClock - .35f) / .75f));
                    if (practiceAssistClock >= 1.10f)
                    {
                        practiceAssistCut = false;
                        SubmitCut();
                    }
                }
                else
                {
                    idleGuide += dt;
                    if (idleGuide >= 8f) { idleGuide = 0f; ReplayGuide(true); }
                }
            }
            else if (phase == Phase.Playing)
            {
                timeLeft -= dt;
                if (timeLeft <= 0f) EndRun("time");
            }
            else if (phase == Phase.Reveal)
            {
                revealClock += dt;
                if (revealClock >= (revealCorrect ? 1.45f : 1.30f)) FinishReveal();
            }

            UpdateWorld(dt);
            UpdateUi(dt);
        }

        void ShowTitle()
        {
            phase = Phase.Title;
            st.score = 0; st.lives = KeotlainRules.StartLives; st.level = 1; st.solved = 0; st.phase = "title";
            st.selectedLength = 1; st.cartridgeRemaining = 29; st.cartridgesLeft = 4; st.selectedOrder = 0;
            st.pieceIndex = 0; st.orderPieces = 1; st.firstAttemptTotal = 0; st.firstAttemptCorrect = 0;
            st.cuts = 0; st.pointerVersion = 0; st.orderId = ""; st.prompt = ""; st.misconceptionId = ""; st.onboarding = false;
            active.Clear(); deck = null; selectedSlot = 0; nextDeckIndex = 0; cartridgesUsed = 1;
            draggingStrip = draggingBlade = false; endReason = "";
            practiceStripTouched = false;
            practiceAssistCut = false; practiceAssistClock = 0f;
            ResetWorldForRun(); ShowTitleVisual(); SetScreen();
            MgfBridge.NotifyChanged();
        }

        void ResetRunState()
        {
            runSerial++;
            deck = KeotlainRules.BuildRunDeck(runSerial);
            active.Clear(); nextDeckIndex = 0; selectedSlot = 0;
            for (int i = 0; i < 3 && nextDeckIndex < deck.Count; i++) AddNextOrder();
            st.score = 0; st.lives = KeotlainRules.StartLives; st.level = 1; st.solved = 0;
            st.selectedLength = 1; st.cartridgeRemaining = KeotlainRules.CartridgeLength; st.cartridgesLeft = KeotlainRules.CartridgeCount;
            st.selectedOrder = 0; st.pieceIndex = 0; st.orderPieces = 1; st.firstAttemptTotal = 0; st.firstAttemptCorrect = 0;
            st.cuts = 0; st.orderId = ""; st.prompt = ""; st.misconceptionId = ""; st.onboarding = false;
            cartridgesUsed = 1; timeLeft = 105f; revealClock = 0f; idleGuide = 0f; endReason = "";
            draggingStrip = draggingBlade = false;
            practiceStripTouched = false;
            practiceAssistCut = false; practiceAssistClock = 0f;
            ResetWorldForRun();
        }

        void AddNextOrder()
        {
            if (deck == null || nextDeckIndex >= deck.Count) return;
            active.Add(new ActiveOrder { problem = deck[nextDeckIndex++], piece = 0, clean = true, firstRecorded = false });
        }

        void StartPractice()
        {
            ResetRunState();
            active.Clear(); active.Add(new ActiveOrder { problem = practice, piece = 0, clean = true, firstRecorded = false });
            selectedSlot = 0; st.onboarding = true; phase = Phase.Practice; st.phase = "playing";
            st.score = 0; st.solved = 0; st.selectedLength = 1; st.cartridgeRemaining = 29; st.cartridgesLeft = 4;
            idleGuide = 0f; SyncCurrent(); StartProblemVisual(true); SetScreen(); ReplayGuide(false);
            MgfSfx.Play("whoosh", .30f); MgfBridge.NotifyChanged();
        }

        void StartRunDirect()
        {
            ResetRunState();
            phase = Phase.Playing; st.phase = "playing"; st.onboarding = false;
            SyncCurrent(); StartProblemVisual(false); SetScreen(); MgfBridge.NotifyChanged();
        }

        void StartMainAfterPractice()
        {
            // 연습은 통계·자원·점수에서 제외한다. 이미 만든 실제 덱을 다시 연다.
            active.Clear(); nextDeckIndex = 0; selectedSlot = 0;
            for (int i = 0; i < 3 && nextDeckIndex < deck.Count; i++) AddNextOrder();
            st.score = 0; st.solved = 0; st.lives = 3; st.firstAttemptTotal = 0; st.firstAttemptCorrect = 0; st.cuts = 0;
            st.selectedLength = 1; st.cartridgeRemaining = 29; st.cartridgesLeft = 4; cartridgesUsed = 1;
            st.onboarding = false; st.misconceptionId = ""; timeLeft = 105f;
            phase = Phase.Playing; st.phase = "playing";
            SyncCurrent(); StartProblemVisual(false); SetScreen(); MgfBridge.NotifyChanged();
        }

        void SyncCurrent()
        {
            var o = Current;
            if (o == null) { st.orderId = ""; st.prompt = ""; return; }
            st.selectedOrder = selectedSlot;
            st.orderId = o.problem.id;
            st.prompt = o.problem.prompt;
            st.level = Math.Max(1, o.problem.band);
            st.pieceIndex = o.piece;
            st.orderPieces = o.problem.pieces.Length;
            RefreshOrdersUi();
        }

        void SelectOrder(int slot)
        {
            if ((phase != Phase.Playing && phase != Phase.Practice) || active.Count == 0) return;
            selectedSlot = Mathf.Clamp(slot, 0, active.Count - 1);
            st.misconceptionId = ""; SyncCurrent(); OnOrderSelected(selectedSlot); MgfSfx.Play("tap", .14f);
            MgfBridge.NotifyChanged();
        }

        void SetLength(int value, bool fromPointer)
        {
            int v = Mathf.Clamp(value, 1, 29);
            if (st.selectedLength == v) return;
            st.selectedLength = v;
            OnLengthChanged(v, fromPointer);
            MgfBridge.NotifyChanged();
        }

        void ChangeCartridge()
        {
            if (phase != Phase.Playing || st.cartridgesLeft <= 1)
            {
                RefuseInput(phase == Phase.Practice ? "연습에서는 카트리지를 바꾸지 않아도 된다" : "새 카트리지가 없다");
                return;
            }
            cartridgesUsed++;
            st.cartridgeRemaining = KeotlainRules.CartridgeLength;
            st.cartridgesLeft = KeotlainRules.CartridgeCount - cartridgesUsed + 1;
            OnCartridgeChanged(); MgfSfx.Play("whoosh", .28f); MgfBridge.NotifyChanged();
        }

        void SubmitCut()
        {
            if (phase != Phase.Playing && phase != Phase.Practice) return;
            var o = Current; if (o == null) return;
            int expected = o.problem.PieceAt(o.piece);
            int cut = st.selectedLength;

            if (phase == Phase.Playing && cut > st.cartridgeRemaining)
            {
                RefuseInput("남은 " + st.cartridgeRemaining + " cm보다 길다. 왼쪽 레버로 교체하시오");
                ResetBladeVisual(); return;
            }

            revealPractice = phase == Phase.Practice;
            revealCorrect = cut == expected;
            scoreBeforeCut = st.score;

            if (revealPractice)
            {
                if (revealCorrect) st.score = 80;
                else st.misconceptionId = KeotlainRules.MisconceptionId(o.problem, cut);
            }
            else
            {
                if (!o.firstRecorded) { o.firstRecorded = true; st.firstAttemptTotal++; }
                st.cartridgeRemaining -= cut;
                st.cuts++;
                if (revealCorrect)
                {
                    st.score += 90 + st.level * 15 + (o.clean ? 20 : 0);
                    o.piece++;
                    if (o.piece >= o.problem.pieces.Length)
                    {
                        st.solved++;
                        if (o.clean) st.firstAttemptCorrect++;
                    }
                }
                else
                {
                    o.clean = false;
                    st.lives--;
                    st.misconceptionId = KeotlainRules.MisconceptionId(o.problem, cut);
                }
            }

            phase = Phase.Reveal; st.phase = "paused"; revealClock = 0f;
            draggingStrip = draggingBlade = false;
            BeginRevealVisual(o.problem, expected, cut, revealCorrect, revealPractice);
            SetScreen(); MgfBridge.NotifyChanged();
        }

        void FinishReveal()
        {
            if (phase != Phase.Reveal) return;
            FinishRevealVisual(revealCorrect);

            if (revealPractice)
            {
                if (revealCorrect) StartMainAfterPractice();
                else
                {
                    phase = Phase.Practice; st.phase = "playing"; st.selectedLength = 1; idleGuide = 0f;
                    StartProblemVisual(true); ReplayGuide(true); SetScreen(); MgfBridge.NotifyChanged();
                }
                return;
            }

            if (!revealCorrect)
            {
                if (st.lives <= 0) { EndRun("tape"); return; }
                if (CleanPotential() < 7) { EndRun("precision"); return; }
                if (RemainingRequired() > RemainingStock()) { EndRun("stock"); return; }
                phase = Phase.Playing; st.phase = "playing"; SyncCurrent(); StartProblemVisual(false); SetScreen(); MgfBridge.NotifyChanged();
                return;
            }

            var o = Current;
            if (o != null && o.piece >= o.problem.pieces.Length)
            {
                active.RemoveAt(selectedSlot);
                AddNextOrder();
                if (active.Count == 0)
                {
                    if (st.solved == KeotlainRules.Goal && st.firstAttemptCorrect >= 7 && st.lives > 0) EndRun("clear");
                    else EndRun("precision");
                    return;
                }
                selectedSlot = Mathf.Clamp(selectedSlot, 0, active.Count - 1);
            }
            if (RemainingRequired() > RemainingStock()) { EndRun("stock"); return; }
            phase = Phase.Playing; st.phase = "playing"; SyncCurrent(); StartProblemVisual(false); SetScreen(); MgfBridge.NotifyChanged();
        }

        int CleanPotential()
        {
            int n = st.firstAttemptCorrect;
            for (int i = 0; i < active.Count; i++) if (active[i].clean) n++;
            n += deck == null ? 0 : deck.Count - nextDeckIndex;
            return n;
        }

        int RemainingRequired()
        {
            int total = 0;
            for (int i = 0; i < active.Count; i++)
                for (int k = active[i].piece; k < active[i].problem.pieces.Length; k++) total += active[i].problem.pieces[k];
            if (deck != null) for (int i = nextDeckIndex; i < deck.Count; i++) total += deck[i].Answer;
            return total;
        }

        int RemainingStock()
        {
            return st.cartridgeRemaining + Math.Max(0, st.cartridgesLeft - 1) * KeotlainRules.CartridgeLength;
        }

        void EndRun(string reason)
        {
            if (phase == Phase.End) return;
            phase = Phase.End; endReason = reason; draggingStrip = draggingBlade = false;
            st.phase = reason == "clear" ? "clear" : "gameover";
            int best = PlayerPrefs.GetInt("keotlain.best", 0);
            if (st.score > best) { PlayerPrefs.SetInt("keotlain.best", st.score); PlayerPrefs.Save(); }
            ShowEnd(reason); SetScreen(); MgfSfx.Play(reason == "clear" ? "win" : "lose", .62f); MgfBridge.NotifyChanged();
        }

        int LengthFromScreen(float x)
        {
            float t = Mathf.InverseLerp(Screen.width * .08f, Screen.width * .68f, x);
            return 1 + Mathf.Clamp(Mathf.RoundToInt(t * 28f), 0, 28);
        }

        void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow)) SetLength(st.selectedLength - 1, true);
            if (Input.GetKeyDown(KeyCode.RightArrow)) SetLength(st.selectedLength + 1, true);
            if (Input.GetKeyDown(KeyCode.Space) && (phase == Phase.Playing || phase == Phase.Practice)) SubmitCut();

            if (MgfPointer.Down)
            {
                st.pointerVersion++;
                pointerDown = MgfPointer.Position;
                SpawnTapRipple(pointerDown);

                if (phase == Phase.Title) { StartPractice(); return; }
                if (phase == Phase.End) { ShowTitle(); return; }
                if (phase == Phase.Reveal)
                {
                    if (revealClock > .32f) { revealClock = 99f; FinishReveal(); }
                    else RefuseInput("절단 결과를 확인하시오");
                    return;
                }

                float nx = pointerDown.x / Mathf.Max(1f, Screen.width);
                float ny = pointerDown.y / Mathf.Max(1f, Screen.height);
                // 첫 스트립 드래그 뒤에는 정답 눈금→칼날 스와이프를 짧게 자동 시연한다.
                // 학생이 직접 칼날을 먼저 잡으면 시연을 취소하고 정상 스와이프로 이어 간다.
                if (phase == Phase.Practice && practiceAssistCut)
                {
                    if (nx > .55f && ny < .55f) practiceAssistCut = false;
                    else return;
                }
                if (ny > .50f)
                {
                    SelectOrder(Mathf.Clamp((int)(nx * 3f), 0, 2));
                    return;
                }
                // 레버는 실제 보라 버튼의 맨 아래 띠에만 둔다. 이전 .18 높이는
                // 1~9 cm 스트립 눈금까지 덮어 연습 정답 5 cm를 잡을 수 없었다.
                if (nx < .22f && ny < .10f)
                {
                    ChangeCartridge(); return;
                }
                if (nx > (phase == Phase.Practice ? .55f : .70f) && ny < .55f)
                {
                    draggingBlade = true; StartBladeVisual(); MgfSfx.Play("tap", .12f); return;
                }
                if (ny < .50f)
                {
                    draggingStrip = true;
                    if (phase == Phase.Practice && !practiceStripTouched)
                    {
                        practiceStripTouched = true;
                        OnTutorialStripTouched();
                    }
                    SetLength(LengthFromScreen(pointerDown.x), true); StartStripDragVisual(); return;
                }
                RefuseInput("아래 검은 스트립을 당기시오");
            }

            if (MgfPointer.Held)
            {
                if (draggingStrip) SetLength(LengthFromScreen(MgfPointer.Position.x), true);
                if (draggingBlade)
                {
                    float dy = pointerDown.y - MgfPointer.Position.y;
                    MoveBladeVisual(Mathf.Clamp01(dy / Mathf.Max(56f, Screen.height * .075f)));
                    if (dy >= Mathf.Max(56f, Screen.height * .075f))
                    {
                        draggingBlade = false; SubmitCut();
                    }
                }
            }

            if (MgfPointer.Up)
            {
                if (draggingStrip)
                {
                    draggingStrip = false; EndStripDragVisual(); MgfSfx.Play("tap", .09f);
                    if (phase == Phase.Practice)
                    {
                        // 연습의 목적은 계산 정답을 벌점 없이 맞히는 것이 아니라 두 실제 조작을
                        // 연결하는 것이다. 첫 유효 드래그 뒤 5 cm에 고정하고 칼날 경로를 완주한다.
                        SetLength(practice.PieceAt(0), true);
                        practiceAssistCut = true; practiceAssistClock = 0f;
                        StartBladeVisual(); ReplayGuide(true);
                    }
                }
                if (draggingBlade)
                {
                    draggingBlade = false; ResetBladeVisual(); RefuseInput("손잡이를 아래로 더 길게 쓸어 절단하시오");
                }
            }
        }

        // ── IMgfGame. 강제 정오답도 selectedLength를 놓고 SubmitCut()하는 실제 절단 판정을 탄다.
        public void TestStart()
        {
            if (phase == Phase.End) return; // lost/clear는 단방향이다.
            StartRunDirect();
        }

        public void TestAnswerCorrect()
        {
            if (phase == Phase.End) return;
            if (phase == Phase.Title || phase == Phase.Practice) StartRunDirect();
            if (phase == Phase.Reveal) { revealClock = 99f; FinishReveal(); }
            if (phase != Phase.Playing || Current == null) return;
            int answer = Current.problem.PieceAt(Current.piece);
            if (answer > st.cartridgeRemaining && st.cartridgesLeft > 1) ChangeCartridge();
            SetLength(answer, false); SubmitCut();
        }

        public void TestAnswerWrong()
        {
            if (phase == Phase.End) return;
            if (phase == Phase.Title || phase == Phase.Practice) StartRunDirect();
            if (phase == Phase.Reveal) { revealClock = 99f; FinishReveal(); }
            if (phase != Phase.Playing || Current == null) return;
            int answer = Current.problem.PieceAt(Current.piece);
            int wrong = answer == 29 ? 28 : answer + 1;
            if (wrong > st.cartridgeRemaining && st.cartridgesLeft > 1) ChangeCartridge();
            SetLength(wrong, false); SubmitCut();
        }

        public string StateJson() { return JsonUtility.ToJson(st); }
        public string ProblemBankJson() { return MgfJson.Bank(bank); }
    }
}
