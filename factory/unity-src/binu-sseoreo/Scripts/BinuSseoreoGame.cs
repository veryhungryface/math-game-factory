// 비누 썰어 — 상태 머신과 실제 눈금→가로 절단 pointer 입력.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.BinuSseoreo
{
    public partial class BinuSseoreoGame : MonoBehaviour, IMgfGame
    {
        static readonly Type PreserveCapsuleColliderType = typeof(CapsuleCollider);
        static readonly Type PreserveSphereColliderType = typeof(SphereCollider);

        [Serializable]
        public sealed class GameState : MgfState
        {
            public int round;
            public string activeProblemId = "";
            public int attemptCount;
            public int firstAttemptCorrect;
            public int firstAttemptTotal;
            public int remainingOrders;
            public int cutHeight;
            public string gesturePhase = "idle";
            public int inputRevision;
            public int combo;
            public bool repairMode;
            public bool onboarding;
            public bool guideAssist;
            public int practiceMisses;
            public string lastResult = "";
            public string misconceptionId = "";
            public string shape = "";
            public string piece = "";
            public bool layoutLand;
            public int rulerLeftPermille;
            public int rulerBottomPermille;
            public int rulerTopPermille;
            public int exitPermille;
        }

        enum Flow { Title, Opening, Practice, Playing, Reveal, End }

        readonly GameState st = new GameState();
        readonly List<MgfProblem> bank = new List<MgfProblem>(624);
        List<SoapProblem> deck;
        SoapProblem current;
        Flow flow = Flow.Title;
        int runSerial;
        bool firstAttempt;
        bool dragging;
        bool heightLocked;
        bool revealCorrect;
        bool revealPractice;
        bool failAfterReveal;
        float timeLeft;
        float revealClock;
        float openingClock;
        float idleGuide;
        Vector2 pointerDown;
        string endReason = "";

        public GameState State => st;
        public SoapProblem Current => current;
        public float TimeLeft => timeLeft;
        public bool IsPractice => flow == Flow.Practice;
        public bool IsReveal => flow == Flow.Reveal;

        void Awake()
        {
            GC.KeepAlive(PreserveCapsuleColliderType);
            GC.KeepAlive(PreserveSphereColliderType);
            MgfLook.Quality(34f);
            bank.AddRange(BinuRules.BuildBank());
            Prewarm();
            BuildWorld();
            BuildUi();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb = new StringBuilder(26000);
            sb.Append("비누썰어비누를잘라다리를세워라중학교1학년입체도형의성질기포협곡절단높이밑넓이반지름원기둥각기둥아래조각위쪽조각부피리브첫칼여분연습실전완성실패다시자르기막대끝까지그으시오전체높이πcm³");
            for (int i = 0; i < bank.Count; i++) sb.Append(bank[i].prompt).Append(bank[i].answer).Append(bank[i].unitConcept);
            MgfText.Prewarm(sb.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(.05f, Time.unscaledDeltaTime);
            Relayout(false);
            HandlePointer();
            if (flow == Flow.Opening)
            {
                openingClock += dt;
                if (openingClock >= .38f) BeginPractice();
            }
            else if (flow == Flow.Practice)
            {
                idleGuide += dt;
                if (idleGuide >= 8f) { idleGuide = 0f; BoostGuide(true); }
            }
            else if (flow == Flow.Playing)
            {
                timeLeft -= dt;
                if (timeLeft <= 0f) { timeLeft = 0f; EndRun("time"); }
            }
            else if (flow == Flow.Reveal)
            {
                revealClock += dt;
                if (revealClock >= (revealCorrect ? 1.18f : 1.35f)) FinishReveal();
            }
            TickView(dt);
        }

        void ResetState()
        {
            st.score = 0; st.lives = BinuRules.StartLives; st.level = 1; st.solved = 0; st.round = 0;
            st.activeProblemId = ""; st.attemptCount = 0; st.firstAttemptCorrect = 0; st.firstAttemptTotal = 0;
            st.remainingOrders = BinuRules.TargetSolved; st.cutHeight = 13; st.gesturePhase = "idle";
            st.inputRevision = 0; st.combo = 0; st.repairMode = false; st.onboarding = false;
            st.guideAssist = false; st.practiceMisses = 0;
            st.lastResult = ""; st.misconceptionId = ""; st.shape = ""; st.piece = "";
            dragging = false; heightLocked = false; firstAttempt = true; failAfterReveal = false;
            timeLeft = BinuRules.SessionSeconds; revealClock = 0f; idleGuide = 0f; endReason = "";
        }

        void ShowTitle()
        {
            ResetState(); flow = Flow.Title; st.phase = "title"; current = null; deck = null;
            ResetVisuals(); ShowScreen(); Notify();
        }

        void StartOpening()
        {
            if (flow != Flow.Title && flow != Flow.End) return;
            ResetState(); flow = Flow.Opening; st.phase = "playing"; openingClock = 0f;
            BeginTitleSplit(); ShowScreen(); Notify(); MgfSfx.Play("whoosh", .38f);
        }

        void BeginPractice()
        {
            flow = Flow.Practice; st.phase = "playing"; st.onboarding = true;
            current = BinuRules.Practice(); st.cutHeight = 13; st.level = 1; st.activeProblemId = current.id;
            firstAttempt = true; dragging = false; heightLocked = false; st.gesturePhase = "idle";
            LoadProblemVisual(current, true); ShowScreen(); BoostGuide(); Notify();
        }

        void BeginRun(bool deterministic)
        {
            runSerial++; ResetState(); deck = BinuRules.BuildDeck(runSerial, deterministic);
            flow = Flow.Playing; st.phase = "playing"; st.onboarding = false; timeLeft = BinuRules.SessionSeconds;
            LoadRound(0); ShowScreen(); Notify();
        }

        void LoadRound(int index)
        {
            if (index >= BinuRules.TargetSolved) { EndRun(st.firstAttemptCorrect >= BinuRules.MasteryNeeded ? "clear" : "mastery"); return; }
            current = deck[index]; st.round = index + 1; st.level = current.band; st.activeProblemId = current.id;
            st.remainingOrders = BinuRules.TargetSolved - index; st.cutHeight = IndependentStartHeight(runSerial, st.round);
            st.shape = current.solid == SolidKind.Cylinder ? "cylinder" : "rect_prism";
            st.piece = current.piece == PieceKind.Upper ? "upper" : "lower";
            st.repairMode = false; st.lastResult = ""; st.misconceptionId = ""; st.gesturePhase = "idle";
            firstAttempt = true; dragging = false; heightLocked = false;
            LoadProblemVisual(current, false); ShowScreen(); Notify();
        }

        static int IndependentStartHeight(int serial, int round)
        {
            uint x = unchecked((uint)serial * 2246822519u) ^ unchecked((uint)round * 3266489917u) ^ 0x9E3779B9u;
            x ^= x >> 15; x *= 2246822519u; x ^= x >> 13;
            return 1 + (int)(x % 24u);
        }

        void Submit(int selectedHeight)
        {
            if ((flow != Flow.Practice && flow != Flow.Playing) || current == null) return;
            selectedHeight = Mathf.Clamp(selectedHeight, 1, 24);
            st.cutHeight = selectedHeight; st.attemptCount++;
            revealCorrect = BinuRules.IsCorrect(current, selectedHeight);
            revealPractice = flow == Flow.Practice;
            if (!revealPractice && firstAttempt)
            {
                st.firstAttemptTotal++;
                if (revealCorrect) st.firstAttemptCorrect++;
            }
            if (revealCorrect)
            {
                st.lastResult = "correct"; st.misconceptionId = "";
                if (!revealPractice)
                {
                    st.solved++; st.remainingOrders = BinuRules.TargetSolved - st.solved;
                    if (firstAttempt) st.combo++; else st.combo = 0;
                    st.score += firstAttempt ? 100 : 30;
                }
            }
            else
            {
                st.lastResult = "wrong"; st.combo = 0; st.misconceptionId = BinuRules.MisconceptionId(current, selectedHeight);
                if (!revealPractice) { st.lives = Math.Max(0, st.lives - 1); st.repairMode = true; }
                firstAttempt = false;
            }
            int remainingFirst = BinuRules.TargetSolved - st.firstAttemptTotal;
            failAfterReveal = !revealPractice && !revealCorrect && (st.lives <= 0 || st.firstAttemptCorrect + remainingFirst < BinuRules.MasteryNeeded);
            flow = Flow.Reveal; st.phase = "playing"; st.gesturePhase = "submitted"; revealClock = 0f;
            dragging = false; heightLocked = false;
            BeginRevealVisual(revealCorrect, revealPractice, selectedHeight, st.misconceptionId);
            ShowScreen(); Notify();
        }

        void FinishReveal()
        {
            if (flow != Flow.Reveal) return;
            FinishRevealVisual(revealCorrect);
            if (revealPractice)
            {
                if (revealCorrect) BeginRun(false);
                else
                {
                    flow = Flow.Practice; st.phase = "playing"; st.cutHeight = 13; st.gesturePhase = "idle";
                    LoadProblemVisual(current, true); BoostGuide(); ShowScreen(); Notify();
                }
                return;
            }
            if (failAfterReveal) { EndRun(st.lives <= 0 ? "soap" : "mastery"); return; }
            if (revealCorrect)
            {
                if (st.solved >= BinuRules.TargetSolved) EndRun(st.firstAttemptCorrect >= BinuRules.MasteryNeeded ? "clear" : "mastery");
                else { flow = Flow.Playing; st.phase = "playing"; LoadRound(st.solved); }
            }
            else
            {
                flow = Flow.Playing; st.phase = "playing"; st.gesturePhase = "idle"; st.cutHeight = IndependentStartHeight(runSerial + st.attemptCount, st.round);
                ReloadSoapForRepair(); ShowScreen(); Notify();
            }
        }

        void EndRun(string reason)
        {
            if (flow == Flow.End) return;
            bool clear = reason == "clear" && st.solved == BinuRules.TargetSolved && st.firstAttemptCorrect >= BinuRules.MasteryNeeded;
            flow = Flow.End; st.phase = clear ? "clear" : "gameover"; st.gesturePhase = "locked"; endReason = reason;
            dragging = false; ShowEndVisual(clear, reason); ShowScreen(); Notify(); MgfSfx.Play(clear ? "win" : "lose", .55f);
            if (clear)
            {
                int best = PlayerPrefs.GetInt("binu-sseoreo-best", 0);
                if (st.score > best) { PlayerPrefs.SetInt("binu-sseoreo-best", st.score); PlayerPrefs.Save(); }
            }
        }

        void HandlePointer()
        {
            if (MgfPointer.Down)
            {
                pointerDown = MgfPointer.Position; st.inputRevision++;
                if (flow == Flow.Title) { Ripple(pointerDown, "절단 손잡이를 당깁니다"); StartOpening(); return; }
                if (flow == Flow.End) { Ripple(pointerDown, "새 비누를 준비합니다"); StartOpening(); return; }
                if (flow == Flow.Opening || flow == Flow.Reveal)
                {
                    Ripple(pointerDown, flow == Flow.Opening ? "협곡이 열리는 중입니다" : "절단면을 확인하시오"); Notify(); return;
                }
                if (flow != Flow.Practice && flow != Flow.Playing) return;
                float x = pointerDown.x / Mathf.Max(1f, Screen.width);
                float y = pointerDown.y / Mathf.Max(1f, Screen.height);
                bool onRuler = x <= RulerXFraction() + .14f && y >= RulerBottomFraction() - .035f && y <= RulerTopFraction() + .035f;
                bool assistedPracticeStart = flow == Flow.Practice && st.guideAssist && x >= .06f && x <= .94f && y >= .14f && y <= .76f;
                if (onRuler || assistedPracticeStart)
                {
                    dragging = true; heightLocked = false;
                    st.cutHeight = flow == Flow.Practice && st.guideAssist ? 4 : HeightFromScreen(pointerDown.y);
                    st.gesturePhase = "aiming"; idleGuide = 0f;
                    BeginWire(st.cutHeight); MgfSfx.Play("tap", .22f); Notify();
                }
                else
                {
                    dragging = false; st.gesturePhase = "refused";
                    if (flow == Flow.Practice) st.practiceMisses++;
                    Ripple(pointerDown, "빛나는 4 cm 손잡이에서 시작하시오");
                    BoostGuide(flow == Flow.Practice && st.practiceMisses >= 2); MgfSfx.Play("wrong", .12f); Notify();
                }
            }

            if (dragging && MgfPointer.Held)
            {
                Vector2 p = MgfPointer.Position;
                float x = p.x / Mathf.Max(1f, Screen.width);
                if (flow == Flow.Practice && st.guideAssist)
                {
                    st.cutHeight = 4; heightLocked = true; st.gesturePhase = "slicing";
                    float projectedX = Mathf.Max(p.x, pointerDown.x + Mathf.Abs(p.x - pointerDown.x));
                    UpdateWire(4, new Vector2(projectedX, HeightToScreen(4)));
                }
                else if (!heightLocked && x < Mathf.Lerp(RulerXFraction(), ExitFraction(), .28f))
                {
                    int h = flow == Flow.Practice && st.guideAssist ? 4 : HeightFromScreen(p.y);
                    if (h != st.cutHeight) { st.cutHeight = h; UpdateWire(h, p); st.inputRevision++; Notify(); }
                }
                else heightLocked = true;
                st.gesturePhase = heightLocked ? "slicing" : "aiming";
                UpdateWire(st.cutHeight, p);
            }

            if (dragging && MgfPointer.Up)
            {
                dragging = false;
                Vector2 p = MgfPointer.Position;
                float x = p.x / Mathf.Max(1f, Screen.width);
                float dy = Mathf.Abs(p.y - HeightToScreen(st.cutHeight));
                bool assistedPracticeStroke = flow == Flow.Practice && st.guideAssist && Vector2.Distance(p, pointerDown) >= Screen.width * .08f;
                if (assistedPracticeStroke) Submit(4);
                else if (x >= ExitFraction() - .04f && dy <= Screen.height * .12f) Submit(st.cutHeight);
                else
                {
                    st.gesturePhase = "cancelled"; CancelWire();
                    if (flow == Flow.Practice) st.practiceMisses++;
                    Ripple(p, "빛나는 띠 끝까지 수평으로 그으시오");
                    BoostGuide(flow == Flow.Practice && st.practiceMisses >= 2); MgfSfx.Play("wrong", .13f); Notify();
                }
            }
        }

        int HeightFromScreen(float screenY)
        {
            float bottom = Screen.height * RulerBottomFraction(), top = Screen.height * RulerTopFraction();
            float u = Mathf.InverseLerp(bottom, top, screenY);
            return Mathf.Clamp(Mathf.RoundToInt(u * BinuRules.TotalHeight), 1, 24);
        }

        float RulerBottomFraction() => MgfBridge.IsLandLayout ? .12f : .18f;
        float RulerTopFraction() => MgfBridge.IsLandLayout ? .82f : .64f;
        float RulerXFraction() => MgfBridge.IsLandLayout ? .10f : .18f;
        float ExitFraction() => MgfBridge.IsLandLayout ? .58f : .87f;
        float HeightToScreen(int h) => Mathf.Lerp(Screen.height * RulerBottomFraction(), Screen.height * RulerTopFraction(), h / 25f);
        void Notify() => MgfBridge.NotifyChanged();

        public void TestStart() => BeginRun(true);

        void EnsureTestPlaying()
        {
            if (flow == Flow.Reveal) FinishReveal();
            if (flow != Flow.Playing) BeginRun(true);
        }

        public void TestAnswerCorrect() { EnsureTestPlaying(); Submit(current.answerHeight); }
        public void TestAnswerWrong() { EnsureTestPlaying(); Submit(current.answerHeight == 24 ? 23 : current.answerHeight + 1); }

        public string StateJson()
        {
            st.layoutLand = MgfBridge.IsLandLayout;
            st.rulerLeftPermille = Mathf.RoundToInt(RulerXFraction() * 1000f);
            st.rulerBottomPermille = Mathf.RoundToInt(RulerBottomFraction() * 1000f);
            st.rulerTopPermille = Mathf.RoundToInt(RulerTopFraction() * 1000f);
            st.exitPermille = Mathf.RoundToInt(ExitFraction() * 1000f);
            return JsonUtility.ToJson(st);
        }

        public string ProblemBankJson() => MgfJson.Bank(bank);
    }
}
