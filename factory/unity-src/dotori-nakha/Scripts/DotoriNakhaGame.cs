// 도토리 낙하 — 타이틀, 연습, 8무리 중앙값 캐처, 수리와 실패 상태.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.DotoriNakha
{
    public partial class DotoriNakhaGame : MonoBehaviour, IMgfGame
    {
        // IL2CPP 스트리핑 뒤에도 코드 프리미티브 콜라이더 타입을 보존한다.
        static readonly Type PreserveSphereColliderType = typeof(SphereCollider);
        static readonly Type PreserveCapsuleColliderType = typeof(CapsuleCollider);
        static readonly Type PreserveBoxColliderType = typeof(BoxCollider);

        [Serializable]
        public sealed class GameState : MgfState
        {
            public int round;
            public int attemptedRounds;
            public int firstCorrect;
            public bool firstAttemptRecorded;
            public int selectedIndex;
            public int[] rawWeights = Array.Empty<int>();
            public int[] studentOrder = Array.Empty<int>();
            public int[] manualOrderIds = Array.Empty<int>();
            public string activeProblemId = "";
            public string lastInputKind = "none";
            public string lastResult = "";
            public string misconceptionId = "";
            public bool onboarding;
            public bool repairMode;
            public int inputRevision;
            public int streak;
            public int ropeSegments;
            public bool layoutLand;
            public int railLeftPermille;
            public int railRightPermille;
            public int railYPermille;
        }

        enum Flow { Title, Opening, PracticeOdd, Playing, PracticeEven, Reveal, End }

        readonly GameState st = new GameState();
        readonly List<MgfProblem> bank = new List<MgfProblem>(696);
        List<DotoriProblem> problemPool;
        List<DotoriProblem> deck;
        DotoriProblem current;
        Flow flow = Flow.Title;
        int runSerial;
        bool firstAttempt;
        bool draggingBasket;
        bool draggingTag;
        int draggedTag = -1;
        int tagIndexAtDown = -1;
        bool tagOrderChanged;
        float tagDragTravel;
        float basketDragTravel;
        float lastBasketPointerX;
        int valueAtDown2;
        Vector2 pointerDown;
        bool revealCorrect;
        bool revealPractice;
        float revealClock;
        float openingClock;
        float timeLeft;
        float idleGuide;
        float endClock;
        bool practicedOdd, practicedEven;
        string endReason = "";

        public GameState State => st;
        public DotoriProblem Current => current;
        public float TimeLeft => timeLeft;
        public bool IsPractice => flow == Flow.PracticeOdd || flow == Flow.PracticeEven;
        public bool IsReveal => flow == Flow.Reveal;
        public bool IsTitle => flow == Flow.Title;

        void Awake()
        {
            GC.KeepAlive(PreserveSphereColliderType);
            GC.KeepAlive(PreserveCapsuleColliderType);
            GC.KeepAlive(PreserveBoxColliderType);
            MgfLook.Quality(34f);
            runSerial = (int)((DateTime.Now.Ticks / 10000L) % 900000L) * 7 + 13;
            problemPool = DotoriRules.BuildProblems();
            for (int i = 0; i < problemPool.Count; i++) bank.Add(problemPool[i].ToMgf());
            Prewarm();
            BuildWorld();
            BuildUi();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb = new StringBuilder(65000);
            sb.Append(DotoriGlyphs.All).Append("0123456789.,()+÷=→·g ");
            for (int i = 0; i < bank.Count; i++) sb.Append(bank[i].prompt).Append(bank[i].answer).Append(bank[i].unitConcept);
            MgfText.Prewarm(sb.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(.05f, Time.unscaledDeltaTime);
            Relayout(false);
            HandlePointer();
            HandleKeyboard();

            if (flow == Flow.Opening)
            {
                openingClock += dt;
                if (openingClock >= .28f)
                {
                    if (practicedOdd) BeginRun(); else BeginOddPractice();
                }
            }
            else if (flow == Flow.PracticeOdd || flow == Flow.PracticeEven)
            {
                idleGuide += dt;
                if (idleGuide >= 8f) { idleGuide = 0f; BoostGuide(true); }
            }
            else if (flow == Flow.Playing)
            {
                timeLeft -= dt;
                if (timeLeft <= 0f) { timeLeft = 0f; EndRun("time"); }
                else
                {
                    idleGuide += dt;
                    if (idleGuide >= 8f) { idleGuide = 0f; BoostGuide(false); }
                }
            }
            else if (flow == Flow.Reveal)
            {
                revealClock += dt;
                // 첫 제출 연속 정답이면 운반 밧줄이 이어져 다음 잠금 해제 연출이 0.35초 빨라진다(판정·정답 분포와 무관).
                float hold = revealCorrect ? (st.streak >= 2 && !revealPractice ? .78f : 1.13f) : 1.25f;
                if (revealPractice && revealCorrect) hold = 1.45f;
                if (revealClock >= hold) FinishReveal();
            }
            else if (flow == Flow.End) endClock += dt;
            TickView(dt);
        }

        void ResetState()
        {
            st.score = 0; st.lives = DotoriRules.StartLives; st.level = 1; st.phase = "title"; st.solved = 0;
            st.round = 0; st.attemptedRounds = 0; st.firstCorrect = 0; st.firstAttemptRecorded = false;
            st.selectedIndex = 0; st.rawWeights = Array.Empty<int>(); st.studentOrder = Array.Empty<int>(); st.manualOrderIds = Array.Empty<int>();
            st.activeProblemId = ""; st.lastInputKind = "none"; st.lastResult = ""; st.misconceptionId = "";
            st.onboarding = false; st.repairMode = false; st.inputRevision = 0; st.streak = 0; st.ropeSegments = 0;
            firstAttempt = true; draggingBasket = false; draggingTag = false; draggedTag = -1; tagIndexAtDown = -1;
            tagOrderChanged = false; tagDragTravel = 0f; basketDragTravel = 0f; lastBasketPointerX = 0f;
            revealClock = 0f; openingClock = 0f; idleGuide = 0f; timeLeft = DotoriRules.SessionSeconds; endReason = "";
            for (int i = 0; i < mistakeTally.Length; i++) mistakeTally[i] = 0;
        }

        void ShowTitle()
        {
            ResetState(); flow = Flow.Title; current = null; deck = null; st.phase = "title";
            ResetVisuals(); ShowScreen(); Notify();
        }

        void StartOpening()
        {
            if (flow != Flow.Title && flow != Flow.End) return;
            ResetState(); flow = Flow.Opening; st.phase = "playing"; openingClock = 0f; endClock = 0f;
            BeginTitleDrop(); ShowScreen(); Notify(); MgfSfx.Play("whoosh", .34f);
        }

        void BeginOddPractice()
        {
            flow = Flow.PracticeOdd; st.phase = "playing"; st.onboarding = true; st.level = 1;
            LoadProblem(DotoriRules.PracticeOdd(), true, 4); BoostGuide(true);
        }

        void BeginEvenPractice()
        {
            flow = Flow.PracticeEven; st.phase = "playing"; st.onboarding = true; st.level = 2;
            LoadProblem(DotoriRules.PracticeEven(), true, 4); BoostGuide(true);
        }

        void BeginRun()
        {
            runSerial++;
            ResetState();
            deck = DotoriRules.BuildDeck(problemPool, runSerial);
            flow = Flow.Playing; st.phase = "playing"; st.onboarding = false; timeLeft = DotoriRules.SessionSeconds;
            LoadRound(0); ShowScreen(); Notify();
        }

        void LoadRound(int index)
        {
            if (index >= DotoriRules.TargetSolved)
            {
                EndRun(st.firstCorrect >= DotoriRules.MasteryNeeded ? "clear" : "mastery");
                return;
            }
            flow = Flow.Playing; st.phase = "playing"; st.onboarding = false;
            st.round = index + 1; st.level = deck[index].band;
            LoadProblem(deck[index], false, IndependentStartValue2(runSerial, st.round, deck[index].answer2));
        }

        void LoadProblem(DotoriProblem problem, bool practice, int start2)
        {
            current = problem;
            st.activeProblemId = problem.id;
            st.selectedIndex = Mathf.Clamp(start2 - 2, 0, 22);
            st.rawWeights = (int[])problem.raw.Clone();
            st.studentOrder = (int[])problem.raw.Clone();
            st.manualOrderIds = new int[problem.raw.Length];
            for (int i = 0; i < st.manualOrderIds.Length; i++) st.manualOrderIds[i] = i;
            st.firstAttemptRecorded = false; st.lastResult = ""; st.misconceptionId = ""; st.repairMode = false;
            firstAttempt = true; draggingBasket = false; draggingTag = false; draggedTag = -1; idleGuide = 0f;
            tagIndexAtDown = -1; tagOrderChanged = false; tagDragTravel = 0f; basketDragTravel = 0f;
            LoadProblemVisual(problem, practice); ShowScreen(); Notify();
        }

        static int IndependentStartValue2(int serial, int round, int answer2)
        {
            uint x = unchecked((uint)(serial * 0x45D9F3B + round * 0x119DE1F3));
            x ^= x >> 16; x *= 0x7FEB352D; x ^= x >> 15;
            int value2 = 2 + (int)(x % 23u);
            if (value2 == answer2) value2 = value2 == 24 ? 2 : value2 + 1;
            return value2;
        }

        int SelectedValue2 => st.selectedIndex + 2;

        void Submit(int value2)
        {
            if ((flow != Flow.PracticeOdd && flow != Flow.PracticeEven && flow != Flow.Playing) || current == null) return;
            value2 = Mathf.Clamp(value2, 2, 24); st.selectedIndex = value2 - 2;
            revealPractice = IsPractice;
            revealCorrect = DotoriRules.IsCorrect(current, value2);

            if (!revealPractice && firstAttempt)
            {
                st.attemptedRounds++; st.firstAttemptRecorded = true;
                if (revealCorrect) { st.firstCorrect++; st.streak++; st.ropeSegments = Math.Min(8, st.ropeSegments + 1); }
                else { st.streak = 0; st.ropeSegments = Math.Max(0, st.ropeSegments - 1); }
            }

            if (revealCorrect)
            {
                st.lastResult = "correct"; st.misconceptionId = "";
                if (!revealPractice)
                {
                    st.solved++;
                    st.score += firstAttempt ? 100 : 20;
                }
            }
            else
            {
                st.lastResult = "wrong";
                st.misconceptionId = DotoriRules.ClassifyMisconception(current, value2);
                if (!revealPractice && firstAttempt) TallyMistake(st.misconceptionId);
                if (!revealPractice)
                {
                    st.lives = Math.Max(0, st.lives - 1);
                    st.score = Math.Max(0, st.score - 10);
                    timeLeft = Mathf.Max(0f, timeLeft - 2f);
                    st.repairMode = true;
                }
                firstAttempt = false;
            }

            draggingBasket = false; draggingTag = false; st.lastInputKind = "basket_release";
            BeginRevealVisual(revealCorrect, revealPractice, value2, st.misconceptionId);

            // 걸쇠 0 또는 첫 제출 오답 3회는 그 프레임에 입력을 잠그고 즉시 실패한다.
            if (!revealPractice && !revealCorrect && (st.lives <= 0 || st.attemptedRounds - st.firstCorrect >= 3))
            {
                EndRun(st.lives <= 0 ? "latches" : "mastery");
                return;
            }
            flow = Flow.Reveal; st.phase = "playing"; revealClock = 0f; ShowScreen(); Notify();
        }

        void FinishReveal()
        {
            if (flow != Flow.Reveal) return;
            FinishRevealVisual(revealCorrect);
            if (revealPractice)
            {
                if (revealCorrect)
                {
                    if (current.id == "practice-odd") { practicedOdd = true; BeginRun(); }
                    else { practicedEven = true; flow = Flow.Playing; st.onboarding = false; LoadRound(st.solved); }
                }
                else
                {
                    flow = current.id == "practice-odd" ? Flow.PracticeOdd : Flow.PracticeEven;
                    st.phase = "playing"; st.lastResult = ""; st.selectedIndex = 0;
                    RestoreAfterWrong(); LoadProblemVisual(current, true); BoostGuide(true); ShowScreen(); Notify();
                }
                return;
            }
            if (revealCorrect)
            {
                if (st.solved >= DotoriRules.TargetSolved) EndRun(st.firstCorrect >= DotoriRules.MasteryNeeded ? "clear" : "mastery");
                else if (st.solved == 2 && !practicedEven) BeginEvenPractice();
                else LoadRound(st.solved);
            }
            else
            {
                flow = Flow.Playing; st.phase = "playing"; st.lastResult = "";
                RestoreAfterWrong(); ShowScreen(); Notify();
            }
        }

        void EndRun(string reason)
        {
            if (flow == Flow.End) return;
            bool clear = reason == "clear" && st.solved == DotoriRules.TargetSolved && st.firstCorrect >= DotoriRules.MasteryNeeded && st.lives > 0 && timeLeft > 0f;
            if (clear) st.score += Mathf.CeilToInt(timeLeft) * 5;
            flow = Flow.End; st.phase = clear ? "clear" : "gameover"; endReason = reason; endClock = 0f;
            draggingBasket = false; draggingTag = false; st.lastInputKind = "locked";
            ShowEndVisual(clear, reason); ShowScreen(); Notify(); MgfSfx.Play(clear ? "win" : "lose", .55f);
            int best = PlayerPrefs.GetInt("dotori-nakha-best-first", 0);
            if (st.firstCorrect > best)
            {
                PlayerPrefs.SetInt("dotori-nakha-best-first", st.firstCorrect);
                PlayerPrefs.Save();
            }
        }

        void HandlePointer()
        {
            if (MgfPointer.Down)
            {
                pointerDown = MgfPointer.Position; valueAtDown2 = SelectedValue2; st.inputRevision++; idleGuide = 0f;
                if (flow == Flow.Title)
                {
                    st.lastInputKind = "title_knot"; Ripple(pointerDown, "바구니 매듭을 풀었습니다"); StartOpening(); return;
                }
                if (flow == Flow.End)
                {
                    if (endClock < .7f) { st.lastInputKind = "busy"; Ripple(pointerDown, "결과를 정리하는 중입니다"); Notify(); return; }
                    st.lastInputKind = "restart_tag"; Ripple(pointerDown, "새 저장고를 엽니다"); StartOpening(); return;
                }
                if (flow == Flow.Opening || flow == Flow.Reveal)
                {
                    st.lastInputKind = "busy"; Ripple(pointerDown, flow == Flow.Opening ? "가지가 열리는 중입니다" : "도토리 낙하 중입니다"); Notify(); return;
                }
                if (flow != Flow.Playing && flow != Flow.PracticeOdd && flow != Flow.PracticeEven) return;

                float y = pointerDown.y / Mathf.Max(1f, Screen.height);
                float x01 = pointerDown.x / Mathf.Max(1f, Screen.width);
                bool landNow = MgfBridge.IsLandLayout;
                // 정렬 가지(학생이 순서를 만드는 줄)만 끌 수 있다. 위의 원자료 꾸러미는 보존되는 근거라 움직이지 않는다.
                bool tagBand = current != null && (landNow
                    ? x01 >= .07f && x01 <= .67f && Mathf.Abs(y - SortRowY()) <= .065f
                    : Mathf.Abs(y - SortRowY()) <= .05f);
                bool rawBand = current != null && y > SortRowY() + (landNow ? .065f : .05f) && y <= .87f;
                bool basketBand = landNow ? y <= .37f : y <= .41f;

                // 연습 자료는 이미 정렬되어 있고 목표가 공개되어 있다. 연습에서는 화면 어디서
                // 드래그를 시작해도 바구니 조작으로 캡처해 첫 사용자의 경로 오인을 없앤다.
                // 탭은 아래 12px 이동 게이트를 통과하지 못한다.
                if (IsPractice)
                {
                    draggingBasket = true; draggingTag = false; st.lastInputKind = "practice_basket_drag";
                    basketDragTravel = 0f; lastBasketPointerX = pointerDown.x;
                    st.selectedIndex = Value2FromScreen(pointerDown.x) - 2;
                    BeginBasketDrag(); Notify();
                }
                else if (tagBand)
                {
                    draggedTag = TagIndexFromScreen(pointerDown.x, st.studentOrder.Length);
                    draggingTag = draggedTag >= 0; draggingBasket = false; st.lastInputKind = "sort_drag";
                    tagIndexAtDown = draggedTag; tagDragTravel = 0f; tagOrderChanged = false;
                    BeginTagDrag(draggedTag); MgfSfx.Play("tap", .12f); Notify();
                }
                else if (basketBand)
                {
                    draggingBasket = true; draggingTag = false; st.lastInputKind = "basket_drag";
                    basketDragTravel = 0f; lastBasketPointerX = pointerDown.x;
                    st.selectedIndex = Value2FromScreen(pointerDown.x) - 2;
                    BeginBasketDrag(); Notify();
                }
                else if (rawBand)
                {
                    draggingBasket = draggingTag = false; st.lastInputKind = "raw_tap";
                    Ripple(pointerDown, "원자료는 그대로 둡니다 · 아래 정렬 가지의 무게표를 끄시오");
                    PointAtSortRow(); MgfSfx.Play("pop", .10f); Notify();
                }
                else
                {
                    draggingBasket = draggingTag = false; st.lastInputKind = "refused";
                    Ripple(pointerDown, "아래 바구니 손잡이를 끌거나 무게표를 옮기시오"); BoostGuide(IsPractice); MgfSfx.Play("wrong", .10f); Notify();
                }
            }

            if (draggingBasket && MgfPointer.Held)
            {
                basketDragTravel += Mathf.Abs(MgfPointer.Position.x - lastBasketPointerX);
                lastBasketPointerX = MgfPointer.Position.x;
                // 브라우저/터치 드라이버에 따라 pointer-up이 캔버스 밖에서 유실될 수 있다.
                // 연습에서만 12px 이상의 실제 끌기 자체를 완료 입력으로 받아 목표점에 놓는다.
                if (IsPractice && current != null && basketDragTravel >= Mathf.Max(12f, Screen.width * .015f))
                {
                    st.selectedIndex = current.answer2 - 2;
                    UpdateBasketDrag();
                    Submit(SelectedValue2);
                    return;
                }
                int next2 = Value2FromScreen(MgfPointer.Position.x);
                // 연습에서만 목표 주변 두 반눈금(±1 g)을 이동 중 정확한 눈금으로 흡착한다.
                // 실전에는 적용하지 않아 정답 누설이나 판정 관용이 생기지 않는다.
                if (IsPractice && current != null && Mathf.Abs(next2 - current.answer2) <= 2) next2 = current.answer2;
                if (next2 != SelectedValue2)
                {
                    st.selectedIndex = next2 - 2; st.inputRevision++; UpdateBasketDrag(); Notify();
                }
            }
            else if (draggingTag && MgfPointer.Held && draggedTag >= 0)
            {
                tagDragTravel = Mathf.Max(tagDragTravel, Vector2.Distance(MgfPointer.Position, pointerDown));
                int target = TagIndexFromScreen(MgfPointer.Position.x, st.studentOrder.Length);
                if (target >= 0 && target != draggedTag)
                {
                    int value = st.studentOrder[target]; st.studentOrder[target] = st.studentOrder[draggedTag]; st.studentOrder[draggedTag] = value;
                    int id = st.manualOrderIds[target]; st.manualOrderIds[target] = st.manualOrderIds[draggedTag]; st.manualOrderIds[draggedTag] = id;
                    draggedTag = target; tagOrderChanged = true; st.inputRevision++; UpdateStudentOrder(); MgfSfx.Play("tap", .11f); Notify();
                }
            }

            if (MgfPointer.Up && draggingBasket)
            {
                draggingBasket = false; EndBasketDrag();
                float moved = Mathf.Max(Mathf.Abs(MgfPointer.Position.x - pointerDown.x), basketDragTravel);
                bool inside = MgfPointer.Position.x >= Screen.width * RailLeftFraction() - 8f && MgfPointer.Position.x <= Screen.width * RailRightFraction() + 8f;
                if (inside && moved >= Mathf.Max(12f, Screen.width * .015f))
                {
                    // 연습은 답(5 g/5.5 g)을 이미 공개한 조작 튜토리얼이다. 첫 유효 드래그는
                    // 유령 손의 도착점으로 자석 보정해 '잡기→끌기→놓기' 경로를 한 번에 학습시킨다.
                    if (IsPractice && current != null)
                        st.selectedIndex = current.answer2 - 2;
                    Submit(SelectedValue2);
                }
                else
                {
                    st.lastInputKind = "basket_tap";
                    Ripple(MgfPointer.Position, inside ? "손잡이를 12px 이상 끌어 눈금에서 놓으시오" : "레일 안에서 손을 놓으시오");
                    BoostGuide(IsPractice); Notify();
                }
            }
            else if (MgfPointer.Up && draggingTag)
            {
                draggingTag = false; EndTagDrag(draggedTag);
                bool noMove = !tagOrderChanged && tagDragTravel < Mathf.Max(8f, Screen.width * .012f) && draggedTag == tagIndexAtDown;
                if (noMove)
                {
                    st.lastInputKind = "sort_tap";
                    Ripple(MgfPointer.Position, "무게표를 좌우로 끌어 순서를 직접 만드시오");
                    BoostGuide(false); MgfSfx.Play("wrong", .10f);
                }
                else st.lastInputKind = "sort_release";
                draggedTag = -1; tagIndexAtDown = -1; tagDragTravel = 0f; tagOrderChanged = false; Notify();
            }
        }

        readonly int[] mistakeTally = new int[4];

        void TallyMistake(string id)
        {
            if (id == DotoriRules.RawMiddle) mistakeTally[0]++;
            else if (id == DotoriRules.EvenLow || id == DotoriRules.EvenHigh) mistakeTally[1]++;
            else if (id == DotoriRules.RangeMidpoint) mistakeTally[2]++;
            else mistakeTally[3]++;
        }

        // 결과 화면의 오개념 되짚기: 실제 첫 제출 오답과 일치한 경우만 보여 준다.
        string MostCommonMistake()
        {
            int best = -1, n = 0;
            for (int i = 0; i < mistakeTally.Length; i++) if (mistakeTally[i] > n) { n = mistakeTally[i]; best = i; }
            switch (best)
            {
                case 0: return "정렬 전 가운데를 쓴 무리 " + n;
                case 1: return "짝수 개에서 한 값만 쓴 무리 " + n;
                case 2: return "최솟값·최댓값의 중간을 쓴 무리 " + n;
                case 3: return "가운데 순번을 다시 셀 무리 " + n;
                default: return "";
            }
        }

        void HandleKeyboard()
        {
            if (flow != Flow.Playing && flow != Flow.PracticeOdd && flow != Flow.PracticeEven) return;
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                st.selectedIndex = Math.Max(0, st.selectedIndex - 1); st.lastInputKind = "keyboard"; st.inputRevision++; UpdateBasketDrag(); Notify();
            }
            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                st.selectedIndex = Math.Min(22, st.selectedIndex + 1); st.lastInputKind = "keyboard"; st.inputRevision++; UpdateBasketDrag(); Notify();
            }
            if (Input.GetKeyDown(KeyCode.Space)) Submit(SelectedValue2);
        }

        int Value2FromScreen(float x)
        {
            float u = Mathf.InverseLerp(Screen.width * RailLeftFraction(), Screen.width * RailRightFraction(), x);
            return Mathf.Clamp(2 + Mathf.RoundToInt(u * 22f), 2, 24);
        }

        int TagIndexFromScreen(float x, int count)
        {
            if (count <= 0) return -1;
            float x01 = x / Mathf.Max(1f, Screen.width);
            int best = 0; float bestD = 9f;
            for (int i = 0; i < count; i++)
            {
                float d = Mathf.Abs(SortSlotX(i, count) - x01);
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        float SortSlotX(int i, int count)
        {
            float u = count <= 1 ? .5f : i / (float)(count - 1);
            return MgfBridge.IsLandLayout ? .12f + .50f * u : .09f + .82f * u;
        }

        float RailLeftFraction() => MgfBridge.IsLandLayout ? .08f : .075f;
        float RailRightFraction() => MgfBridge.IsLandLayout ? .92f : .925f;
        float RailYFraction() => MgfBridge.IsLandLayout ? .22f : .27f;
        float SortRowY() => MgfBridge.IsLandLayout ? .51f : .565f;
        void Notify() => MgfBridge.NotifyChanged();

        public void TestStart() => BeginRun();

        void EnsureTestPlaying()
        {
            if (flow == Flow.Reveal) FinishReveal();
            if (flow != Flow.Playing) BeginRun();
        }

        public void TestAnswerCorrect() { EnsureTestPlaying(); Submit(current.answer2); }
        public void TestAnswerWrong() { EnsureTestPlaying(); Submit(current.answer2 == 24 ? 23 : current.answer2 + 1); }

        public string StateJson()
        {
            st.layoutLand = MgfBridge.IsLandLayout;
            st.railLeftPermille = Mathf.RoundToInt(RailLeftFraction() * 1000f);
            st.railRightPermille = Mathf.RoundToInt(RailRightFraction() * 1000f);
            st.railYPermille = Mathf.RoundToInt(RailYFraction() * 1000f);
            return JsonUtility.ToJson(st);
        }

        public string ProblemBankJson() => MgfJson.Bank(bank);
    }
}
