// 곱빼기 빵집 — 실제 hold/release 상태 머신과 테스트 브리지.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.GopbaegiBbangjip
{
    public partial class GopbaegiGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int stock, poured, capacity, combo, firstAttemptTotal, firstAttemptCorrect;
            public int finalBandFirstCorrect, pointerVersion, orderIndex, targetHidden;
            public string problemId = "", prompt = "", misconceptionId = "", actionPhase = "", endReason = "";
            public bool onboarding, holding, adjusting, canAdd, pairAvailable;
        }

        enum Mode { Title, Practice, Playing, Reveal, End }
        readonly State st = new State();
        readonly List<MgfProblem> bank = new List<MgfProblem>();
        List<DoughOrder> deck;
        DoughOrder current;
        Mode mode = Mode.Title;
        int runSerial, orderIndex, attempt, holdStartPoured, lastSecond = -1;
        float sessionLeft, holdClock, tickClock, revealClock, endClock, idleGuide, hitStop;
        bool flowStarted, revealCorrect, revealPractice, revealUnderfill, keyHold, practiceHoldLearned;
        string endReason = "";

        void Awake()
        {
            MgfLook.Quality(34f);
            // WebGL 스트리핑이 실제 입력 콜라이더 타입을 제거하지 않도록 직접 참조한다.
            var boxLink = gameObject.AddComponent<BoxCollider>(); boxLink.enabled = false;
            var sphereLink = gameObject.AddComponent<SphereCollider>(); sphereLink.enabled = false;
            var capsuleLink = gameObject.AddComponent<CapsuleCollider>(); capsuleLink.enabled = false;
            bank.AddRange(GopbaegiRules.BuildBank());
            // UI 메시를 만들기 전에 동적 SDF 아틀라스를 먼저 채워, 첫 입력 때 글리프 UV가 바뀌지 않게 한다.
            BuildWorld(); Prewarm(); BuildUi(); ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb = new StringBuilder("곱빼기빵집세제곱만큼부어라햇빛반죽블록공방계량카트잠금완성작은큰틀부피닮음비주문현재반죽재고허가봉인누른채유지숫자가오르면계산한정확한눈금에서놓으시오폐업영업성공다시열기◆◇³");
            foreach (var p in bank) { sb.Append(p.prompt); sb.Append(p.answer); sb.Append(p.unitConcept); }
            MgfText.Prewarm(sb.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(.05f, Time.unscaledDeltaTime);
            HandleInput();
            if (hitStop > 0f) hitStop -= dt;
            else
            {
                if (mode == Mode.Playing)
                {
                    sessionLeft -= dt;
                    if (sessionLeft <= 0f) EndRun("timer");
                }
                if (mode == Mode.Practice)
                {
                    idleGuide += dt;
                    if (idleGuide >= 8f) { idleGuide = 0f; BoostGuide(); }
                }
                if (st.holding && (mode == Mode.Practice || mode == Mode.Playing)) AdvanceFlow(dt);
                if (mode == Mode.Reveal)
                {
                    revealClock += dt;
                    if (revealClock >= (revealCorrect ? 1.55f : 2.05f)) FinishReveal();
                }
                if (mode == Mode.End) endClock += dt;
            }
            UpdateWorld(dt); UpdateUi(dt);
        }

        void ShowTitle()
        {
            mode = Mode.Title; endReason = ""; current = null; deck = null;
            st.score = 0; st.lives = GopbaegiRules.StartLives; st.level = 1; st.solved = 0;
            st.stock = GopbaegiRules.StartStock; st.poured = 0; st.combo = 0; st.firstAttemptTotal = 0;
            st.firstAttemptCorrect = 0; st.finalBandFirstCorrect = 0; st.pointerVersion = 0; st.orderIndex = 0;
            st.problemId = st.prompt = st.misconceptionId = st.endReason = ""; st.onboarding = false;
            st.holding = false; st.adjusting = false; st.canAdd = false; st.pairAvailable = false; practiceHoldLearned = false;
            ResetWorldForRun(); SetScreen(); MgfBridge.NotifyChanged();
        }

        void ResetRun()
        {
            runSerial++; deck = GopbaegiRules.RunDeck(runSerial); orderIndex = 0;
            st.score = 0; st.lives = GopbaegiRules.StartLives; st.level = 1; st.solved = 0;
            st.stock = GopbaegiRules.StartStock; st.poured = 0; st.combo = 0;
            st.firstAttemptTotal = 0; st.firstAttemptCorrect = 0; st.finalBandFirstCorrect = 0;
            st.orderIndex = 0; st.misconceptionId = st.endReason = ""; st.holding = false; st.adjusting = false; st.canAdd = false;
            sessionLeft = GopbaegiRules.SessionSeconds; attempt = 0; lastSecond = -1; endReason = ""; practiceHoldLearned = false;
            ResetWorldForRun();
        }

        void StartPractice()
        {
            ResetRun(); mode = Mode.Practice; st.onboarding = true; current = GopbaegiRules.Practice();
            BeginOrder(); BoostGuide(); MgfSfx.Play("whoosh", .30f); SetScreen(); MgfBridge.NotifyChanged();
        }

        void StartRunDirect()
        {
            ResetRun(); mode = Mode.Playing; st.onboarding = false; current = deck[0];
            BeginOrder(); SetScreen(); MgfBridge.NotifyChanged();
        }

        void BeginMainAfterPractice()
        {
            st.score = 0; st.solved = 0; st.combo = 0; st.lives = GopbaegiRules.StartLives;
            st.stock = GopbaegiRules.StartStock; st.firstAttemptTotal = 0; st.firstAttemptCorrect = 0;
            st.finalBandFirstCorrect = 0; st.onboarding = false; st.misconceptionId = "";
            orderIndex = 0; sessionLeft = GopbaegiRules.SessionSeconds; mode = Mode.Playing; current = deck[0];
            BeginOrder(); SetGoalAfterPractice();
        }

        void BeginOrder()
        {
            attempt = 0; st.poured = 0; st.capacity = current.capacity; st.targetHidden = 1;
            st.problemId = current.id; st.prompt = current.prompt; st.level = current.band;
            st.orderIndex = orderIndex + 1; st.misconceptionId = ""; st.holding = false; st.adjusting = false; st.canAdd = false;
            st.pairAvailable = PairAvailable(); idleGuide = 0f;
            ResetWorldForOrder(); RefreshOrderUi(); MgfBridge.NotifyChanged();
        }

        bool PairAvailable() => mode == Mode.Playing && orderIndex >= 3 && orderIndex <= 7 && (orderIndex & 1) == 1;

        void SwapPair()
        {
            if (!PairAvailable() || st.poured > 0 || st.holding) { Refuse("붓기 전 주문만 바꿀 수 있습니다"); return; }
            DoughOrder t = deck[orderIndex]; deck[orderIndex] = deck[orderIndex + 1]; deck[orderIndex + 1] = t;
            current = deck[orderIndex]; st.problemId = current.id; st.prompt = current.prompt; st.level = current.band;
            st.capacity = current.capacity; st.pointerVersion++; RefreshOrderUi(); PulseDrum();
            MgfSfx.Play("tap", .22f); MgfBridge.NotifyChanged();
        }

        void BeginHold()
        {
            if (st.stock <= 0 || st.holding) return;
            st.adjusting = false;
            st.holding = true; holdClock = 0f; tickClock = 0f; flowStarted = false; holdStartPoured = st.poured;
            BeginLeverVisual(); SetScreen(); MgfSfx.Play("tap", .24f); MgfBridge.NotifyChanged();
        }

        void AdvanceFlow(float dt)
        {
            holdClock += dt;
            if (!flowStarted && holdClock >= GopbaegiRules.MinHoldSeconds)
            {
                flowStarted = true;
                if (mode == Mode.Practice) practiceHoldLearned = true;
                for (int i = 0; i < 4; i++) AddOneTick();
                MgfSfx.Play("whoosh", .16f);
            }
            if (!flowStarted) return;
            tickClock += dt;
            while (tickClock >= GopbaegiRules.TickSeconds && st.holding)
            {
                tickClock -= GopbaegiRules.TickSeconds;
                AddOneTick();
            }
        }

        void AddOneTick()
        {
            if (st.poured >= current.capacity || (mode == Mode.Playing && st.stock <= 0))
            {
                st.holding = false; EndLeverVisual(); JudgeRelease(); return;
            }
            st.poured++;
            if (mode == Mode.Playing) st.stock--;
            UpdateFillVisual(st.poured, current.capacity, true);
            if ((st.poured % 25) == 0) MgfSfx.Play("pop", .07f);
            MgfBridge.NotifyChanged();
        }

        void ReleaseHold()
        {
            if (!st.holding) return;
            st.holding = false; EndLeverVisual();
            if (!flowStarted || st.poured - holdStartPoured < 4)
            {
                int refund = st.poured - holdStartPoured;
                st.poured = holdStartPoured;
                if (mode == Mode.Playing) st.stock += refund;
                ShowDrip(); SetScreen(); MgfSfx.Play("tap", .09f); MgfBridge.NotifyChanged(); return;
            }
            // 손을 떼는 시점은 제출이 아니라 거친 계량의 종료다. 20 ms 틱을 사람에게
            // 정답 창으로 강요하지 않고, 이후 ±1 cm³ 밸브와 봉인으로 정확한 정수를 확정한다.
            st.adjusting = true;
            ShowFineTune(); SetScreen(); MgfBridge.NotifyChanged();
        }

        void NudgeAmount(int delta)
        {
            if (!st.adjusting || st.holding || current == null) return;
            int next = Mathf.Clamp(st.poured + delta, 0, current.capacity);
            if (next == st.poured) { Refuse(delta < 0 ? "계량조가 비어 있습니다" : "계량조가 가득 찼습니다"); return; }
            int change = next - st.poured;
            if (mode == Mode.Playing && change > 0 && st.stock < change) { Refuse("반죽 재고가 부족합니다"); return; }
            if (mode == Mode.Playing) st.stock -= change; // 되돌린 1 cm³는 봉인 전 계량 카트로 돌아간다.
            st.poured = next;
            UpdateFillVisual(st.poured, current.capacity, false);
            FineTunePulse(delta); MgfSfx.Play("tap", .12f); MgfBridge.NotifyChanged();
        }

        void SealAmount()
        {
            if (!st.adjusting || st.poured < 4) { Refuse("먼저 반죽을 4 cm³ 이상 계량하세요"); return; }
            st.adjusting = false;
            JudgeRelease();
        }

        void JudgeRelease()
        {
            if (current == null || (mode != Mode.Practice && mode != Mode.Playing)) return;
            st.adjusting = false;
            // 발문은 정확한 부피를 요구한다. target±1도 오답이며 허용 범위는 없다.
            revealCorrect = st.poured == current.target;
            revealPractice = mode == Mode.Practice;
            revealUnderfill = st.poured < current.target;
            if (!revealPractice)
            {
                bool first = attempt == 0;
                if (first) st.firstAttemptTotal++;
                attempt++;
                if (revealCorrect)
                {
                    if (first)
                    {
                        st.firstAttemptCorrect++;
                        if (current.band == 3) st.finalBandFirstCorrect++;
                    }
                    st.solved++; st.combo++; st.score += 120 + st.combo * 18 + (current.band - 1) * 35;
                    st.misconceptionId = "";
                }
                else
                {
                    st.lives--; st.combo = 0; st.misconceptionId = GopbaegiRules.Diagnose(current, st.poured);
                }
            }
            else
            {
                st.misconceptionId = revealCorrect ? "" : GopbaegiRules.Diagnose(current, st.poured);
                if (revealCorrect) { st.score = 100; st.solved = 1; }
            }
            st.targetHidden = 0; st.canAdd = !revealCorrect && revealUnderfill && attempt <= 1;
            mode = Mode.Reveal; revealClock = 0f; hitStop = revealCorrect ? .08f : 0f;
            BeginRevealVisual(revealCorrect, st.poured, current, st.misconceptionId);
            if (revealCorrect) MgfSfx.Play("correct", .46f); else MgfSfx.Play("wrong", .34f);
            SetScreen(); MgfBridge.NotifyChanged();
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
                    mode = Mode.Practice; st.misconceptionId = ""; st.targetHidden = 1;
                    if (!revealUnderfill) st.poured = 0;
                    st.holding = false; st.adjusting = false; RefreshOrderUi(); UpdateFillVisual(st.poured, current.capacity, false); BoostGuide();
                }
            }
            else if (st.lives <= 0) EndRun("seals");
            else if (st.stock <= 0) EndRun("stock");
            else if (revealCorrect)
            {
                orderIndex++;
                if (orderIndex >= GopbaegiRules.TargetOrders)
                {
                    bool mastery = st.firstAttemptCorrect >= 7 && st.finalBandFirstCorrect >= 2;
                    EndRun(mastery ? "clear" : "mastery");
                }
                else { mode = Mode.Playing; current = deck[orderIndex]; BeginOrder(); }
            }
            else
            {
                mode = Mode.Playing; st.misconceptionId = ""; st.targetHidden = 1;
                if (!revealUnderfill) st.poured = 0;
                st.holding = false; st.adjusting = false; RefreshOrderUi(); UpdateFillVisual(st.poured, current.capacity, false);
            }
            SetScreen(); MgfBridge.NotifyChanged();
        }

        void EndRun(string reason)
        {
            if (mode == Mode.End) return;
            endReason = reason; st.endReason = reason; st.holding = false; st.adjusting = false; st.onboarding = false;
            mode = Mode.End; endClock = 0f; ShowEndVisual(reason); SetScreen(); MgfBridge.NotifyChanged();
            MgfSfx.Play(reason == "clear" ? "win" : "lose", .52f);
        }

        bool IsLeverZone(Vector2 p)
        {
            float x = p.x / Mathf.Max(1, Screen.width), y = p.y / Mathf.Max(1, Screen.height);
            return (x > .52f && y < .43f) || Vector2.Distance(p, LeverScreenPoint()) < Mathf.Max(86f, Screen.width * .15f);
        }

        bool IsPairZone(Vector2 p)
        {
            float x = p.x / Mathf.Max(1, Screen.width), y = p.y / Mathf.Max(1, Screen.height);
            return PairAvailable() && x > .50f && y > .67f;
        }

        static bool IsUiZone(RectTransform rt, Vector2 p)
        {
            return rt != null && rt.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(rt, p, null);
        }

        void HandleInput()
        {
            bool down = MgfPointer.Down || Input.GetKeyDown(KeyCode.Space);
            bool up = MgfPointer.Up || Input.GetKeyUp(KeyCode.Space);
            if (Input.GetKeyDown(KeyCode.Space)) keyHold = true;
            if (Input.GetKeyUp(KeyCode.Space)) keyHold = false;

            if (mode == Mode.Title)
            {
                if (down)
                {
                    st.pointerVersion++;
                    // 타이틀은 세계 어디를 눌러도 연습으로 연다. 실제 답 입력은 연습의 레버 hold/release만 허용한다.
                    StartPractice();
                }
                return;
            }
            if (mode == Mode.End)
            {
                if (down && endClock > .65f) { st.pointerVersion++; StartRunDirect(); MgfSfx.Play("whoosh", .28f); }
                return;
            }
            if (mode == Mode.Reveal)
            {
                if (down) { st.pointerVersion++; InvalidTap(MgfPointer.Position, "부피의 비를 확인하고 있습니다"); MgfBridge.NotifyChanged(); }
                return;
            }
            if (mode != Mode.Practice && mode != Mode.Playing) return;

            if (st.adjusting)
            {
                if (Input.GetKeyDown(KeyCode.LeftArrow)) { st.pointerVersion++; NudgeAmount(-1); }
                if (Input.GetKeyDown(KeyCode.RightArrow)) { st.pointerVersion++; NudgeAmount(1); }
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) { st.pointerVersion++; SealAmount(); }
            }

            if (down)
            {
                st.pointerVersion++; idleGuide = 0f;
                if (st.adjusting && IsUiZone(fineMinusRt, MgfPointer.Position)) NudgeAmount(-1);
                else if (st.adjusting && IsUiZone(finePlusRt, MgfPointer.Position)) NudgeAmount(1);
                else if (st.adjusting && IsUiZone(fineSealRt, MgfPointer.Position)) SealAmount();
                else if (IsPairZone(MgfPointer.Position)) SwapPair();
                else if (IsLeverZone(MgfPointer.Position) || keyHold) BeginHold();
                else { InvalidTap(MgfPointer.Position, "압력 레버를 누른 채 눈금을 올리세요"); MgfSfx.Play("wrong", .10f); MgfBridge.NotifyChanged(); }
            }
            if (up) ReleaseHold();
        }

        public void TestStart() => StartRunDirect();

        public void TestAnswerCorrect()
        {
            if (mode == Mode.Title || mode == Mode.End) StartRunDirect();
            if (mode == Mode.Reveal) FinishReveal();
            if (mode == Mode.Practice || mode == Mode.Playing)
            {
                int add = Math.Max(0, current.target - st.poured);
                st.poured = current.target;
                if (mode == Mode.Playing) st.stock = Math.Max(0, st.stock - add);
                st.adjusting = false; UpdateFillVisual(st.poured, current.capacity, false); JudgeRelease();
            }
        }

        public void TestAnswerWrong()
        {
            if (mode == Mode.Title || mode == Mode.End) StartRunDirect();
            if (mode == Mode.Reveal) FinishReveal();
            if (mode == Mode.Practice || mode == Mode.Playing)
            {
                int wrong = Math.Max(4, current.linear);
                int add = Math.Max(0, wrong - st.poured); st.poured = wrong;
                if (mode == Mode.Playing) st.stock = Math.Max(0, st.stock - add);
                st.adjusting = false; UpdateFillVisual(st.poured, current.capacity, false); JudgeRelease();
            }
        }

        public string StateJson()
        {
            if (mode == Mode.Title) st.phase = "title";
            else if (mode == Mode.End || st.lives <= 0 || st.stock <= 0) st.phase = endReason == "clear" ? "clear" : "gameover";
            else st.phase = "playing";
            st.actionPhase = st.adjusting ? "fine" : mode.ToString().ToLowerInvariant();
            return JsonUtility.ToJson(st);
        }

        public string ProblemBankJson() => MgfJson.Bank(bank);
    }
}
