// 구슬 교대 — 심야 결정 관측실, 실제 두 대상쌍 연습, 10관측 상태 머신, 브리지.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.GuseulGyodae
{
    public partial class GuseulGyodaeGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int[] slots = new int[6]; // 1=파랑, 0=흰색
            public int firstAttempts;
            public int firstCorrect;
            public int band3FirstCorrect;
            public bool masteryPossible;
            public bool submissionLatched;
            public bool onboarding;
            public bool frozen;
            public int combo;
            public int pointerVersion;
            public int currentBand;
            public int targetN;
            public int targetD;
            public int fixedABlue;
            public int currentBlue;
            public string currentProblem = "";
            public string prompt = "";
            public string misconceptionId = "";
            public string lastAction = "boot";
            public string lastControl = "";
            public int lastSlot = -1;
            // 실제 포인터 검증용 화면 좌표(왼쪽 위 원점): slots[x,y]*6, supplies[blue x,y,white x,y], handle/belt.
            public int[] slotPx = new int[12];
            public int[] supplyPx = new int[4];
            public int[] dispatchPx = new int[4];
            public int[] restartPx = new int[2];
            public int screenW;
            public int screenH;
        }

        enum Phase { Title, Practice, Playing, Reveal, End }
        enum DragKind { None, Marble, Dispatch }

        readonly State st = new State();
        readonly bool[] blueSlots = new bool[6];
        readonly List<MgfProblem> bank = new List<MgfProblem>();
        readonly List<MarbleProblem> deck = new List<MarbleProblem>();

        System.Random rng;
        MarbleProblem current;
        Phase phase = Phase.Title;
        DragKind dragKind;
        bool dragBlue, currentFirst, revealCorrect, revealPractice, titlePressed;
        bool practiceSwapped, repairMode, tapMarbleArmed, tapMarbleBlue, spinTapArmed;
        int tapSlotArmed = -1;
        int orderIndex, runSerial;
        float orderLeft, globalLeft, revealClock, hitStopLeft, idleClock;
        Vector2 dragStartScreen;
        string endReason = "";

        void Awake()
        {
            MgfLook.Quality(35f);
            // CreatePrimitive(Cylinder/Sphere)가 콜라이더 타입을 이름으로 붙이므로
            // IL2CPP 스트리핑을 막기 위해 명시적으로 참조한다.
            var colliderTypes = new GameObject("PrimitiveColliderTypes");
            colliderTypes.AddComponent<CapsuleCollider>();
            colliderTypes.AddComponent<SphereCollider>();
            colliderTypes.AddComponent<BoxCollider>();
            colliderTypes.AddComponent<MeshCollider>();
            Destroy(colliderTypes);
            GuseulRules.ValidateAll();
            bank.AddRange(GuseulRules.BuildBank());
            BuildWorld();
            BuildUi();
            Prewarm();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb = new StringBuilder("구슬교대결정을바꿔별자리를맞춰라중학교2학년확률심야결정관측실궤도돔손잡이를끌어시작파랑흰색결정공급별자리링관측관문주문수리볼트점수남은시간연습실전다시하기관측완료확률검증미달게임오버");
            sb.Append("임의로꺼낸다공의모양과크기는모두같다파랑공이나올나오지않을두공이모두파랑서로다른추첨각추첨은서로영향을끼치지않는다구성하시오현재전체개탐사드론궤적이론확률관찰상대도수");
            for (int i = 0; i < bank.Count; i++) { sb.Append(bank[i].prompt); sb.Append(bank[i].answer); sb.Append(bank[i].unitConcept); }
            MgfText.Prewarm(sb.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(.05f, Time.deltaTime);
            HandleInput();
            if (hitStopLeft > 0f) hitStopLeft -= dt;
            else
            {
                if (phase == Phase.Practice)
                {
                    idleClock += dt;
                    if (idleClock >= 8f) { idleClock = 0f; ReplayGuide(true); }
                }
                else if (phase == Phase.Playing)
                {
                    globalLeft = Mathf.Max(0f, globalLeft - dt);
                    orderLeft = Mathf.Max(0f, orderLeft - dt);
                    idleClock += dt;
                    if (idleClock >= 7f) { idleClock = 0f; ReplayGuide(false); }
                    if (globalLeft <= 0f) EndRun("time");
                    else if (orderLeft <= 0f) TimeOutOrder();
                }
                else if (phase == Phase.Reveal)
                {
                    revealClock += dt;
                    if (revealClock >= (revealCorrect ? 1.55f : 1.35f)) FinishReveal();
                }
                AnimateWorld(dt);
            }
            UpdateUi(dt);
            ApplyResponsiveLayout();
        }

        void ShowTitle()
        {
            phase = Phase.Title;
            dragKind = DragKind.None;
            current = null;
            deck.Clear();
            endReason = "";
            orderIndex = 0;
            titlePressed = false;
            repairMode = false;
            tapMarbleArmed = false;
            spinTapArmed = false;
            tapSlotArmed = -1;
            st.score = 0;
            st.lives = GuseulRules.StartLives;
            st.level = 1;
            st.solved = 0;
            st.firstAttempts = 0;
            st.firstCorrect = 0;
            st.band3FirstCorrect = 0;
            st.masteryPossible = true;
            st.submissionLatched = false;
            st.onboarding = false;
            st.frozen = true;
            st.combo = 0;
            st.currentBand = 0;
            st.targetN = 0;
            st.targetD = 1;
            st.fixedABlue = 0;
            st.currentProblem = "";
            st.prompt = "";
            st.misconceptionId = "";
            st.lastAction = "title";
            SetSlotsByCount(2, true);
            ShowPhaseUi();
            MgfBridge.NotifyChanged();
        }

        void StartPractice()
        {
            runSerial++;
            rng = new System.Random(unchecked(Environment.TickCount ^ runSerial * 104729));
            phase = Phase.Practice;
            current = GuseulRules.Practice();
            endReason = "";
            repairMode = false;
            practiceSwapped = false;
            tapMarbleArmed = false;
            spinTapArmed = false;
            tapSlotArmed = -1;
            currentFirst = true;
            globalLeft = 180f;
            orderLeft = 999f;
            idleClock = 0f;
            st.score = 0;
            st.lives = GuseulRules.StartLives;
            st.level = 1;
            st.solved = 0;
            st.firstAttempts = 0;
            st.firstCorrect = 0;
            st.band3FirstCorrect = 0;
            st.masteryPossible = true;
            st.combo = 0;
            st.submissionLatched = false;
            st.onboarding = true;
            st.frozen = true;
            st.misconceptionId = "";
            st.lastAction = "practice-start";
            SetSlotsByCount(2, false);
            BindCurrent();
            ResetWorldForOrder(true);
            ShowPhaseUi();
            ReplayGuide(false);
            MgfSfx.Play("whoosh", .35f);
            MgfBridge.NotifyChanged();
        }

        void StartRunDirect()
        {
            runSerial++;
            rng = new System.Random(unchecked(Environment.TickCount ^ runSerial * 130363));
            phase = Phase.Playing;
            endReason = "";
            repairMode = false;
            tapMarbleArmed = false;
            spinTapArmed = false;
            tapSlotArmed = -1;
            orderIndex = 0;
            globalLeft = 180f;
            idleClock = 0f;
            st.score = 0;
            st.lives = GuseulRules.StartLives;
            st.level = 1;
            st.solved = 0;
            st.firstAttempts = 0;
            st.firstCorrect = 0;
            st.band3FirstCorrect = 0;
            st.masteryPossible = true;
            st.combo = 0;
            st.submissionLatched = false;
            st.onboarding = false;
            st.frozen = false;
            st.misconceptionId = "";
            st.lastAction = "run-start";
            if (CountBlue() < 0 || CountBlue() > 6) SetSlotsByCount(2, false);
            BuildRuntimeDeck();
            LoadOrder();
            ShowPhaseUi();
            MgfBridge.NotifyChanged();
        }

        void BeginRunAfterPractice()
        {
            int keepBlue = CountBlue();
            StartRunDirect();
            SetSlotsByCount(keepBlue, false);
            current.currentBlue = keepBlue;
            current.prompt = GuseulRules.Make(current.band, current.k, current.j, keepBlue, 1,
                current.observedBlue, current.observedTrials, current.id).prompt;
            BindCurrent();
            RefreshProblemUi();
            ShowToast("실전 시작 · 만든 구성은 다음 주문에도 남는다", 3.2f);
        }

        void BuildRuntimeDeck()
        {
            deck.Clear();
            List<MarbleProblem> built = GuseulRules.BuildRunDeck(rng, CountBlue());
            deck.AddRange(built);
        }

        void LoadOrder()
        {
            if (orderIndex >= deck.Count) { EndRun(IsMastery() ? "clear" : "mastery"); return; }
            current = deck[orderIndex];
            current.currentBlue = CountBlue();
            MarbleProblem refreshed = GuseulRules.Make(current.band, current.k, current.j, CountBlue(), orderIndex + 1,
                current.observedBlue, current.observedTrials, current.id);
            current.prompt = refreshed.prompt;
            current.answer = refreshed.answer;
            current.targetN = refreshed.targetN;
            current.targetD = refreshed.targetD;
            currentFirst = true;
            repairMode = false;
            tapMarbleArmed = false;
            spinTapArmed = false;
            tapSlotArmed = -1;
            orderLeft = current.band == 1 ? 12f : current.band == 2 ? 14f : 18f;
            idleClock = 0f;
            st.level = current.band;
            st.submissionLatched = false;
            st.misconceptionId = "";
            st.lastAction = "order-" + (orderIndex + 1);
            BindCurrent();
            ResetWorldForOrder(false);
            RefreshProblemUi();
            if (orderIndex == 3) ShowConceptBridge("일어나지 않을 확률 = 1 − 원래 사건의 확률");
            else if (orderIndex == 6) ShowConceptBridge("서로 영향을 끼치지 않는 두 사건은 유리한 경우를 곱한다");
        }

        bool IsMastery()
        {
            return st.solved >= GuseulRules.OrderCount
                && st.firstCorrect >= GuseulRules.RequiredFirstCorrect
                && st.band3FirstCorrect >= GuseulRules.RequiredBand3FirstCorrect;
        }

        bool MasteryStillPossible()
        {
            int remainingFirstAttempts = Mathf.Max(0, GuseulRules.OrderCount - st.firstAttempts);
            int band3Attempts = Mathf.Clamp(st.firstAttempts - 6, 0, 4);
            int remainingBand3Attempts = Mathf.Max(0, 4 - band3Attempts);
            return st.firstCorrect + remainingFirstAttempts >= GuseulRules.RequiredFirstCorrect
                && st.band3FirstCorrect + remainingBand3Attempts >= GuseulRules.RequiredBand3FirstCorrect;
        }

        void BindCurrent()
        {
            st.currentProblem = current == null ? "" : current.id;
            st.prompt = current == null ? "" : current.prompt;
            st.currentBand = current == null ? 0 : current.band;
            st.targetN = current == null ? 0 : current.targetN;
            st.targetD = current == null ? 1 : current.targetD;
            st.fixedABlue = current == null ? 0 : current.j;
            st.currentBlue = CountBlue();
            for (int i = 0; i < 6; i++) st.slots[i] = blueSlots[i] ? 1 : 0;
        }

        int CountBlue()
        {
            int n = 0;
            for (int i = 0; i < blueSlots.Length; i++) if (blueSlots[i]) n++;
            return n;
        }

        void SetSlotsByCount(int blueCount, bool title)
        {
            blueCount = Mathf.Clamp(blueCount, 0, 6);
            for (int i = 0; i < 6; i++) blueSlots[i] = i < blueCount;
            if (!title && rng != null)
            {
                for (int i = 5; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    bool t = blueSlots[i]; blueSlots[i] = blueSlots[j]; blueSlots[j] = t;
                }
            }
            BindCurrent();
            RefreshMarbles(false);
        }

        void ChangeSlot(int slot, bool blue)
        {
            if (slot < 0 || slot >= 6) return;
            if (blueSlots[slot] == blue)
            {
                RefuseInput(blue ? "이미 파랑 구슬이다 · 다른 색 슬롯으로 옮기시오" : "이미 흰 구슬이다 · 다른 색 슬롯으로 옮기시오");
                return;
            }
            blueSlots[slot] = blue;
            st.lastAction = blue ? "swap-blue" : "swap-white";
            st.currentBlue = CountBlue();
            if (phase == Phase.Practice && blue && CountBlue() == 3) practiceSwapped = true;
            BindCurrent();
            SnapMarbleVisual(slot, blue);
            RefreshProblemUi();
            if (phase == Phase.Practice && practiceSwapped) AdvancePracticeGuide();
            MgfSfx.Play("pop", .22f);
            MgfBridge.NotifyChanged();
        }

        void SubmitCase()
        {
            if ((phase != Phase.Practice && phase != Phase.Playing) || current == null) return;
            if (phase == Phase.Practice && !practiceSwapped)
            {
                RefuseInput("파랑 구슬을 흰 슬롯으로 먼저 옮기시오");
                ReplayGuide(true);
                return;
            }
            revealPractice = phase == Phase.Practice;
            revealCorrect = GuseulRules.IsCorrect(current, CountBlue());
            st.submissionLatched = true;
            st.lastAction = "spin-submit";
            if (revealPractice)
            {
                if (revealCorrect) { st.score = 100; st.solved = 1; }
                else st.misconceptionId = GuseulRules.ClassifyWrong(current, CountBlue());
            }
            else
            {
                if (currentFirst)
                {
                    st.firstAttempts++;
                    if (revealCorrect)
                    {
                        st.firstCorrect++;
                        if (current.band == 3) st.band3FirstCorrect++;
                    }
                    currentFirst = false;
                }
                if (revealCorrect)
                {
                    st.combo++;
                    st.solved++;
                    st.score += repairMode ? 20 : 100 + Mathf.Min(60, st.combo * 10) + Mathf.RoundToInt(orderLeft * 2f);
                    st.misconceptionId = "";
                }
                else
                {
                    st.combo = 0;
                    st.lives = Mathf.Max(0, st.lives - 1);
                    st.misconceptionId = GuseulRules.ClassifyWrong(current, CountBlue());
                }
            }
            hitStopLeft = revealCorrect ? .075f : 0f;
            revealClock = 0f;
            phase = Phase.Reveal;
            dragKind = DragKind.None;
            BindCurrent();
            BeginRevealVisual(revealCorrect);
            ShowPhaseUi();
            MgfBridge.NotifyChanged();
        }

        void FinishReveal()
        {
            if (phase != Phase.Reveal) return;
            CompleteRevealVisual(revealCorrect);
            if (revealPractice)
            {
                if (revealCorrect) BeginRunAfterPractice();
                else
                {
                    phase = Phase.Practice;
                    practiceSwapped = CountBlue() == 3;
                    st.submissionLatched = false;
                    st.lastAction = "practice-repair";
                    ResetWorldForOrder(true);
                    ShowPhaseUi();
                    ReplayGuide(true);
                    MgfBridge.NotifyChanged();
                }
                return;
            }

            if (!revealCorrect)
            {
                if (st.lives <= 0) { EndRun("bolts"); return; }
                phase = Phase.Playing;
                repairMode = true;
                orderLeft = 6f;
                st.submissionLatched = false;
                st.lastAction = "repair";
                ShowWrongReason(st.misconceptionId);
                ResetWorldForOrder(false);
                ShowPhaseUi();
                MgfBridge.NotifyChanged();
                return;
            }

            orderIndex++;
            if (orderIndex >= GuseulRules.OrderCount) { EndRun(IsMastery() ? "clear" : "mastery"); return; }
            phase = Phase.Playing;
            LoadOrder();
            ShowPhaseUi();
            MgfBridge.NotifyChanged();
        }

        void TimeOutOrder()
        {
            if (phase != Phase.Playing) return;
            if (currentFirst) { st.firstAttempts++; currentFirst = false; }
            st.lives = Mathf.Max(0, st.lives - 1);
            st.combo = 0;
            st.misconceptionId = "no-dispatch-timeout";
            st.lastAction = "timeout";
            if (st.lives <= 0) { EndRun("bolts"); return; }
            repairMode = true;
            orderLeft = 6f;
            ShowToast("시간 초과 · 같은 궤도 돔을 6초 안에 수리하시오", 2.2f);
            DropBoltVisual();
            MgfSfx.Play("wrong", .32f);
            MgfBridge.NotifyChanged();
        }

        void EndRun(string reason)
        {
            if (phase == Phase.End) return;
            phase = Phase.End;
            dragKind = DragKind.None;
            endReason = reason;
            st.frozen = true;
            st.submissionLatched = true;
            st.lastAction = "end-" + reason;
            ShowEnd(reason);
            ShowPhaseUi();
            MgfSfx.Play(reason == "clear" ? "win" : "lose", .55f);
            MgfBridge.NotifyChanged();
        }

        void HandleInput()
        {
            if (phase == Phase.Title && MgfPointer.Held && !titlePressed)
            {
                titlePressed = true;
                BeginTitlePress();
            }
            if (MgfPointer.Down) HandlePointerDown();
            if (MgfPointer.Held && titlePressed) UpdateTitlePress(MgfPointer.Position);
            if (MgfPointer.Up && titlePressed)
            {
                titlePressed = false;
                EndTitlePress();
                StartPractice();
                return;
            }
            if (MgfPointer.Up && phase == Phase.Title)
            {
                EndTitlePress();
                StartPractice();
                return;
            }
            if (dragKind == DragKind.Marble && MgfPointer.Held) UpdateMarbleDrag(MgfPointer.Position);
            if (dragKind == DragKind.Dispatch && MgfPointer.Held) UpdateDispatchDrag(MgfPointer.Position, dragStartScreen);

            if (MgfPointer.Up && dragKind == DragKind.Marble)
            {
                dragKind = DragKind.None;
                int slot = SlotAtPointer(MgfPointer.Position);
                float moved = Vector2.Distance(MgfPointer.Position, dragStartScreen);
                EndMarbleDrag();
                if (slot < 0 && moved < 14f * Mathf.Max(1f, Screen.width / 390f))
                {
                    tapMarbleArmed = true;
                    tapMarbleBlue = dragBlue;
                    st.lastAction = dragBlue ? "blue-picked" : "white-picked";
                    ShowToast("구슬을 집었다 · 바꿀 돔 슬롯을 누르시오", 2.8f);
                    MgfBridge.NotifyChanged();
                    return;
                }
                if (slot < 0) slot = NearestSlotInWideSnap(MgfPointer.Position);
                if (slot >= 0) ChangeSlot(slot, dragBlue);
                else { RefuseInput("구슬을 유리 돔의 빛나는 슬롯에 놓으시오"); ReplayGuide(true); }
                return;
            }
            if (MgfPointer.Up && dragKind == DragKind.Dispatch)
            {
                dragKind = DragKind.None;
                float logicalScale = Mathf.Max(1f, Screen.width / 390f);
                float moved = MgfPointer.Position.x - dragStartScreen.x;
                // 연습과 실전은 같은 손잡이→궤도 링 드래그만 인정한다.
                if (Mathf.Abs(moved) < 14f * logicalScale)
                {
                    cartridgeRoot.localPosition = cartridgeHome;
                    cartridgeRoot.localScale = Vector3.one;
                    cartridgeRoot.localRotation = Quaternion.identity;
                    dispatchHandle.localPosition = handleHome;
                    spinTapArmed = true;
                    st.lastAction = "spin-handle-picked";
                    ShowToast("회전 손잡이를 잡았다 · 오른쪽 확률 궤도를 누르시오", 2.8f);
                    MgfBridge.NotifyChanged();
                    return;
                }
                bool dispatched = moved > 48f * logicalScale;
                EndDispatchDrag(dispatched);
                if (dispatched) SubmitCase();
                else { RefuseInput("돔 손잡이를 오른쪽 궤도 링까지 끌어 돌리시오"); ReplayGuide(true); }
                return;
            }
            if (MgfPointer.Up && tapSlotArmed >= 0)
            {
                float moved = Vector2.Distance(MgfPointer.Position, dragStartScreen);
                if (moved >= 14f * Mathf.Max(1f, Screen.width / 390f)
                    && WideSnapControl(MgfPointer.Position, out string releaseControl, out _)
                    && (releaseControl == "blue" || releaseControl == "white"))
                {
                    int target = tapSlotArmed;
                    tapSlotArmed = -1;
                    bool pickedBlue = releaseControl == "blue";
                    if (blueSlots[target] != pickedBlue) ChangeSlot(target, pickedBlue);
                    else RefuseInput(pickedBlue ? "선택한 슬롯은 이미 파랑이다" : "선택한 슬롯은 이미 흰색이다");
                }
            }
        }

        void HandlePointerDown()
        {
            st.pointerVersion++;
            st.lastAction = "pointer";
            SpawnTapRipple(MgfPointer.Position);
            MgfBridge.NotifyChanged();
            if (phase == Phase.Title)
            {
                titlePressed = true;
                BeginTitlePress();
                MgfSfx.Play("tap", .24f);
                return;
            }
            if (phase == Phase.End)
            {
                if (restartRect && RectTransformUtility.RectangleContainsScreenPoint(restartRect, MgfPointer.Position, null)) StartPractice();
                else
                {
                    st.lastAction = "end-locked";
                    if (restartRect) MgfFx.Punch(restartRect, .08f, .20f);
                    MgfSfx.Play("wrong", .12f);
                    MgfBridge.NotifyChanged();
                }
                return;
            }
            if (phase == Phase.Reveal)
            {
                RefuseInput("구슬 궤적이 멈출 때까지 잠깐 기다리시오");
                return;
            }
            if (phase != Phase.Practice && phase != Phase.Playing) return;
            idleClock = 0f;
            if (tapMarbleArmed)
            {
                int armedSlot = SlotAtPointer(MgfPointer.Position);
                if (armedSlot < 0) armedSlot = NearestSlotInWideSnap(MgfPointer.Position);
                if (armedSlot >= 0)
                {
                    if (blueSlots[armedSlot] != tapMarbleBlue)
                    {
                        tapMarbleArmed = false;
                        ChangeSlot(armedSlot, tapMarbleBlue);
                    }
                    else RefuseInput(tapMarbleBlue ? "흰 슬롯을 누르시오" : "파랑 슬롯을 누르시오");
                    return;
                }
            }
            if (spinTapArmed && NearOrbitGate(MgfPointer.Position))
            {
                spinTapArmed = false;
                SubmitCase();
                return;
            }
            // 보이는 대상 주변의 넓은 스냅 영역은 연습과 실전에서 완전히 동일하다.
            if (RaycastControl(out string control, out int slot) || WideSnapControl(MgfPointer.Position, out control, out slot))
            {
                st.lastControl = control;
                st.lastSlot = slot;
                if (control == "blue" || control == "white")
                {
                    bool pickedBlue = control == "blue";
                    if (tapSlotArmed >= 0)
                    {
                        int target = tapSlotArmed;
                        tapSlotArmed = -1;
                        if (blueSlots[target] != pickedBlue) ChangeSlot(target, pickedBlue);
                        else RefuseInput(pickedBlue ? "선택한 슬롯은 이미 파랑이다 · 흰 결정을 누르시오" : "선택한 슬롯은 이미 흰색이다 · 파랑 결정을 누르시오");
                        return;
                    }
                    dragBlue = pickedBlue;
                    dragKind = DragKind.Marble;
                    dragStartScreen = MgfPointer.Position;
                    BeginMarbleDrag(dragBlue);
                    return;
                }
                if (control == "slot")
                {
                    tapSlotArmed = slot;
                    dragStartScreen = MgfPointer.Position;
                    PulseSlot(slot);
                    ShowToast(blueSlots[slot]
                        ? "파랑 슬롯 선택 · 바꿀 흰 결정을 누르시오"
                        : "흰 슬롯 선택 · 바꿀 파랑 결정을 누르시오", 2.8f);
                    st.lastAction = "slot-picked";
                    ReplayGuide(true);
                    MgfBridge.NotifyChanged();
                    return;
                }
                if (control == "dispatch")
                {
                    if (phase == Phase.Practice && !practiceSwapped)
                    {
                        RefuseInput("파랑 구슬샘에서 흰 슬롯으로 먼저 옮기시오");
                        ReplayGuide(true);
                        return;
                    }
                    dragKind = DragKind.Dispatch;
                    dragStartScreen = MgfPointer.Position;
                    BeginDispatchDrag();
                    return;
                }
            }
            RefuseInput(phase == Phase.Practice && !practiceSwapped
                ? "왼쪽 파랑 구슬샘에서 시작해 흰 슬롯까지 끌어오시오"
                : "돔 오른쪽 손잡이를 궤도 링까지 끌어 돌리시오");
            ReplayGuide(true);
        }

        void FinishPendingReveal()
        {
            if (phase == Phase.Reveal) { revealClock = 99f; FinishReveal(); }
        }

        // QA 강제 훅은 실제 SubmitCase 판정 경로를 공유한다.
        public void TestStart() { FinishPendingReveal(); StartRunDirect(); }

        public void TestAnswerCorrect()
        {
            FinishPendingReveal();
            if (phase != Phase.Playing || current == null) StartRunDirect();
            SetSlotsByCount(current.k, false);
            SubmitCase();
        }

        public void TestAnswerWrong()
        {
            FinishPendingReveal();
            if (phase != Phase.Playing || current == null) StartRunDirect();
            int wrong = current.k == 6 ? 5 : current.k + 1;
            SetSlotsByCount(wrong, false);
            SubmitCase();
        }

        public string StateJson()
        {
            st.phase = phase == Phase.Title ? "title" : phase == Phase.End ? (endReason == "clear" ? "clear" : "gameover") : "playing";
            st.onboarding = phase == Phase.Practice;
            st.frozen = phase == Phase.Practice || phase == Phase.Reveal || phase == Phase.End;
            st.masteryPossible = MasteryStillPossible();
            BindCurrent();
            FillScreenProbe();
            return JsonUtility.ToJson(st);
        }

        public string ProblemBankJson() => MgfJson.Bank(bank);
    }
}
