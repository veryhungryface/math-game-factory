using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.GoseumdochiUcheguk
{
    public partial class GoseumdochiUchegukGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int zone;
            public int substage;
            public int firstAttemptTotal;
            public int firstAttemptCorrect;
            public int remainingTime;
            public int stamps;
            public int pointerVersion;
            public string pressedAddressIds = "";
            public string stencilWindowIds = "";
            public string shutterId = "";
            public string lastFailureReason = "";
            public string problemId = "";
            public string prompt = "";
            public string misconceptionId = "";
            public bool onboarding;
            public bool frozen;
        }

        enum RunPhase { Title, Practice, Playing, Feedback, End }
        enum DragKind { None, Card, Stamp, Bell, Shutter, TitleCord }

        readonly State st = new State();
        readonly List<MgfProblem> bank = new List<MgfProblem>();
        readonly List<AddressMark> built = new List<AddressMark>();
        readonly int[] windows = { -1, -1, -1, -1, -1 };
        readonly bool[] shutters = new bool[5];

        RunPhase runPhase = RunPhase.Title;
        AddressProblem current;
        DragKind dragKind;
        int dragCardValue = -1;
        int dragShutterIndex = -1;
        Vector2 dragStart;
        bool zoneFirstClean;
        bool feedbackCorrect;
        bool feedbackPractice;
        bool practiceFirstDone;
        bool zoneSubmitted;
        float feedbackClock;
        float runLeft;
        float idleClock;
        float titleClock;
        float stampPull;
        float bellPull;
        string endReason = "";

        void Awake()
        {
            MgfLook.Quality(38f);
            AddressRules.ExhaustiveInvariantCheck();
            bank.AddRange(AddressRules.BuildBank());
            PrewarmText();
            BuildWorld();
            BuildUi();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void PrewarmText()
        {
            var chars = new HashSet<char>();
            string fixedText = "고슴도치우체국카드로번지를달아라중학교2학년경우의수봉인끈당겨개국카드를십의자리와일의자리창에놓고손잡이를당기시오모든유효주소를한번씩만들고종줄을당겨제출하시오압인스텐실셔터철거집게회장부회장대표첫시도방수가방결정비십의자리후보수남은일의자리후보수전체주소수다시배달하기연습실전구역";
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

            if (runPhase == RunPhase.Playing)
            {
                runLeft = Mathf.Max(0f, runLeft - dt);
                idleClock += dt;
                if (idleClock >= 8f) { idleClock = 0f; ReplayGuide(true); }
                if (runLeft <= 0f) EndRun(false, "결정비가 우편 군락에 도착했다");
            }
            else if (runPhase == RunPhase.Practice)
            {
                idleClock += dt;
                if (idleClock >= 8f) { idleClock = 0f; ReplayGuide(true); }
            }
            else if (runPhase == RunPhase.Feedback)
            {
                feedbackClock += dt;
                if (feedbackClock >= (feedbackCorrect ? 1.55f : 1.35f)) FinishFeedback();
            }

            UpdateWorld(dt);
            UpdateUi(dt);
        }

        void ShowTitle()
        {
            runPhase = RunPhase.Title;
            current = AddressRules.Practice();
            dragKind = DragKind.None; built.Clear(); ClearStencil();
            st.score = 0; st.lives = 3; st.level = 1; st.solved = 0; st.zone = 0; st.substage = 0;
            st.firstAttemptTotal = 0; st.firstAttemptCorrect = 0; st.remainingTime = 150; st.stamps = 0;
            st.pointerVersion = 0; st.phase = "title"; st.onboarding = false; st.frozen = false;
            st.problemId = ""; st.prompt = ""; st.lastFailureReason = ""; st.misconceptionId = "";
            practiceFirstDone = false; zoneSubmitted = false; zoneFirstClean = true; endReason = "";
            ResetWorldForRun(); RefreshSnapshot(); SetPhaseVisuals(); MgfBridge.NotifyChanged();
        }

        void StartPractice()
        {
            ResetRun();
            runPhase = RunPhase.Practice;
            current = AddressRules.Practice();
            st.phase = "playing"; st.zone = 0; st.level = 1; st.substage = 0;
            st.onboarding = true; st.frozen = true; st.problemId = current.id; st.prompt = current.prompt;
            practiceFirstDone = false; built.Clear(); ClearStencil();
            ResetWorldForProblem(); SetPhaseVisuals(); RefreshProblemUi(); ReplayGuide(false);
            ShowToast("카드 1을 십의 자리에 끌어 놓으시오", 3.5f);
            MgfSfx.Play("whoosh", .28f); RefreshSnapshot(); MgfBridge.NotifyChanged();
        }

        void StartDirect()
        {
            ResetRun();
            runPhase = RunPhase.Playing; st.phase = "playing"; st.onboarding = false; st.frozen = false;
            LoadZone(1, 0); SetPhaseVisuals(); MgfBridge.NotifyChanged();
        }

        void ResetRun()
        {
            built.Clear(); ClearStencil(); dragKind = DragKind.None;
            st.score = 0; st.lives = 3; st.level = 1; st.solved = 0; st.zone = 1; st.substage = 0;
            st.firstAttemptTotal = 0; st.firstAttemptCorrect = 0; st.stamps = 0;
            st.lastFailureReason = ""; st.misconceptionId = ""; st.remainingTime = 150;
            runLeft = 150f; idleClock = 0f; zoneSubmitted = false; zoneFirstClean = true; endReason = "";
            ResetWorldForRun();
        }

        void BeginMainAfterPractice()
        {
            PlayerPrefs.SetInt("goseumdochi-ucheguk.onboardingSeen", 1);
            PlayerPrefs.Save();
            built.Clear(); ClearStencil(); zoneSubmitted = false;
            st.score = 0; st.lives = 3; st.solved = 0; st.firstAttemptTotal = 0; st.firstAttemptCorrect = 0; st.stamps = 0;
            st.lastFailureReason = ""; st.misconceptionId = "";
            runLeft = 150f; runPhase = RunPhase.Playing; st.phase = "playing"; st.onboarding = false; st.frozen = false;
            LoadZone(1, 0); SetPhaseVisuals(); ShowToast("실전 · 모든 주소를 만든 뒤 종줄을 당기시오", 3f);
        }

        void LoadZone(int zone, int substage)
        {
            st.zone = zone; st.substage = substage; st.level = zone;
            zoneSubmitted = false;
            if (zone == 1) current = AddressRules.ZoneOne();
            else if (zone == 2) current = AddressRules.ZoneTwo();
            else current = substage == 0 ? AddressRules.ZoneThreeOrdered() : AddressRules.ZoneThreeUnordered();
            st.problemId = current.id; st.prompt = current.prompt; st.misconceptionId = ""; st.lastFailureReason = "";
            built.Clear(); ClearStencil();
            if (substage == 0) zoneFirstClean = true;
            ResetWorldForProblem(); RefreshProblemUi(); RefreshSnapshot(); ReplayGuide(false); MgfBridge.NotifyChanged();
        }

        void HandleInput()
        {
            Vector2 pos = MgfPointer.Position;
            if (MgfPointer.Down)
            {
                st.pointerVersion++; st.remainingTime = Mathf.CeilToInt(runLeft);
                SpawnTapRipple(pos); MgfBridge.NotifyChanged();
                dragStart = pos;

                if (runPhase == RunPhase.Title)
                {
                    if (Hit(titleBagRt, pos)) { dragKind = DragKind.TitleCord; PressTitleBag(); }
                    else if (Hit(helpRt, pos)) { ShowToast("카드를 창에 놓고 압인한 뒤, 모든 주소가 있으면 종줄을 당기시오", 4f); PulseTitleBag(); }
                    else { Refuse("가운데 봉인 우편낭의 끈을 당기시오"); PulseTitleBag(); }
                    return;
                }
                if (runPhase == RunPhase.End)
                {
                    if (Hit(restartRt, pos)) StartPractice();
                    else { Refuse("빛나는 우편낭을 눌러 다시 배달하시오"); PulseRestart(); }
                    return;
                }
                if (runPhase == RunPhase.Feedback)
                {
                    Refuse("셔터 표시를 확인하시오"); return;
                }

                int house = HitHouse(pos);
                if (house >= 0)
                {
                    if (runPhase == RunPhase.Practice) Refuse("연습 주소는 남겨 두고 다음 압인을 준비하시오");
                    else RemoveHouse(house);
                    return;
                }
                if (Hit(stampHandleRt, pos)) { dragKind = DragKind.Stamp; stampPull = 0f; AnticipateStamp(); return; }
                if (Hit(bellCordRt, pos)) { dragKind = DragKind.Bell; bellPull = 0f; AnticipateBell(); return; }

                int shutter = HitShutter(pos);
                if (shutter >= 0)
                {
                    if (windows[shutter] < 0) { Refuse("카드를 놓은 창의 셔터만 닫을 수 있다"); PulseWindow(shutter); }
                    else { dragKind = DragKind.Shutter; dragShutterIndex = shutter; }
                    return;
                }

                int card = HitCard(pos);
                if (card >= 0)
                {
                    dragKind = DragKind.Card; dragCardValue = current.cards[card]; BeginCardDrag(card, pos); MgfSfx.Play("tap", .14f); return;
                }

                if (Hit(tensSlotRt, pos)) { Refuse("아래 카드 한 장을 끌어 십의 자리에 놓으시오"); PulseCards(); return; }
                int w = HitWindow(pos);
                if (w >= 0) { Refuse("아래 카드 한 장을 끌어 일의 자리 창에 놓으시오"); PulseCards(); return; }
                Refuse("카드를 끌거나 압인 손잡이·종줄을 아래로 당기시오"); ReplayGuide(true);
            }

            if (MgfPointer.Held)
            {
                if (dragKind == DragKind.Card) UpdateCardDrag(pos);
                else if (dragKind == DragKind.Stamp) { stampPull = Mathf.Max(0f, dragStart.y - pos.y); UpdateStampPull(stampPull); }
                else if (dragKind == DragKind.Bell) { bellPull = Mathf.Max(0f, dragStart.y - pos.y); UpdateBellPull(bellPull); }
                else if (dragKind == DragKind.Shutter) UpdateShutterDrag(dragShutterIndex, pos.x - dragStart.x);
                else if (dragKind == DragKind.TitleCord) UpdateTitleCord(Mathf.Max(0f, dragStart.y - pos.y));
            }

            if (MgfPointer.Up)
            {
                if (dragKind == DragKind.TitleCord)
                {
                    ReleaseTitleBag();
                    if (Vector2.Distance(pos, dragStart) >= 18f || Hit(titleBagRt, pos))
                    {
                        if (PlayerPrefs.GetInt("goseumdochi-ucheguk.onboardingSeen", 0) == 1) StartDirect(); else StartPractice();
                    }
                }
                else if (dragKind == DragKind.Card) FinishCardDrop(pos);
                else if (dragKind == DragKind.Stamp)
                {
                    ReleaseStamp();
                    if (stampPull >= Mathf.Max(44f, Screen.height * .052f)) StampCurrent();
                    else { Refuse("압인 손잡이를 아래로 더 길게 당기시오"); ReplayGuide(true); }
                }
                else if (dragKind == DragKind.Bell)
                {
                    ReleaseBell();
                    if (bellPull >= Mathf.Max(48f, Screen.height * .058f)) SubmitBell();
                    else { Refuse("종줄을 아래로 더 길게 당겨 제출하시오"); PulseBell(); }
                }
                else if (dragKind == DragKind.Shutter)
                {
                    if (windows[dragShutterIndex] >= 0) ToggleShutter(dragShutterIndex);
                }
                dragKind = DragKind.None; dragCardValue = -1; dragShutterIndex = -1;
            }
        }

        void FinishCardDrop(Vector2 pos)
        {
            bool placed = false;
            if (Hit(tensSlotRt, pos)) { SetTens(dragCardValue); placed = true; }
            else
            {
                int w = HitWindow(pos);
                if (w >= 0) { SetWindow(w, dragCardValue); placed = true; }
            }
            EndCardDrag(placed, pos);
            if (!placed) { Refuse("카드를 십의 자리나 일의 자리 창 안에 놓으시오"); ReplayGuide(true); }
            else { idleClock = 0f; RefreshSnapshot(); UpdatePracticeGuide(); MgfBridge.NotifyChanged(); }
        }

        int tens = -1;

        void SetTens(int value)
        {
            tens = value; SetTensVisual(value); MgfSfx.Play("pop", .18f); StampPlatePress(tensSlotRt);
        }

        void SetWindow(int index, int value)
        {
            windows[index] = value; shutters[index] = false; SetWindowVisual(index, value, false);
            MgfSfx.Play("pop", .16f); StampPlatePress(windowRts[index]);
        }

        void ToggleShutter(int index)
        {
            shutters[index] = !shutters[index]; SetWindowVisual(index, windows[index], shutters[index]);
            MgfSfx.Play(shutters[index] ? "tap" : "pop", .15f); idleClock = 0f;
            RefreshSnapshot(); UpdatePracticeGuide(); MgfBridge.NotifyChanged();
        }

        void StampCurrent()
        {
            if (runPhase != RunPhase.Practice && runPhase != RunPhase.Playing) return;
            if (tens < 0) { Refuse("십의 자리 카드를 먼저 놓으시오"); PulseTens(); return; }
            var seconds = new List<int>();
            for (int i = 0; i < windows.Length; i++) if (windows[i] >= 0 && !shutters[i]) seconds.Add(windows[i]);
            if (seconds.Count == 0) { Refuse("열린 일의 자리 창이 하나 이상 필요하다"); PulseWindows(); return; }

            if (runPhase == RunPhase.Practice)
            {
                if (!practiceFirstDone)
                {
                    if (tens != 1 || seconds.Count != 1 || seconds[0] != 2)
                    {
                        Refuse("연습에서는 1을 십의 자리, 2를 열린 일의 자리 창에 놓으시오"); ReplayGuide(true); return;
                    }
                    AddStamped(tens, seconds); practiceFirstDone = true; st.stamps++;
                    ShowMathReveal("십의 자리 1 · 일의 자리 2 → 주소 12");
                    ShowToast("이제 십의 자리 2, 열린 창 1·3으로 한 열을 압인하시오", 4f);
                    UpdatePracticeGuide(); RefreshSnapshot(); MgfBridge.NotifyChanged(); return;
                }
                bool exact = tens == 2 && seconds.Count == 2 && seconds.Contains(1) && seconds.Contains(3);
                bool twoClosed = false;
                for (int i = 0; i < windows.Length; i++) if (windows[i] == 2 && shutters[i]) twoClosed = true;
                if (!exact || !twoClosed)
                {
                    Refuse("카드 2의 창은 셔터로 닫고 1·3 창만 열어 압인하시오"); ReplayGuide(true); return;
                }
                AddStamped(tens, seconds); st.stamps++;
                ShowMathReveal("십의 자리 후보 수 × 남은 일의 자리 후보 수 = 전체 주소 수");
                feedbackPractice = true; feedbackCorrect = true; feedbackClock = 0f; runPhase = RunPhase.Feedback;
                MgfSfx.Play("correct", .38f); CourierZip(true); RefreshSnapshot(); MgfBridge.NotifyChanged(); return;
            }

            AddStamped(tens, seconds); st.stamps++;
            StampImpact(seconds.Count); idleClock = 0f; RefreshSnapshot(); MgfBridge.NotifyChanged();
        }

        void AddStamped(int first, IList<int> seconds)
        {
            for (int i = 0; i < seconds.Count; i++)
            {
                // 원시 입력을 보존한다. 순서 없는 모드의 정규화는 제출 판정에서만 한다.
                var mark = new AddressMark(first, seconds[i], false);
                built.Add(mark); SpawnHouse(built.Count - 1, mark, i * .09f);
            }
            MgfSfx.Play(seconds.Count >= 3 ? "whoosh" : "pop", .34f);
            RebuildHouseList();
        }

        void RemoveHouse(int index)
        {
            if (index < 0 || index >= built.Count) return;
            RemoveHouseVisual(index); built.RemoveAt(index); RebuildHouseList(); RebuildWorldHouses(built);
            st.score = Math.Max(0, st.score - 5); ShowToast("주소 한 채를 철거했다 · 시간은 계속 흐른다", 2.2f);
            MgfSfx.Play("tap", .13f); RefreshSnapshot(); MgfBridge.NotifyChanged();
        }

        void SubmitBell()
        {
            if (runPhase != RunPhase.Playing) { if (runPhase == RunPhase.Practice) Refuse("연습 압인을 먼저 완성하시오"); return; }
            if (built.Count == 0) { Refuse("주소를 하나 이상 압인한 뒤 종줄을 당기시오"); PulseCards(); return; }
            bool correct = AddressRules.Exact(current, built);
            if (!zoneSubmitted)
            {
                zoneSubmitted = true;
                if (st.zone < 3 || st.substage == 0) st.firstAttemptTotal++;
            }
            Resolve(correct, correct ? "" : AddressRules.Diagnose(current, built));
        }

        void Resolve(bool correct, string misconception)
        {
            if (runPhase != RunPhase.Playing) return;
            feedbackCorrect = correct; feedbackPractice = false; feedbackClock = 0f; runPhase = RunPhase.Feedback;
            st.misconceptionId = misconception; st.lastFailureReason = misconception;
            if (correct)
            {
                st.score += 600 + current.answerSet.Count * 18 + Mathf.CeilToInt(runLeft * .35f);
                if (st.zone < 3 || st.substage == 1)
                {
                    if (zoneFirstClean) st.firstAttemptCorrect++;
                    st.solved++;
                }
                ShowMathReveal(current.reveal);
                CorrectDistrictEvent(built); CourierZip(st.zone == 3 && st.substage == 1);
                MgfSfx.Play("correct", .42f);
            }
            else
            {
                zoneFirstClean = false; st.lives--;
                ShowFailure(misconception); WrongRainBag(st.lives); MgfSfx.Play("wrong", .34f);
                if (st.lives <= 0) endReason = "방수 우편 가방 3개가 모두 젖었다";
            }
            RefreshSnapshot(); MgfBridge.NotifyChanged();
        }

        void FinishFeedback()
        {
            HideReveal();
            if (feedbackPractice)
            {
                BeginMainAfterPractice(); return;
            }
            if (!feedbackCorrect)
            {
                if (st.lives <= 0) EndRun(false, endReason);
                else
                {
                    runPhase = RunPhase.Playing; st.phase = "playing"; idleClock = 0f;
                    RefreshProblemUi(); ReplayGuide(false); MgfBridge.NotifyChanged();
                }
                return;
            }

            if (st.zone == 3 && st.substage == 0)
            {
                LoadZone(3, 1); runPhase = RunPhase.Playing; st.phase = "playing";
                ShowToast("역할이 같으면 가·나와 나·가는 같은 대표 쌍이다", 3.6f); return;
            }
            if (st.zone < 3)
            {
                LoadZone(st.zone + 1, 0); runPhase = RunPhase.Playing; st.phase = "playing"; return;
            }
            bool mastered = st.firstAttemptCorrect >= 2 && zoneFirstClean;
            EndRun(mastered, mastered ? "세 우편 구역의 노선이 모두 이어졌다" : "완성했지만 첫 시도 숙련 조건을 더 연습해야 한다");
        }

        string FailureMessage(string id)
        {
            switch (id)
            {
                case AddressRules.MisconceptionLeadingZero: return "0은 십의 자리에 올 수 없다 · 01 같은 주소의 셔터가 닫혔다";
                case AddressRules.MisconceptionReuseCard: return "카드는 한 장씩이다 · 11처럼 같은 카드를 두 번 쓴 주소를 확인하시오";
                case AddressRules.MisconceptionOrderIgnored: return "회장과 부회장은 역할이 다르다 · 가→나와 나→가는 서로 다르다";
                case AddressRules.MisconceptionOrderDouble: return "대표 2명은 역할이 같다 · 가·나와 나·가는 같은 한 쌍이다";
                case AddressRules.MisconceptionDuplicate: return "같은 주소가 두 번 압인되었다 · 한 채만 남기시오";
                default: return "아직 빠진 주소가 있다 · 십의 자리별 열린 창을 확인하시오";
            }
        }

        void EndRun(bool win, string reason)
        {
            runPhase = RunPhase.End; st.phase = win ? "clear" : reason.Contains("완성했지만") ? "practice_complete" : "gameover";
            st.remainingTime = Mathf.CeilToInt(runLeft); endReason = reason; dragKind = DragKind.None;
            SetEndScreen(win, reason); MgfSfx.Play(win ? "win" : "lose", .42f); MgfBridge.NotifyChanged();
        }

        void ClearStencil()
        {
            tens = -1;
            for (int i = 0; i < windows.Length; i++) { windows[i] = -1; shutters[i] = false; }
        }

        void RefreshSnapshot()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < built.Count; i++)
            {
                if (i > 0) sb.Append(','); sb.Append(AddressRules.MarkLabel(current, built[i]));
            }
            st.pressedAddressIds = sb.ToString();
            sb.Length = 0;
            for (int i = 0; i < windows.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(windows[i] < 0 ? "_" : AddressRules.CardLabel(current, windows[i]));
            }
            st.stencilWindowIds = sb.ToString();
            sb.Length = 0;
            for (int i = 0; i < shutters.Length; i++) if (shutters[i]) { if (sb.Length > 0) sb.Append(','); sb.Append(i); }
            st.shutterId = sb.ToString(); st.remainingTime = Mathf.CeilToInt(runLeft);
        }

        void UpdatePracticeGuide()
        {
            if (runPhase == RunPhase.Practice) ReplayGuide(false);
        }

        // ── IMgfGame. 두 훅 모두 실제 SubmitBell 집합 판정 경로를 그대로 탄다.
        public void TestStart() => StartDirect();

        public void TestAnswerCorrect()
        {
            if (runPhase != RunPhase.Playing) StartDirect();
            built.Clear();
            foreach (var mark in current.answerSet) built.Add(mark);
            RebuildHouseList(); RebuildWorldHouses(built); SubmitBell();
        }

        public void TestAnswerWrong()
        {
            if (runPhase != RunPhase.Playing) StartDirect();
            built.Clear();
            if (current.mode == AddressMode.OrderedDigits && Array.IndexOf(current.cards, 0) >= 0)
                built.Add(new AddressMark(0, current.cards[current.cards.Length - 1], false));
            else built.Add(new AddressMark(current.cards[0], current.cards[0], false));
            RebuildHouseList(); RebuildWorldHouses(built); SubmitBell();
        }

        public string StateJson() => JsonUtility.ToJson(st);
        public string ProblemBankJson() => MgfJson.Bank(bank);
    }
}
