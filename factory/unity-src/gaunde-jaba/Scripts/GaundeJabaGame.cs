// 가운데 잡아 — 상태 머신, 그물 눈금 드래그, 자료 정리 줄 입력.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.GaundeJaba
{
    public partial class GaundeJabaGame : MonoBehaviour, IMgfGame
    {
        // GameObject.CreatePrimitive가 IL2CPP 스트리핑 뒤에도 런타임 콜라이더 타입을 만들 수 있게 보존한다.
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
            public int netValue2 = 2;
            public int[] rawValues = Array.Empty<int>();
            public int[] studentOrder = Array.Empty<int>();
            public string lastInputKind = "none";
            public string activeProblemId = "";
            public string lastResult = "";
            public string misconceptionId = "";
            public bool onboarding;
            public bool repairMode;
            public int inputRevision;
            public bool layoutLand;
            public int railLeftPermille;
            public int railRightPermille;
            public int railYPermille;
        }

        enum Flow { Title, Opening, PracticeOdd, Playing, PracticeEven, Reveal, End }

        readonly GameState st = new GameState();
        readonly List<MgfProblem> bank = new List<MgfProblem>(516);
        List<MedianProblem> problemPool;
        List<MedianProblem> deck;
        MedianProblem current;
        Flow flow = Flow.Title;
        int runSerial;
        bool firstAttempt;
        bool draggingNet;
        bool draggingTag;
        bool practiceGuidedDrag;
        int draggedTag = -1;
        bool revealCorrect;
        bool revealPractice;
        float revealClock;
        float openingClock;
        float timeLeft;
        float idleGuide;
        Vector2 pointerDown;
        float lastNetPointerX;
        float netDragTravel;
        int valueAtDown;
        string endReason = "";

        public GameState State => st;
        public MedianProblem Current => current;
        public float TimeLeft => timeLeft;
        public bool IsPractice => flow == Flow.PracticeOdd || flow == Flow.PracticeEven;
        public bool IsReveal => flow == Flow.Reveal;
        public bool IsTitle => flow == Flow.Title;

        void Awake()
        {
            GC.KeepAlive(PreserveSphereColliderType);
            GC.KeepAlive(PreserveCapsuleColliderType);
            GC.KeepAlive(PreserveBoxColliderType);
            MgfLook.Quality(36f);
            problemPool = MedianRules.BuildProblems();
            for (int i = 0; i < problemPool.Count; i++) bank.Add(problemPool[i].ToMgf());
            Prewarm();
            BuildWorld();
            BuildUi();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb = new StringBuilder(42000);
            sb.Append("가운데잡아시간띠의가운데를접어라중학교1학년자료의정리와해석중앙값크기순짝수개가운데두값의평균관측시간초도자추종이꽃직조매듭첫시도정답실전연습잘못된표식을고치시오여기를잡아");
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
                if (openingClock >= .42f) BeginOddPractice();
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
                if (revealClock >= (revealCorrect ? 1.12f : 1.28f)) FinishReveal();
            }
            TickView(dt);
        }

        void ResetState()
        {
            st.score = 0; st.lives = MedianRules.StartLives; st.level = 1; st.phase = "title"; st.solved = 0;
            st.round = 0; st.attemptedRounds = 0; st.firstCorrect = 0; st.firstAttemptRecorded = false;
            st.netValue2 = 2; st.rawValues = Array.Empty<int>(); st.studentOrder = Array.Empty<int>();
            st.lastInputKind = "none"; st.activeProblemId = ""; st.lastResult = ""; st.misconceptionId = "";
            st.onboarding = false; st.repairMode = false; st.inputRevision = 0;
            draggingNet = false; draggingTag = false; practiceGuidedDrag = false; draggedTag = -1; firstAttempt = true;
            netDragTravel = 0f; lastNetPointerX = 0f;
            revealClock = 0f; openingClock = 0f; idleGuide = 0f; timeLeft = MedianRules.SessionSeconds; endReason = "";
        }

        void ShowTitle()
        {
            ResetState(); flow = Flow.Title; current = null; deck = null; st.phase = "title";
            ResetVisuals(); ShowScreen(); Notify();
        }

        void StartOpening()
        {
            if (flow != Flow.Title && flow != Flow.End) return;
            ResetState(); flow = Flow.Opening; st.phase = "playing"; openingClock = 0f;
            BeginTitleFold(); ShowScreen(); Notify(); MgfSfx.Play("whoosh", .34f);
        }

        void BeginOddPractice()
        {
            flow = Flow.PracticeOdd; st.phase = "playing"; st.onboarding = true; st.level = 1;
            LoadProblem(MedianRules.PracticeOdd(), true, 2); BoostGuide(true);
        }

        void BeginEvenPractice()
        {
            flow = Flow.PracticeEven; st.phase = "playing"; st.onboarding = true; st.level = 2;
            LoadProblem(MedianRules.PracticeEven(), true, 2); BoostGuide(true);
        }

        void BeginRun(bool deterministic)
        {
            runSerial++; ResetState(); deck = MedianRules.BuildDeck(problemPool, runSerial, deterministic);
            flow = Flow.Playing; st.phase = "playing"; st.onboarding = false; timeLeft = MedianRules.SessionSeconds;
            LoadRound(0); ShowScreen(); Notify();
        }

        void LoadRound(int index)
        {
            if (index >= MedianRules.TargetSolved)
            {
                EndRun(st.firstCorrect >= MedianRules.MasteryNeeded ? "clear" : "mastery"); return;
            }
            flow = Flow.Playing; st.phase = "playing"; st.onboarding = false;
            st.round = index + 1; st.level = deck[index].band;
            int start2 = IndependentStartValue2(runSerial, st.round, deck[index].answer2);
            LoadProblem(deck[index], false, start2);
        }

        void LoadProblem(MedianProblem p, bool practice, int start2)
        {
            current = p; st.activeProblemId = p.id; st.netValue2 = start2;
            st.rawValues = (int[])p.raw.Clone(); st.studentOrder = (int[])p.raw.Clone();
            st.firstAttemptRecorded = false; st.lastResult = ""; st.misconceptionId = ""; st.repairMode = false;
            firstAttempt = true; draggingNet = false; draggingTag = false; practiceGuidedDrag = false; draggedTag = -1; idleGuide = 0f;
            netDragTravel = 0f;
            LoadProblemVisual(p, practice); ShowScreen(); Notify();
        }

        static int IndependentStartValue2(int serial, int round, int answer2)
        {
            uint x = unchecked((uint)(serial * 0x45D9F3B + round * 0x119DE1F3));
            x ^= x >> 16; x *= 0x7FEB352D; x ^= x >> 15;
            int v = 2 + (int)(x % 23u);
            if (v == answer2) v = v == 24 ? 2 : v + 1;
            return v;
        }

        void Submit(int value2)
        {
            if ((flow != Flow.PracticeOdd && flow != Flow.PracticeEven && flow != Flow.Playing) || current == null) return;
            value2 = Mathf.Clamp(value2, 2, 24); st.netValue2 = value2;
            revealPractice = IsPractice; revealCorrect = MedianRules.IsCorrect(current, value2);
            if (!revealPractice && firstAttempt)
            {
                st.attemptedRounds++; st.firstAttemptRecorded = true;
                if (revealCorrect) st.firstCorrect++;
            }
            if (revealCorrect)
            {
                st.lastResult = "correct"; st.misconceptionId = "";
                if (!revealPractice)
                {
                    st.solved++; st.score += firstAttempt ? 100 : 20;
                }
            }
            else
            {
                st.lastResult = "wrong"; st.misconceptionId = MedianRules.ClassifyMisconception(current, value2);
                if (!revealPractice) { st.lives = Math.Max(0, st.lives - 1); st.repairMode = true; }
                firstAttempt = false;
            }
            draggingNet = false; draggingTag = false; st.lastInputKind = "net_release";
            BeginRevealVisual(revealCorrect, revealPractice, value2, st.misconceptionId);
            if (!revealPractice && !revealCorrect && st.lives <= 0)
            {
                EndRun("knots"); return;
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
                    if (current.id == "practice-odd") BeginRun(false);
                    else { flow = Flow.Playing; st.onboarding = false; LoadRound(st.solved); }
                }
                else
                {
                    flow = current.id == "practice-odd" ? Flow.PracticeOdd : Flow.PracticeEven;
                    st.phase = "playing"; st.netValue2 = 2; st.lastResult = ""; LoadProblemVisual(current, true); BoostGuide(true); ShowScreen(); Notify();
                }
                return;
            }
            if (revealCorrect)
            {
                if (st.solved >= MedianRules.TargetSolved) EndRun(st.firstCorrect >= MedianRules.MasteryNeeded ? "clear" : "mastery");
                else if (st.solved == 2) BeginEvenPractice();
                else LoadRound(st.solved);
            }
            else
            {
                flow = Flow.Playing; st.phase = "playing"; st.lastResult = "";
                st.netValue2 = IndependentStartValue2(runSerial + st.inputRevision, st.round, current.answer2);
                RestoreAfterWrong(); ShowScreen(); Notify();
            }
        }

        void EndRun(string reason)
        {
            if (flow == Flow.End) return;
            bool clear = reason == "clear" && st.solved == MedianRules.TargetSolved && st.firstCorrect >= MedianRules.MasteryNeeded && st.lives > 0 && timeLeft > 0f;
            flow = Flow.End; st.phase = clear ? "clear" : "gameover"; endReason = reason;
            draggingNet = false; draggingTag = false; st.lastInputKind = "locked";
            ShowEndVisual(clear, reason); ShowScreen(); Notify(); MgfSfx.Play(clear ? "win" : "lose", .55f);
            int best = PlayerPrefs.GetInt("gaunde-jaba-best-first", 0);
            if (st.firstCorrect > best) { PlayerPrefs.SetInt("gaunde-jaba-best-first", st.firstCorrect); PlayerPrefs.Save(); }
        }

        void HandlePointer()
        {
            if (MgfPointer.Down)
            {
                pointerDown = MgfPointer.Position; valueAtDown = st.netValue2; st.inputRevision++; idleGuide = 0f;
                if (flow == Flow.Title) { st.lastInputKind = "title_net"; Ripple(pointerDown, "시간 표본 기록을 엽니다"); StartOpening(); return; }
                if (flow == Flow.End) { st.lastInputKind = "restart"; Ripple(pointerDown, "새 표본 기록을 엽니다"); StartOpening(); return; }
                if (flow == Flow.Opening || flow == Flow.Reveal)
                {
                    st.lastInputKind = "busy"; Ripple(pointerDown, flow == Flow.Opening ? "기록이 펼쳐지는 중입니다" : "크기순 근거를 확인하시오"); Notify(); return;
                }
                if (flow != Flow.Playing && flow != Flow.PracticeOdd && flow != Flow.PracticeEven) return;
                float y = pointerDown.y / Mathf.Max(1f, Screen.height);
                float x01 = pointerDown.x / Mathf.Max(1f, Screen.width);
                // 첫 화면에서는 화면 대부분의 실제 drag가 같은 도자 추 연습으로 이어진다.
                // 탭은 아래 Up 게이트에서 거절되며, 다음 화면부터 정리 줄 drag를 정상 제공한다.
                bool guidedPracticeBand = flow == Flow.PracticeOdd && y <= .78f;
                bool tagBand = !guidedPracticeBand && current != null && (MgfBridge.IsLandLayout
                    ? x01 <= .37f && y >= .28f && y <= .64f
                    : y >= .46f && y <= .68f);
                if (tagBand)
                {
                    draggedTag = TagIndexFromScreen(pointerDown.x, st.studentOrder.Length);
                    draggingTag = draggedTag >= 0; draggingNet = false; st.lastInputKind = "sort_drag";
                    BeginTagDrag(draggedTag); Notify();
                }
                // 92px 도자 추보다 넓은 하단 43% 포인터 띠를 잡기 영역으로 둔다.
                // 첫 고정 연습만 실제 수평 드래그가 확인되면 명시된 5초 눈금에 흡착해 조작을 가르친다.
                else if (guidedPracticeBand || y <= .43f)
                {
                    draggingNet = true; draggingTag = false; st.lastInputKind = "net_drag";
                    practiceGuidedDrag = IsPractice && current != null && current.id == "practice-odd";
                    netDragTravel = 0f; lastNetPointerX = pointerDown.x;
                    if (!practiceGuidedDrag) st.netValue2 = Value2FromScreen(pointerDown.x);
                    BeginNetDrag(); Notify();
                }
                else
                {
                    draggingNet = draggingTag = false; st.lastInputKind = "refused";
                    Ripple(pointerDown, "아래 도자 추를 끌거나 정리 줄의 기록을 옮기시오"); BoostGuide(IsPractice); MgfSfx.Play("wrong", .10f); Notify();
                }
            }

            if (draggingNet && MgfPointer.Held)
            {
                netDragTravel += Mathf.Abs(MgfPointer.Position.x - lastNetPointerX);
                lastNetPointerX = MgfPointer.Position.x;
                int next = Value2FromScreen(MgfPointer.Position.x);
                if (next != st.netValue2) { st.netValue2 = next; st.inputRevision++; UpdateNetDrag(); Notify(); }
            }
            else if (draggingTag && MgfPointer.Held && draggedTag >= 0)
            {
                int target = TagIndexFromScreen(MgfPointer.Position.x, st.studentOrder.Length);
                if (target >= 0 && target != draggedTag)
                {
                    int tmp = st.studentOrder[target]; st.studentOrder[target] = st.studentOrder[draggedTag]; st.studentOrder[draggedTag] = tmp;
                    draggedTag = target; st.inputRevision++; UpdateStudentOrder(); MgfSfx.Play("tap", .11f); Notify();
                }
            }

            if (MgfPointer.Up && draggingNet)
            {
                draggingNet = false;
                float moved = Mathf.Abs(MgfPointer.Position.x - pointerDown.x);
                // 첫 연습은 탭과 구분되는 실제 수평 이동이면 충분하다. 제출 게이트와 같은 규모로
                // 맞춰, 짧게 끌어 본 학생도 오답으로 벌하지 않고 목표 눈금까지 안내한다.
                if (practiceGuidedDrag && netDragTravel >= Mathf.Max(5f, Screen.width * .0125f))
                {
                    st.netValue2 = current.answer2;
                    UpdateNetDrag();
                }
                practiceGuidedDrag = false;
                EndNetDrag();
                if (moved >= Mathf.Max(4f, Screen.width * .012f) || st.netValue2 != valueAtDown) Submit(st.netValue2);
                else
                {
                    st.lastInputKind = "net_tap"; Ripple(MgfPointer.Position, "도자 추를 좌우로 끌어 눈금에서 놓으시오"); BoostGuide(IsPractice); Notify();
                }
            }
            else if (MgfPointer.Up && draggingTag)
            {
                draggingTag = false; EndTagDrag(draggedTag); draggedTag = -1; st.lastInputKind = "sort_release"; Notify();
            }
        }

        void HandleKeyboard()
        {
            if (flow != Flow.Playing && flow != Flow.PracticeOdd && flow != Flow.PracticeEven) return;
            if (Input.GetKeyDown(KeyCode.LeftArrow)) { st.netValue2 = Math.Max(2, st.netValue2 - 1); st.lastInputKind = "keyboard"; st.inputRevision++; UpdateNetDrag(); Notify(); }
            if (Input.GetKeyDown(KeyCode.RightArrow)) { st.netValue2 = Math.Min(24, st.netValue2 + 1); st.lastInputKind = "keyboard"; st.inputRevision++; UpdateNetDrag(); Notify(); }
            if (Input.GetKeyDown(KeyCode.Space)) Submit(st.netValue2);
        }

        int Value2FromScreen(float x)
        {
            float u = Mathf.InverseLerp(Screen.width * RailLeftFraction(), Screen.width * RailRightFraction(), x);
            return Mathf.Clamp(2 + Mathf.RoundToInt(u * 22f), 2, 24);
        }

        int TagIndexFromScreen(float x, int count)
        {
            if (count <= 0) return -1;
            float left = MgfBridge.IsLandLayout ? .04f : .07f;
            float right = MgfBridge.IsLandLayout ? .34f : .93f;
            float u = Mathf.InverseLerp(Screen.width * left, Screen.width * right, x);
            return Mathf.Clamp(Mathf.FloorToInt(u * count), 0, count - 1);
        }

        float RailLeftFraction() => MgfBridge.IsLandLayout ? .08f : .075f;
        float RailRightFraction() => MgfBridge.IsLandLayout ? .92f : .925f;
        float RailYFraction() => MgfBridge.IsLandLayout ? .155f : .18f;
        void Notify() => MgfBridge.NotifyChanged();

        public void TestStart() => BeginRun(true);

        void EnsureTestPlaying()
        {
            if (flow == Flow.Reveal) FinishReveal();
            if (flow != Flow.Playing) BeginRun(true);
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
