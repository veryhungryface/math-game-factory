// 눈토끼 시소 — 상태 머신, 실제 포인터 입력, 실패 상태와 MGF 브리지.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.NuntokkiSiso
{
    public partial class NuntokkiSisoGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int ordersDone;
            public int firstAttemptTotal;
            public int firstAttemptCorrect;
            public int combo;
            public int selectedK;
            public int selectedHeight;
            public int targetMini;
            public int attempts;
            public int pointerVersion;
            public int handleX;
            public int handleY;
            public int pinX;
            public int pinY;
            public int gaugeTopY;
            public int gaugeBottomY;
            public int wrongMarks;
            public string problemId = "";
            public string prompt = "";
            public string misconceptionId = "";
            public string actionPhase = "";
            public string guidePhase = "";
            public string layout = "";
            public string endReason = "";
            public bool onboarding;
            public bool handleMoved;
            public bool pinPulling;
        }

        enum Mode { Title, Practice, Playing, Reveal, End }

        readonly State st = new State();
        readonly List<MgfProblem> bank = new List<MgfProblem>();
        Mode mode = Mode.Title;
        List<SnowOrder> deck;
        SnowOrder current;
        int runSerial;
        int orderIndex;
        int attemptOnCurrent;
        int previousCorrectK;
        float sessionLeft;
        float revealClock;
        float idleGuide;
        float practiceClock;
        float endClock;
        float dragStartY;
        float dragTravel;
        float visualK = 5f;
        float pinStartY;
        float pinPullProgress;
        float practicePullCarry;
        int shortPinPulls;
        bool draggingHandle;
        bool draggingPin;
        bool keyboardPinArmed;
        bool revealCorrect;
        bool revealPractice;
        int revealSelectedK;
        string endReason = "";

        void Awake()
        {
            MgfLook.Quality(34f);
            // 런타임 프리미티브 콜라이더 타입이 WebGL 스트리핑에서 빠지지 않도록 직접 참조한다.
            var sphereLink = gameObject.AddComponent<SphereCollider>();
            sphereLink.enabled = false;
            var capsuleLink = gameObject.AddComponent<CapsuleCollider>();
            capsuleLink.enabled = false;
            bank.AddRange(NuntokkiRules.BuildBank());
            NuntokkiRules.AuditBank(bank);
            BuildWorld();
            BuildUi();
            Prewarm();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var text = new StringBuilder("눈토끼시소부피만큼키워균형을잡아라닮음비부피의비같은재료밀도양쪽팔길이높이손잡이핀당기기햇살하트광장완성축제종료다시시작너무크다작다미니눈사람개분중학교수학도형");
            for (int i = 0; i < bank.Count; i++)
            {
                text.Append(bank[i].prompt);
                text.Append(bank[i].answer);
                text.Append(bank[i].unitConcept);
            }
            MgfText.Prewarm(text.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(.05f, Time.deltaTime);
            ApplyResponsiveLayout();
            SyncControlGeometry();
            HandleInput();

            if (mode == Mode.Practice)
            {
                practiceClock += dt;
                idleGuide += dt;
                if (idleGuide >= 8f) { idleGuide = 0f; BoostGuide(); }
                if (practiceClock >= 1.25f && current != null && st.selectedK != current.targetK)
                {
                    // 연습 전용 구조 신호: 첫 안내를 잠시 보여 준 뒤 정답을 채점하는 대신 고정 예시 눈금으로
                    // 손잡이 이동을 한 번 시범 보이고, 실제 핀 당김은 플레이어에게 남긴다.
                    SelectK(current.targetK, true);
                    ShowToast("연습 예시를 4 cm에 맞췄어요 · 이제 핀을 당기세요");
                    ReplayPinGuide();
                }
            }
            else if (mode == Mode.Playing)
            {
                sessionLeft -= dt;
                if (sessionLeft <= 0f) EndRun("timer");
            }
            else if (mode == Mode.Reveal)
            {
                revealClock += dt;
                float wait = revealCorrect ? (revealPractice ? 2.4f : 2.05f) : (revealPractice ? 2.35f : 2.15f);
                if (revealClock >= wait) FinishReveal();
            }
            else if (mode == Mode.End) endClock += dt;

            UpdateWorld(dt);
            UpdateUi(dt);
        }

        void ShowTitle()
        {
            mode = Mode.Title;
            deck = null;
            current = null;
            endReason = "";
            st.score = 0;
            st.lives = NuntokkiRules.StartLives;
            st.level = 1;
            st.solved = 0;
            st.ordersDone = 0;
            st.firstAttemptTotal = 0;
            st.firstAttemptCorrect = 0;
            st.combo = 0;
            st.selectedK = 5;
            st.selectedHeight = 10;
            st.targetMini = 0;
            st.attempts = 0;
            st.pointerVersion = 0;
            st.wrongMarks = 0;
            st.problemId = "";
            st.prompt = "";
            st.misconceptionId = "";
            st.endReason = "";
            st.onboarding = false;
            st.handleMoved = false;
            st.pinPulling = false;
            visualK = 5f;
            draggingHandle = draggingPin = keyboardPinArmed = false;
            if (titleMetaTxt) titleMetaTxt.text = "중2 · 도형의 닮음    최고 첫 시도 " + PlayerPrefs.GetInt("nuntokki-siso.best", 0) + "/7";
            ResetWorldForRun();
            SetScreen();
            MgfBridge.NotifyChanged();
        }

        void ResetRun()
        {
            runSerial++;
            deck = NuntokkiRules.RunDeck(runSerial);
            orderIndex = 0;
            attemptOnCurrent = 0;
            previousCorrectK = 0;
            sessionLeft = NuntokkiRules.SessionSeconds;
            st.score = 0;
            st.lives = NuntokkiRules.StartLives;
            st.level = 1;
            st.solved = 0;
            st.ordersDone = 0;
            st.firstAttemptTotal = 0;
            st.firstAttemptCorrect = 0;
            st.combo = 0;
            st.selectedK = 5;
            st.selectedHeight = 10;
            st.targetMini = 0;
            st.attempts = 0;
            st.wrongMarks = 0;
            st.problemId = "";
            st.prompt = "";
            st.misconceptionId = "";
            st.endReason = "";
            st.handleMoved = false;
            st.pinPulling = false;
            draggingHandle = draggingPin = keyboardPinArmed = false;
            pinPullProgress = 0;
            practicePullCarry = 0;
            shortPinPulls = 0;
            visualK = 5f;
            endReason = "";
            ResetWorldForRun();
        }

        void StartPractice()
        {
            ResetRun();
            mode = Mode.Practice;
            st.onboarding = true;
            current = NuntokkiRules.Practice();
            idleGuide = 0;
            practiceClock = 0;
            BeginOrder(true);
            SetScreen();
            BoostGuide();
            MgfSfx.Play("whoosh", .28f);
            MgfBridge.NotifyChanged();
        }

        void StartRunDirect()
        {
            ResetRun();
            mode = Mode.Playing;
            st.onboarding = false;
            current = deck[0];
            BeginOrder(false);
            SetScreen();
            MgfBridge.NotifyChanged();
        }

        void BeginMainAfterPractice()
        {
            st.score = 0;
            st.lives = NuntokkiRules.StartLives;
            st.solved = 0;
            st.ordersDone = 0;
            st.firstAttemptTotal = 0;
            st.firstAttemptCorrect = 0;
            st.combo = 0;
            st.wrongMarks = 0;
            st.onboarding = false;
            st.misconceptionId = "";
            orderIndex = 0;
            sessionLeft = NuntokkiRules.SessionSeconds;
            mode = Mode.Playing;
            current = deck[0];
            BeginOrder(false);
            SetScreen();
        }

        void BeginOrder(bool practice)
        {
            attemptOnCurrent = 0;
            st.attempts = 0;
            st.problemId = current.id;
            st.prompt = current.prompt;
            st.level = Math.Max(1, current.band);
            st.targetMini = current.targetMini;
            st.selectedK = practice ? 5 : (2 + ((runSerial + orderIndex * 3) % 7));
            if (st.selectedK == current.targetK) st.selectedK = st.selectedK == 8 ? 7 : st.selectedK + 1;
            st.selectedHeight = current.baseHeight * st.selectedK;
            st.handleMoved = false;
            st.pinPulling = false;
            st.misconceptionId = "";
            draggingHandle = draggingPin = keyboardPinArmed = false;
            pinPullProgress = 0;
            practicePullCarry = 0;
            shortPinPulls = 0;
            visualK = st.selectedK;
            ResetWorldForOrder();
            RefreshOrderUi();
            MgfBridge.NotifyChanged();
        }

        void AdvanceOrder()
        {
            if (mode != Mode.Playing) return;
            if (orderIndex >= deck.Count)
            {
                EndRun(st.firstAttemptCorrect >= NuntokkiRules.MasteryFirstCorrect ? "clear" : "not_mastered");
                return;
            }
            current = deck[orderIndex];
            BeginOrder(false);
        }

        void SelectK(int k, bool moved)
        {
            k = Mathf.Clamp(k, NuntokkiRules.MinK, NuntokkiRules.MaxK);
            bool changed = st.selectedK != k;
            bool firstPracticeMove = mode == Mode.Practice && moved && !st.handleMoved;
            st.selectedK = k;
            st.selectedHeight = current == null ? 2 * k : current.baseHeight * k;
            visualK = k;
            if (moved) st.handleMoved = true;
            idleGuide = 0;
            if (changed) MgfSfx.Play("tap", .10f);
            SnapHandleVisual(k, moved);
            if (firstPracticeMove)
            {
                ShowToast("노란 손길을 따라 고정핀을 아래로 당기세요");
                ReplayPinGuide();
            }
            MgfBridge.NotifyChanged();
        }

        void ResolveSelection(int selectedK)
        {
            if ((mode != Mode.Practice && mode != Mode.Playing) || current == null) return;
            revealPractice = mode == Mode.Practice;
            revealCorrect = NuntokkiRules.IsCorrect(current, selectedK);
            revealSelectedK = selectedK;
            if (!revealPractice)
            {
                bool first = attemptOnCurrent == 0;
                attemptOnCurrent++;
                st.attempts = attemptOnCurrent;
                if (first) st.firstAttemptTotal++;
                if (revealCorrect)
                {
                    if (first)
                    {
                        st.firstAttemptCorrect++;
                        st.combo++;
                        if (st.combo % 3 == 0 && st.lives < NuntokkiRules.StartLives) st.lives++;
                    }
                    else st.combo = 0;
                    st.score += 160 + Math.Max(0, st.combo) * 30;
                    st.solved++;
                    st.ordersDone++;
                    orderIndex++;
                    previousCorrectK = current.targetK;
                    st.misconceptionId = "";
                }
                else
                {
                    st.combo = 0;
                    st.lives--;
                    st.wrongMarks++;
                    st.misconceptionId = NuntokkiRules.MisconceptionId(current, selectedK, previousCorrectK);
                }
            }
            else if (revealCorrect)
            {
                st.score = 80;
                st.solved = 1;
                st.misconceptionId = "";
            }
            else st.misconceptionId = NuntokkiRules.MisconceptionId(current, selectedK, previousCorrectK);

            draggingPin = false;
            st.pinPulling = false;
            mode = Mode.Reveal;
            revealClock = 0;
            BeginRevealVisual(revealCorrect, revealPractice, selectedK, current.targetK, current.targetMini, st.misconceptionId);
            SetScreen();
            MgfBridge.NotifyChanged();
        }

        void FinishReveal()
        {
            if (mode != Mode.Reveal) return;
            CompleteRevealVisual();
            if (revealPractice)
            {
                if (revealCorrect) BeginMainAfterPractice();
                else
                {
                    mode = Mode.Practice;
                    st.misconceptionId = "";
                    st.handleMoved = false;
                    st.pinPulling = false;
                    st.selectedK = revealSelectedK;
                    st.selectedHeight = current.baseHeight * revealSelectedK;
                    visualK = revealSelectedK;
                    pinPullProgress = 0;
                    practicePullCarry = 0;
                    ResetWorldForOrder();
                    RefreshOrderUi();
                    ShowToast("실제 " + NuntokkiRules.Cube(revealSelectedK) + "개분입니다 · 손잡이를 다시 조절하세요");
                    BoostGuide();
                }
            }
            else if (revealCorrect && orderIndex >= NuntokkiRules.TargetOrders)
                EndRun(st.firstAttemptCorrect >= NuntokkiRules.MasteryFirstCorrect ? "clear" : "not_mastered");
            else if (!revealCorrect && st.lives <= 0) EndRun("hearts_empty");
            else if (revealCorrect)
            {
                mode = Mode.Playing;
                AdvanceOrder();
            }
            else
            {
                mode = Mode.Playing;
                st.misconceptionId = "";
                st.handleMoved = false;
                st.pinPulling = false;
                pinPullProgress = 0;
                visualK = st.selectedK;
                ResetWorldForOrder();
                RefreshOrderUi();
                ShowToast("젖은 눈금을 피하고 k³을 다시 확인하세요");
                BoostGuide();
            }
            SetScreen();
            MgfBridge.NotifyChanged();
        }

        void EndRun(string reason)
        {
            if (mode == Mode.End) return;
            endReason = reason;
            st.endReason = reason;
            st.onboarding = false;
            st.pinPulling = false;
            draggingHandle = draggingPin = false;
            mode = Mode.End;
            endClock = 0;
            ShowEndVisual(reason);
            SetScreen();
            MgfBridge.NotifyChanged();
            MgfSfx.Play(reason == "clear" ? "win" : "lose", .50f);
            if (reason == "clear")
            {
                int best = PlayerPrefs.GetInt("nuntokki-siso.best", 0);
                if (st.firstAttemptCorrect > best)
                {
                    PlayerPrefs.SetInt("nuntokki-siso.best", st.firstAttemptCorrect);
                    PlayerPrefs.Save();
                }
            }
        }

        bool IsHandleZone(Vector2 p)
        {
            float r = Mathf.Max(58f, Mathf.Min(Screen.width, Screen.height) * .10f);
            return Vector2.Distance(p, handleScreen) <= r ||
                   (p.x >= gaugeRect.xMin && p.x <= gaugeRect.xMax && p.y >= gaugeRect.yMin && p.y <= gaugeRect.yMax);
        }

        bool IsPinZone(Vector2 p)
        {
            float r = Mathf.Max(64f, Mathf.Min(Screen.width, Screen.height) * .12f);
            if (mode == Mode.Practice && st.handleMoved) r = Mathf.Max(r, 96f);
            return Vector2.Distance(p, pinScreen) <= r;
        }

        int KFromPointer(float screenY)
        {
            float t = Mathf.InverseLerp(gaugeRect.yMin, gaugeRect.yMax, screenY);
            return Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(NuntokkiRules.MinK, NuntokkiRules.MaxK, t)), NuntokkiRules.MinK, NuntokkiRules.MaxK);
        }

        void HandleInput()
        {
            if (mode == Mode.Title)
            {
                if (MgfPointer.Down)
                {
                    st.pointerVersion++;
                    PulseTitle();
                    MgfBridge.NotifyChanged();
                    StartPractice();
                }
                return;
            }
            if (mode == Mode.End)
            {
                if (MgfPointer.Down && endClock >= .8f)
                {
                    st.pointerVersion++;
                    StartRunDirect();
                    MgfSfx.Play("whoosh", .28f);
                }
                return;
            }
            if (mode == Mode.Reveal)
            {
                if (MgfPointer.Down)
                {
                    st.pointerVersion++;
                    RippleAt(MgfPointer.Position, "균형 결과를 확인하는 중입니다");
                    MgfBridge.NotifyChanged();
                }
                return;
            }
            if (mode != Mode.Practice && mode != Mode.Playing) return;

            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                keyboardPinArmed = false;
                SelectK(st.selectedK + 1, true);
            }
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                if (keyboardPinArmed)
                {
                    keyboardPinArmed = false;
                    ResolveSelection(st.selectedK);
                    return;
                }
                SelectK(st.selectedK - 1, true);
            }
            if (Input.GetKeyDown(KeyCode.Return))
            {
                if (!st.handleMoved) { RefusePin("먼저 높이 손잡이를 움직이세요"); }
                else { keyboardPinArmed = true; ShowToast("↓ 키를 눌러 고정핀을 당기세요"); }
            }

            if (MgfPointer.Down)
            {
                st.pointerVersion++;
                idleGuide = 0;
                bool practicePinReady = mode == Mode.Practice && st.handleMoved && current != null && st.selectedK == current.targetK;
                if (practicePinReady)
                {
                    // 구조 시범 뒤에는 화면 어디서 시작한 당김도 핀 연습으로 일관되게 받는다.
                    draggingHandle = false;
                    draggingPin = true;
                    pinStartY = MgfPointer.Position.y;
                    pinPullProgress = practicePullCarry;
                    st.pinPulling = true;
                    BeginPinPull();
                    MgfSfx.Play("tap", .16f);
                }
                else if (IsHandleZone(MgfPointer.Position))
                {
                    draggingHandle = true;
                    draggingPin = false;
                    dragStartY = MgfPointer.Position.y;
                    dragTravel = 0;
                    BeginHandleDrag();
                    MgfSfx.Play("tap", .14f);
                }
                else if (IsPinZone(MgfPointer.Position))
                {
                    draggingHandle = false;
                    if (!st.handleMoved)
                    {
                        RefusePin("먼저 높이 손잡이를 움직이세요");
                    }
                    else
                    {
                        draggingPin = true;
                        pinStartY = MgfPointer.Position.y;
                        pinPullProgress = mode == Mode.Practice ? practicePullCarry : 0;
                        st.pinPulling = true;
                        BeginPinPull();
                        MgfSfx.Play("tap", .16f);
                    }
                }
                else if (mode == Mode.Practice && st.handleMoved)
                {
                    // 연습에서는 움직이는 노란 손길을 따라 화면 어디서든 당기기 제스처를
                    // 시작할 수 있다. 본 게임은 실제 핀 히트 영역과 아래 방향을 그대로 요구한다.
                    draggingHandle = false;
                    draggingPin = true;
                    pinStartY = MgfPointer.Position.y;
                    pinPullProgress = practicePullCarry;
                    st.pinPulling = true;
                    BeginPinPull();
                    MgfSfx.Play("tap", .16f);
                }
                else
                {
                    draggingHandle = draggingPin = false;
                    GuideInvalidInput(MgfPointer.Position);
                    MgfSfx.Play("wrong", .08f);
                }
                MgfBridge.NotifyChanged();
            }

            if (draggingHandle && MgfPointer.Held)
            {
                dragTravel = Mathf.Max(dragTravel, Mathf.Abs(MgfPointer.Position.y - dragStartY));
                float t = Mathf.InverseLerp(gaugeRect.yMin, gaugeRect.yMax, MgfPointer.Position.y);
                visualK = Mathf.Lerp(NuntokkiRules.MinK, NuntokkiRules.MaxK, Mathf.Clamp01(t));
                UpdateHandleDrag(visualK);
            }
            if (draggingHandle && MgfPointer.Up)
            {
                draggingHandle = false;
                int k = KFromPointer(MgfPointer.Position.y);
                bool moved = dragTravel >= 14f || k != st.selectedK;
                if (moved) SelectK(k, true);
                else
                {
                    visualK = st.selectedK;
                    RefuseHandle("손잡이를 위아래로 끌어 높이를 정하세요");
                }
                EndHandleDrag();
            }

            if (draggingPin && MgfPointer.Held)
            {
                // Unity 화면 좌표는 아래가 작다. 아래로 당기면 startY-currentY가 양수다.
                float signedPull = pinStartY - MgfPointer.Position.y;
                pinPullProgress = mode == Mode.Practice
                    ? Mathf.Clamp(practicePullCarry + Mathf.Abs(signedPull), 0f, 110f)
                    : Mathf.Clamp(signedPull, 0f, 110f);
                UpdatePinPull(pinPullProgress);
            }
            if (draggingPin && MgfPointer.Up)
            {
                draggingPin = false;
                st.pinPulling = false;
                float requiredPull = mode == Mode.Practice ? 36f : 58f;
                if (mode == Mode.Practice)
                {
                    float gesturePull = Mathf.Max(0f, pinPullProgress - practicePullCarry);
                    // 노란 경로를 보고 누르기부터 시도한 학생도 두 번이면 진행을 느낀다.
                    // 실제로 끈 거리는 그대로 누적하므로 8px 두 번 같은 짧은 당김은 아직 제출되지 않는다.
                    float creditedPull = gesturePull < 1f ? 18f : gesturePull;
                    practicePullCarry = Mathf.Clamp(practicePullCarry + creditedPull, 0f, requiredPull);
                    pinPullProgress = practicePullCarry;
                    UpdatePinPull(pinPullProgress);
                }
                if (pinPullProgress >= requiredPull)
                {
                    shortPinPulls = 0;
                    practicePullCarry = 0;
                    ResolveSelection(st.selectedK);
                }
                else
                {
                    shortPinPulls++;
                    if (mode == Mode.Practice)
                    {
                        ShowToast("핀 줄을 조금 더 아래로 당기세요");
                        BoostGuide();
                        MgfSfx.Play("wrong", .10f);
                        MgfFx.Punch(pinRt, .10f, .24f);
                        if (shortPinPulls >= 2)
                        {
                            shortPinPulls = 0;
                            ShowToast("노란 손길이 움직이는 길 끝까지 당기세요");
                            ReplayPinGuide();
                        }
                    }
                    else
                    {
                        RefusePin("핀 줄을 조금 더 아래로 당기세요");
                        pinPullProgress = 0;
                    }
                    EndPinPull(false);
                    MgfBridge.NotifyChanged();
                }
            }
        }

        public void TestStart() => StartRunDirect();

        public void TestAnswerCorrect()
        {
            if (mode == Mode.Title || mode == Mode.End) StartRunDirect();
            if (mode == Mode.Reveal) FinishReveal();
            if ((mode == Mode.Practice || mode == Mode.Playing) && current != null)
            {
                SelectK(current.targetK, true);
                ResolveSelection(current.targetK);
            }
        }

        public void TestAnswerWrong()
        {
            if (mode == Mode.Title || mode == Mode.End) StartRunDirect();
            if (mode == Mode.Reveal) FinishReveal();
            if ((mode == Mode.Practice || mode == Mode.Playing) && current != null)
            {
                int wrong = current.targetK == NuntokkiRules.MaxK ? current.targetK - 1 : current.targetK + 1;
                SelectK(wrong, true);
                ResolveSelection(wrong);
            }
        }

        public string StateJson()
        {
            if (mode == Mode.Title) st.phase = "title";
            else if (mode == Mode.End) st.phase = endReason == "clear" ? "clear" : (endReason == "not_mastered" ? "not_mastered" : "gameover");
            else st.phase = "playing";
            st.actionPhase = mode.ToString().ToLowerInvariant();
            return JsonUtility.ToJson(st);
        }

        public string ProblemBankJson() => MgfJson.Bank(bank);
    }
}
