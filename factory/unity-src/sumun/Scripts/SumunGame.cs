// 수문 — 삼각 여수로의 평행 철 수문을 칸에 세운 뒤 가로로 쓸어 잠그는 90초 아케이드.
// 답 입력 경로: (1) 수문을 위아래로 끌어 1 cm 칸에 세운다 (2) 수문을 따라 가로로 쓸어 잠근다.
// 판정은 Judge.Ok(정수 칸). 훅도 같은 Commit() 을 탄다. 하단 n지선다 없음.
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;

namespace Mgf.Sumun
{
    public partial class SumunGame : MonoBehaviour, IMgfGame
    {
        [System.Serializable]
        class State : MgfState
        {
            public int combo, maxCombo, firstTry, attempts, taps, ad, of = 10;
            public string kind = "";
            public string misconception = "";
            public bool onboarding;
        }

        readonly State st = new State();
        enum Ph { Title, Practice, Play, End }
        Ph ph = Ph.Title;

        Sheet cur;
        List<Sheet> deck;
        int deckIdx = -1;
        System.Random rng = new System.Random(20261001);
        readonly List<MgfProblem> bank = new List<MgfProblem>();

        bool frozen, tried;
        float runLeft, sheetLeft, hitStop, idleT, ghostT, revealT, dumpT, slideT, refuseT, toastT, toastDur;
        float shownScore, titleT, endT, jiggleT, pulseTm, foamT, lockFlash, shimmerT;
        int shownScoreInt = -1, lastTimeInt = -1, lastAd = -999;
        Vector2 refuseAt;
        int best, bestScore;
        string endReason = "";
        float ghostScale = 1f;
        int obStep;
        float camSettle;
        bool land;

        enum Press { None, Gate, Play, Cta, EndCta, Other }
        Press press;
        Vector2 downScreen, lastScreen;
        float downTime, horizAcc, vertAcc, tickCarry, adVis;
        bool dragMoved, lockedThis, committedDrag, hintedMatch;
        float moveBudget; // 초당 8칸 상한
        float dumpDur = 1.15f;

        // 잠금은 포인터를 뗀 뒤의 가로 획만. Held 중 42px 누적은 세로 드래그를 오잠금으로 만든다.
        float LockPx() => Mathf.Max(100f, Screen.width * 0.25f);
        bool IsLockStroke() => Mathf.Abs(horizAcc) >= LockPx() && Mathf.Abs(horizAcc) > Mathf.Abs(vertAcc) * 1.5f;

        static readonly System.Type[] keepTypes = { typeof(BoxCollider), typeof(SphereCollider), typeof(MeshCollider), typeof(CapsuleCollider) };

        void Awake()
        {
            SumunSound.Init(gameObject);
            BuildBank();
            BuildWorld();
            BuildUi();
            Prewarm();
            _ = keepTypes[0];
            best = PlayerPrefs.GetInt("sumun.best", 0);
            bestScore = PlayerPrefs.GetInt("sumun.bestScore", 0);
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb = new System.Text.StringBuilder(
                "수문수문을밀어잠가라수문을비에맞춰잠가라추를중선비에맞춰잠가라중학교2학년평행선과선분의길이의비최고점수잠금핀여수로폐쇄시간종료소진다시잠그기한판90초본판10회연습중점무게중심중선넓이닮음평행칸cm²△ABCDEGMN spl잠금연속콤보첫시도정답현재비꼭짓점으로부터잘못걸린옮겨밑변의이등분세평행선구하시오걸어라오늘의여수로");
            foreach (var p in bank) { sb.Append(p.prompt); sb.Append(p.answer); }
            var g = new SheetGen(new System.Random(3));
            for (int i = 0; i < 8; i++) foreach (var s in g.Deck()) { sb.Append(Words.Prompt(s)); sb.Append(Words.RevealRight(s)); sb.Append(Words.RevealWrong(s)); }
            MgfText.Prewarm(sb.ToString());
        }

        void BuildBank()
        {
            var seen = new HashSet<string>();
            Add(SheetGen.Practice(), seen);
            var g = new SheetGen(new System.Random(777));
            int guard = 0;
            while (bank.Count < 360 && guard++ < 8000)
                Add(g.Any(), seen);
        }

        void Add(Sheet s, HashSet<string> seen)
        {
            string p = Words.Prompt(s);
            if (!seen.Add(p + "|" + Words.Answer(s))) return;
            bank.Add(new MgfProblem
            {
                id = "sm" + (bank.Count + 1),
                prompt = p,
                choices = null,
                answer = Words.Answer(s),
                answerNumeric = s.asked,
                unitConcept = Words.Concept(s)
            });
        }

        public void TestStart() { StartRun(false); }

        public void TestAnswerCorrect()
        {
            if (ph == Ph.Title || ph == Ph.End) StartRun(false);
            if (ph == Ph.Practice) EndPractice();
            FinishPending();
            if (ph != Ph.Play || cur == null) return;
            cur.ad = cur.target; adVis = cur.ad;
            Commit(true);
        }

        public void TestAnswerWrong()
        {
            if (ph == Ph.Title || ph == Ph.End) StartRun(false);
            if (ph == Ph.Practice) EndPractice();
            FinishPending();
            if (ph != Ph.Play || cur == null) return;
            int w = cur.target == cur.adLo ? cur.adHi : cur.adLo;
            if (w == cur.target) w = Mathf.Clamp(cur.target - 1, cur.adLo, cur.adHi);
            cur.ad = w; adVis = cur.ad;
            Commit(false);
        }

        public string StateJson()
        {
            st.phase = ph == Ph.Title ? "title" : ph == Ph.End ? (endReason == "clear" ? "clear" : "gameover") : "playing";
            st.onboarding = ph == Ph.Practice;
            if (cur != null && ph != Ph.Title)
            {
                st.ad = cur.ad;
                st.kind = cur.kind.ToString();
                st.misconception = cur.misconceptionId;
                st.level = Mathf.Max(1, cur.stage);
            }
            return JsonUtility.ToJson(st);
        }

        public string ProblemBankJson() => MgfJson.Bank(bank);

        void ShowTitle()
        {
            ph = Ph.Title;
            titleT = 0; endReason = "";
            cur = SheetGen.Practice();
            cur.ab = 9; cur.ac = 9; cur.bc = 8; cur.target = 6; cur.m = 2; cur.n = 1; cur.ad = 1; cur.ad0 = 1; cur.adHi = 8; cur.de = Judge.De(cur, 6);
            if (cur.de < 0) cur.de = 6;
            SetupSheet(cur, true);
            bestTxt.text = bestScore > 0 ? "최고 점수 " + bestScore : "한 판 90초 · 본판 10회";
            SetVisible();
            MgfBridge.NotifyChanged();
        }

        void StartRun(bool withPractice)
        {
            CancelInvoke();
            st.score = 0; st.solved = 0; st.combo = 0; st.maxCombo = 0; st.firstTry = 0; st.attempts = 0;
            st.lives = Rules.Pins; st.level = 1;
            shownScore = 0; shownScoreInt = -1; lastTimeInt = -1;
            runLeft = Rules.RunSec; frozen = false; endReason = ""; dumpT = 0; slideT = 0; revealT = 0;
            var day = System.DateTime.Now;
            rng = new System.Random(day.Year * 10000 + day.Month * 100 + day.Day);
            deck = new SheetGen(rng).Deck();
            deckIdx = -1;
            HideToast();
            RestorePins();
            if (withPractice)
            {
                ph = Ph.Practice; obStep = 0; idleT = 0; ghostScale = 1f; ghostT = 0; frozen = true;
                cur = SheetGen.Practice();
                SetupSheet(cur, false);
            }
            else
            {
                ph = Ph.Play; frozen = false;
                NextSheet();
            }
            SetVisible();
            MgfBridge.NotifyChanged();
        }

        void EndPractice()
        {
            frozen = false;
            ph = Ph.Play;
            st.score = 0; st.solved = 0; st.combo = 0;
            NextSheet();
            SetVisible();
        }

        void NextSheet()
        {
            FinishPending();
            lockedThis = false; tried = false; hintedMatch = false; sheetLeft = Rules.SheetSec; idleT = 0; revealT = 0; dumpT = 0; slideT = 0;
            int broken = Rules.Pins - st.lives;
            deckIdx++;
            Sheet s = null;
            int stage = st.solved < 3 ? 1 : st.solved < 7 ? 2 : 3;
            st.level = stage;
            if (deck != null && deckIdx < deck.Count)
            {
                s = deck[deckIdx].Clone();
                s.no = st.solved + 1;
                if (!new SheetGen(rng).FitBroken(s, broken)) s = null;
            }
            if (s == null) s = new SheetGen(rng).NextFitted(st.solved + 1, stage, broken);
            cur = s;
            cur.Reset();
            adVis = cur.ad;
            lastAd = cur.ad;
            SetupSheet(cur, false);
            RefreshHud();
            MgfBridge.NotifyChanged();
        }

        void FinishPending()
        {
            if (revealT > 0) revealT = 0;
            if (dumpT > 0) dumpT = 0;
            HideGhost();
        }

        void Commit(bool ok)
        {
            if (lockedThis && ph == Ph.Play) return;
            CancelInvoke();
            lockedThis = true;
            st.attempts++;
            st.taps++;
            if (!tried) { tried = true; if (ok) st.firstTry++; }

            if (ph == Ph.Practice)
            {
                if (ok)
                {
                    hitStop = 0.08f;
                    dumpDur = 2.25f; dumpT = 2.25f; revealT = 2.25f;
                    SumunSound.Play("clang"); SumunSound.Play("pin", 0.9f); SumunSound.Play("water", 0.8f); SumunSound.Play("good", 0.7f);
                    PunchGate();
                    goalTxt.text = Words.RevealRight(cur);
                    Invoke(nameof(EndPractice), 2.25f);
                }
                else
                {
                    // 연습 오잠금은 스폰으로 되돌리지 않는다(AD=1 동일 화면 함정).
                    revealT = 0.9f; jiggleT = 0.35f;
                    SumunSound.Play("leak"); SumunSound.Play("refuse");
                    ShowGhost(cur.target);
                    toast("점선이 정답 칸이다. 칸을 옮긴 뒤 가로로 쓸어라", 1.15f);
                    lockedThis = false;
                    RefreshGate(true);
                }
                MgfBridge.NotifyChanged();
                return;
            }

            if (ph != Ph.Play) return;

            if (ok)
            {
                st.combo++; if (st.combo > st.maxCombo) st.maxCombo = st.combo;
                int add = 100 * Rules.Mult(st.combo);
                if (cur.kind == Kind.Mid) add += 50;
                if (cur.kind == Kind.Cent || cur.kind == Kind.Area) add += 50;
                st.score += add;
                st.solved++;
                hitStop = 0.09f; dumpDur = 1.4f; dumpT = 1.4f; lockFlash = 0.35f;
                SumunSound.Play("clang"); SumunSound.Play("pin"); SumunSound.Play("water", 0.85f);
                if (st.combo >= 2) SumunSound.Play("good", 0.55f + 0.08f * Mathf.Min(3, st.combo));
                PunchGate();
                goalTxt.text = Words.RevealRight(cur);
                if (st.solved >= Rules.Locks) Invoke(nameof(ClearRun), 1.4f);
                else Invoke(nameof(NextSheet), 1.4f);
            }
            else
            {
                st.combo = 0;
                st.lives--;
                revealT = 0.85f; slideT = 0.55f; jiggleT = 0.2f;
                SumunSound.Play("leak"); SumunSound.Play("drop");
                ShowGhost(cur.target);
                toast(Words.RevealWrong(cur), 1.1f);
                BreakPin();
                if (st.lives <= 0) Invoke(nameof(FailPins), 0.9f);
                else Invoke(nameof(NextSheet), 0.95f);
            }
            RefreshHud();
            MgfBridge.NotifyChanged();
        }

        void ClearRun() { EndRun("clear"); }
        void FailPins() { EndRun("pins"); }
        void FailTime() { EndRun("time"); }

        void EndRun(string why)
        {
            CancelInvoke();
            FinishPending();
            ph = Ph.End; endReason = why; endT = 0;
            if (st.solved > best) { best = st.solved; PlayerPrefs.SetInt("sumun.best", best); }
            if (st.score > bestScore) { bestScore = st.score; PlayerPrefs.SetInt("sumun.bestScore", bestScore); PlayerPrefs.Save(); shimmerT = 0.8f; }
            if (why == "clear") SumunSound.Play("win"); else SumunSound.Play("end");
            SetVisible();
            MgfBridge.NotifyChanged();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (hitStop > 0) { hitStop -= dt; dt = 0; }
            bool nowLand = (float)Screen.width / Mathf.Max(1, Screen.height) >= 1.2f;
            if (nowLand != land && cur != null)
            {
                land = nowLand;
                LayoutTri(cur);
                DrawStatic(cur);
                RefreshGate(true);
                LayoutHud();
                FrameCam();
            }
            else land = nowLand;
            Animate(dt);
            HandleInput(dt);
            if (ph == Ph.Play && !frozen && dt > 0)
            {
                runLeft -= dt; sheetLeft -= dt;
                if (runLeft <= 0) FailTime();
                else if (sheetLeft <= 0 && !lockedThis) Commit(false);
            }
            if (ph == Ph.Practice)
            {
                idleT += dt;
                if (idleT > 8f) { ghostScale = 1.45f; idleT = 0; }
            }
        }

        void HandleInput(float dt)
        {
            moveBudget = Rules.TickPerSec * Mathf.Max(dt, 0.0001f);

            if (Input.GetKeyDown(KeyCode.Escape) && (ph == Ph.Play || ph == Ph.Practice) && !lockedThis)
            {
                cur.ad = cur.ad0; adVis = cur.ad; RefreshGate(true); SumunSound.Play("refuse", 0.5f);
            }
            if ((ph == Ph.Play || ph == Ph.Practice) && !lockedThis)
            {
                int d = 0;
                if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) d = -1;
                if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) d = 1;
                if (d != 0) Nudge(d);
            }

            if (MgfPointer.Down)
            {
                st.taps++;
                downScreen = lastScreen = MgfPointer.Position;
                downTime = Time.unscaledTime;
                horizAcc = 0; vertAcc = 0; dragMoved = false; committedDrag = false;
                press = HitPress();
                if (ph == Ph.End) { press = Press.EndCta; }
                else if (ph == Ph.Title && press == Press.Cta) { /* wait drag */ }
                else if (press == Press.Other || press == Press.Play || press == Press.Gate)
                {
                    if (press == Press.Other) Refuse(MgfPointer.Position);
                    else pulseTm = 0.2f;
                }
                MgfBridge.NotifyChanged();
            }

            if (MgfPointer.Held && press != Press.None)
            {
                var p = MgfPointer.Position;
                var dlt = p - lastScreen;
                lastScreen = p;
                horizAcc += dlt.x; vertAcc += dlt.y;
                if ((p - downScreen).sqrMagnitude > 100f) dragMoved = true;

                if (ph == Ph.Title)
                {
                    DragAd(dlt, dt);
                    if (cur != null && cur.ad >= 3 && vertAcc < -40f) { StartRun(true); press = Press.None; return; }
                }
                else if ((ph == Ph.Play || ph == Ph.Practice) && !lockedThis)
                {
                    DragAd(dlt, dt);
                    // Held 중에는 칸만 옮긴다. 가로는 포인터를 뗀 획에서만 잠근다.
                }
            }

            if (MgfPointer.Up && press != Press.None)
            {
                if (ph == Ph.End) { StartRun(true); press = Press.None; return; }
                if (ph == Ph.Title)
                {
                    StartRun(true);
                    press = Press.None; return;
                }
                if ((ph == Ph.Play || ph == Ph.Practice) && !lockedThis)
                {
                    if (!dragMoved)
                    {
                        Refuse(downScreen);
                        SpawnGhostFinger();
                        toast(cur != null && cur.isPlumb ? "추를 위아래로 끌어 칸에 세운 뒤, 가로로 쓸어 잠가라" : "수문을 위아래로 끌어 칸에 세운 뒤, 가로로 쓸어 잠가라", 1.2f);
                    }
                    else if (IsLockStroke())
                    {
                        if (Judge.Ok(cur))
                        {
                            committedDrag = true;
                            Commit(true);
                        }
                        else
                        {
                            // 비가 다를 때 가로 획은 핀을 깎지 않고 토스트만(억울한 오잠금 방지).
                            if (!tried) tried = true;
                            st.attempts++;
                            SpinPinsHint();
                            JiggleGate();
                            SumunSound.Play("refuse", 0.55f);
                            toast("이 칸의 비는 목표와 다르다", 1.15f);
                            if (ph == Ph.Practice) { ShowGhost(cur.target); revealT = 0.9f; }
                            MgfBridge.NotifyChanged();
                        }
                    }
                    else if (Judge.Ok(cur))
                    {
                        SpinPinsHint();
                        if (!hintedMatch)
                        {
                            SumunSound.Play("refuse", 0.4f);
                            toast("맞으면 수문을 따라 가로로 쓸어 잠가라", 1.15f);
                        }
                    }
                }
                press = Press.None;
            }
        }

        Press HitPress()
        {
            if (!cam) return Press.Other;
            if (MgfPointer.DownHit(cam, out var hit))
            {
                var n = hit.collider ? hit.collider.name : "";
                if (n.StartsWith("Sluice") || n.StartsWith("Weight") || n.StartsWith("Gate") || n.StartsWith("Plumb")) return Press.Gate;
                if (n.StartsWith("Play") || n.StartsWith("Bank") || n.StartsWith("Water")) return Press.Play;
                if (n.StartsWith("Cta")) return Press.Cta;
            }
            // 화면 중·하단은 수문 조작으로 친다(실입력 격자 30/50/70 × 32/52/72).
            float ny = MgfPointer.Position.y / Mathf.Max(1, Screen.height);
            if (ny > 0.18f && ny < 0.88f) return Press.Play;
            return Press.Other;
        }

        void DragAd(Vector2 dlt, float dt)
        {
            if (cur == null) return;
            // 화면 세로 드래그 → 칸. 아래(음수 y in screen? Input.mousePosition y-up) 아래가 AD 증가.
            float spanPx = Screen.height * 0.42f;
            float ticks = cur.adHi - cur.adLo;
            if (ticks < 1) ticks = 1;
            float wantDelta = -dlt.y / spanPx * ticks; // 아래로 끌면 AD 증가
            tickCarry += wantDelta;
            float step = Mathf.Clamp(tickCarry, -moveBudget, moveBudget);
            tickCarry -= step;
            adVis = Mathf.Clamp(adVis + step, cur.adLo, cur.adHi);
            int nad = Mathf.Clamp(Mathf.RoundToInt(adVis), cur.adLo, cur.adHi);
            if (nad != cur.ad)
            {
                cur.ad = nad;
                SumunSound.Play("tick", 0.55f, 0.92f + 0.04f * (nad % 3));
                RefreshGate(false);
                NoteMatchHint();
                MgfBridge.NotifyChanged();
            }
            else RefreshGate(false);
        }

        void Nudge(int d)
        {
            if (cur == null || lockedThis) return;
            int nad = Mathf.Clamp(cur.ad + d, cur.adLo, cur.adHi);
            if (nad == cur.ad) { JiggleGate(); SumunSound.Play("refuse", 0.3f); return; }
            cur.ad = nad; adVis = nad;
            SumunSound.Play("tick");
            RefreshGate(true);
            NoteMatchHint();
            MgfBridge.NotifyChanged();
        }

        void Refuse(Vector2 screen)
        {
            refuseT = 0.32f;
            refuseAt = screen;
            JiggleGate();
            SumunSound.Play("ripple");
            PointAtGate();
        }

        void NoteMatchHint()
        {
            if (hintedMatch || lockedThis || cur == null) return;
            if (cur.ad != cur.target) return;
            hintedMatch = true;
            SpinPinsHint();
            pulseTm = 0.7f;
            toast("맞으면 수문을 따라 가로로 쓸어 잠가라", 1.2f);
        }

        void toast(string s, float dur) { toastTxt.text = s; toastT = 0; toastDur = dur; toastG.alpha = 1; }
        void HideToast() { toastT = 9; if (toastG) toastG.alpha = 0; }
    }
}
