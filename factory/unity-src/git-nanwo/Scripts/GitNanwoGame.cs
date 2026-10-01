// 깃 나눠 — 왕복하는 철 밀대를 탭으로 떨어뜨려 평행선이 나누는 선분의 비를 맞추는 90초 재단 아케이드.
//
// 답 입력 경로: 플레이필드 탭 또는 Space = 지금 밀대가 있는 1 cm 칸에 떨어뜨려 즉시 판정.
// 판정은 Judge.Ok(정수 칸). 훅도 같은 Commit() 을 탄다. 하단 n지선다 없음. 코드는 정답 칸으로 끌어 주지 않는다.
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;

namespace Mgf.GitNanwo
{
    public partial class GitNanwoGame : MonoBehaviour, IMgfGame
    {
        [System.Serializable]
        class State : MgfState
        {
            public int combo, maxCombo, firstTry, attempts, taps, of = 10;
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

        bool frozen, tried, lockedThis;
        float runLeft, sheetLeft, hitStop, idleT, ghostT, revealT, refuseT, toastT, toastDur;
        float shownScore, titleT, endT, jiggleT, pulseTm, dropSquash, splitT, slideT, swingAmp, shimmerT;
        float shownCm, cmVel, ghostScale = 1f;
        int shownScoreInt = -1, lastTimeInt = -1, lastSnap = -1;
        int best, bestScore, bestCombo;
        string endReason = "";
        int obStep;
        bool land;
        int playSheetN;
        bool sheetTimed;
        float tickVis = 1f;
        int oscDir = 1;
        bool titleDrop;
        float titleDropT;
        Vector2 refuseAt;
        int cmGoal;
        bool afterWrong, lastOk;

        enum Press { None, Play, Cta, EndCta, Skip, Flag, Other }
        Press press;
        Vector2 downScreen;
        float downTime;
        bool dragMoved;

        void Awake()
        {
            GitSound.Init(gameObject);
            BuildBank();
            BuildWorld();
            BuildUi();
            Prewarm();
            best = PlayerPrefs.GetInt("gitnanwo.best", 0);
            bestScore = PlayerPrefs.GetInt("gitnanwo.bestScore", 0);
            bestCombo = PlayerPrefs.GetInt("gitnanwo.bestCombo", 0);
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb = new System.Text.StringBuilder(
                "깃나눠밀대를눌러길이의비를맞춰라시작하기중학교2학년평행선과선분의길이의비최고컷콤보예비깃정확재단다시하기한판90초본판10회연습중점무게중심중선넓이닮음평행칸cm²△ABCDEGMN구하시오눌러지금은자르는중주문서중점연결정리꼭짓점으로부터밑변의이등분세평행선연속첫시도정답현재비칸탭밀대부터까지의길이를같은비의칸을이으면그줄이밑변과평행하다잘못걸린옮겨한낮재단테이블삼각기쇠꽂이완성더미시간종료소진맞았다멈춰라우령서는칸에서눌러라△GBC");
            foreach (var p in bank) { sb.Append(p.prompt); sb.Append(p.answer); }
            var g = new SheetGen(new System.Random(3));
            for (int i = 0; i < 6; i++) foreach (var s in g.Deck()) { sb.Append(Words.Prompt(s)); sb.Append(Words.RevealRight(s)); sb.Append(Words.RevealWrong(s)); }
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
            if (s == null) return;
            string p = Words.Prompt(s);
            string a = Words.Answer(s);
            if (!seen.Add(p)) return;
            double num = s.asked;
            bank.Add(new MgfProblem
            {
                id = "gn" + (bank.Count + 1),
                prompt = p,
                choices = null,
                answer = a,
                answerNumeric = num,
                unitConcept = Words.Concept(s)
            });
        }

        public void TestStart() { StartRun(false); }

        public void TestAnswerCorrect()
        {
            if (ph == Ph.Title || ph == Ph.End) StartRun(false);
            if (ph == Ph.Practice) EndPractice();
            FinishPending();
            CancelInvoke();
            lockedThis = false;
            if (ph != Ph.Play || cur == null) return;
            tickVis = cur.target;
            cur.ad = cur.target;
            Commit(true);
        }

        public void TestAnswerWrong()
        {
            if (ph == Ph.Title || ph == Ph.End) StartRun(false);
            if (ph == Ph.Practice) EndPractice();
            FinishPending();
            CancelInvoke();
            lockedThis = false;
            if (ph != Ph.Play || cur == null) return;
            int w = cur.misTick > 0 && cur.misTick != cur.target ? cur.misTick : (cur.target == cur.adLo ? cur.adHi : cur.adLo);
            if (w == cur.target) w = Mathf.Clamp(cur.target - 1, cur.adLo, cur.adHi);
            tickVis = w;
            cur.ad = w;
            Commit(false);
        }

        public string StateJson()
        {
            st.phase = ph == Ph.Title ? "title" : ph == Ph.End ? (endReason == "clear" ? "clear" : "gameover") : "playing";
            st.onboarding = ph == Ph.Practice;
            st.lives = Mathf.Max(0, st.lives);
            if (cur != null && ph != Ph.Title)
            {
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
            titleT = 0; endReason = ""; titleDrop = false; titleDropT = 0;
            cur = SheetGen.Practice();
            cur.ab = 12; cur.ac = 12; cur.bc = 10; cur.target = 8; cur.m = 2; cur.n = 1;
            cur.ad = 6; cur.ad0 = 6; cur.adLo = 4; cur.adHi = 9; cur.de = Judge.De(cur, 8);
            if (cur.de < 0) cur.de = 6;
            tickVis = 6f; oscDir = 1;
            SetupSheet(cur, true);
            bestTxt.text = bestScore > 0 ? "최고 점수 " + bestScore + "  ·  컷 " + best + "  ·  콤보 " + bestCombo : "한 판 90초  ·  정확 컷 10회";
            SetVisible();
            MgfBridge.NotifyChanged();
        }

        void StartRun(bool withPractice)
        {
            CancelInvoke();
            FinishPending();
            lockedThis = false;
            tried = false;
            hitStop = 0; revealT = 0; refuseT = 0; dropSquash = 0; splitT = 0; slideT = 0; jiggleT = 0;
            st.score = 0; st.solved = 0; st.combo = 0; st.maxCombo = 0; st.firstTry = 0; st.attempts = 0;
            st.lives = Rules.Flags; st.level = 1; st.of = Rules.Cuts; st.taps = 0;
            shownScore = 0; shownScoreInt = -1; lastTimeInt = -1;
            runLeft = Rules.RunSec; frozen = false; endReason = "";
            afterWrong = false;
            rng = new System.Random(System.DateTime.Now.Millisecond + 17);
            deck = new SheetGen(rng).Deck();
            deckIdx = -1;
            playSheetN = 0;
            HideToast();
            HideGhost();
            ResetFlags();
            if (withPractice)
            {
                ph = Ph.Practice; obStep = 0; idleT = 0; ghostScale = 1f; ghostT = 0; frozen = false;
                cur = SheetGen.Practice();
                tickVis = cur.ad0; oscDir = 1;
                lastSnap = -1;
                SetupSheet(cur, false);
                ShowGhost(cur.target);
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
            lockedThis = false;
            ph = Ph.Play;
            st.score = 0; st.solved = 0; st.combo = 0; st.lives = Rules.Flags;
            ghostScale = 1f;
            if (skipG) skipG.alpha = 0;
            NextSheet();
            SetVisible();
        }

        void NextSheet()
        {
            FinishPending();
            lockedThis = false; tried = false; idleT = 0; revealT = 0; splitT = 0; slideT = 0; dropSquash = 0;
            playSheetN++;
            sheetTimed = playSheetN > Rules.UntimedSheets;
            sheetLeft = Rules.SheetSec;
            int stage = st.solved < 4 ? 1 : st.solved < 7 ? 2 : 3;
            if (st.combo >= 3) stage = Mathf.Min(3, stage + 1);
            if (runLeft <= 60f && stage < 2) stage = 2;
            if (runLeft <= 30f) stage = 3;
            st.level = stage;
            deckIdx++;
            Sheet s = null;
            if (afterWrong && stage >= 2)
            {
                s = new SheetGen(rng).NextFor(st.solved + 1, 2);
                if (s.kind != Kind.Three) s = new SheetGen(rng).NextFor(st.solved + 1, 2);
                afterWrong = false;
            }
            else if (deck != null && deckIdx < deck.Count)
            {
                s = deck[deckIdx].Clone();
                s.no = st.solved + 1;
                s.stage = stage;
                if (stage >= 2) s.hideRatio = true;
            }
            if (s == null) s = new SheetGen(rng).NextFor(st.solved + 1, stage);
            cur = s;
            cur.Reset();
            tickVis = cur.ad0;
            oscDir = 1;
            lastSnap = -1;
            shownCm = 0; cmVel = 0; cmGoal = 0;
            SetupSheet(cur, false);
            RefreshHud();
            MgfBridge.NotifyChanged();
        }

        void FinishPending()
        {
            if (revealT > 0) revealT = 0;
            HideGhost();
        }

        int SnapTick()
        {
            if (cur == null) return 1;
            int s = Mathf.RoundToInt(tickVis);
            return Mathf.Clamp(s, cur.adLo, cur.adHi);
        }

        void Drop()
        {
            if (cur == null || lockedThis) return;
            if (ph != Ph.Play && ph != Ph.Practice) return;
            int snap = SnapTick();
            if (ph == Ph.Practice && snap <= cur.adLo)
            {
                RefuseDrop("꼭짓점 칸이 아니다");
                return;
            }
            cur.ad = snap;
            dropSquash = 1f;
            GitSound.Play("drop", 0.9f);
            Commit(cur.ad == cur.target);
        }

        void RefuseDrop(string why)
        {
            jiggleT = 0.22f;
            GitSound.Play("refuse");
            toast(why, 0.9f);
            MgfBridge.NotifyChanged();
        }

        void Commit(bool ok)
        {
            if (lockedThis && ph == Ph.Play) return;
            CancelInvoke();
            lockedThis = true;
            lastOk = ok;
            st.attempts++;
            st.taps++;
            if (!tried) { tried = true; if (ok) st.firstTry++; }

            if (ph == Ph.Practice)
            {
                if (ok)
                {
                    hitStop = 0.12f;
                    revealT = 1.5f; splitT = 1.5f; slideT = 1.4f; dropSquash = 1.1f;
                    GitSound.Play("tear"); GitSound.Play("slide", 0.8f);
                    goalTxt.text = Words.RevealRight(cur);
                    cmGoal = Words.CounterGoal(cur);
                    Invoke(nameof(EndPractice), 1.55f);
                }
                else
                {
                    revealT = 0.85f; jiggleT = 0.35f;
                    GitSound.Play("rattle"); GitSound.Play("refuse");
                    toast("유령 밀대가 서는 칸에서 눌러라", 1.2f);
                    lockedThis = false;
                    cur.ad = cur.ad0;
                    tickVis = cur.ad0;
                    ghostScale = 1.45f;
                    ShowGhost(cur.target);
                }
                RefreshHud();
                MgfBridge.NotifyChanged();
                return;
            }

            if (ph != Ph.Play) return;

            if (ok)
            {
                st.combo++; if (st.combo > st.maxCombo) st.maxCombo = st.combo;
                int add = 100 * Rules.Mult(st.combo);
                if (cur.kind == Kind.Mid) add += 50;
                if (cur.kind == Kind.Cent || cur.kind == Kind.Area) add += 80;
                st.score += add;
                st.solved++;
                hitStop = 0.12f; revealT = 1.15f; splitT = 1.15f; slideT = 1.1f; dropSquash = 1.15f;
                GitSound.Play("tear"); GitSound.Play("slide");
                if (st.combo >= 2) GitSound.Play("combo", 0.55f + 0.08f * Mathf.Min(3, st.combo));
                goalTxt.text = Words.RevealRight(cur);
                cmGoal = Words.CounterGoal(cur);
                if (st.solved >= Rules.Cuts) Invoke(nameof(ClearRun), 1.2f);
                else Invoke(nameof(NextSheet), 1.2f);
            }
            else
            {
                st.combo = 0;
                st.lives--;
                afterWrong = true;
                revealT = 0.85f; jiggleT = 0.28f; splitT = 0.7f;
                GitSound.Play("rattle"); GitSound.Play("impale");
                ImpaleFlag();
                ShowGhost(cur.target);
                toast(Words.RevealWrong(cur), 1.15f);
                if (st.lives <= 0) EndRun("flags");
                else Invoke(nameof(NextSheet), 0.95f);
            }
            RefreshHud();
            MgfBridge.NotifyChanged();
        }

        void ClearRun() { EndRun("clear"); }
        void FailFlags() { EndRun("flags"); }
        void FailTime() { EndRun("time"); }

        void EndRun(string why)
        {
            CancelInvoke();
            FinishPending();
            lockedThis = false;
            frozen = true;
            ph = Ph.End; endReason = why; endT = 0;
            if (st.solved > best) { best = st.solved; PlayerPrefs.SetInt("gitnanwo.best", best); }
            if (st.maxCombo > bestCombo) { bestCombo = st.maxCombo; PlayerPrefs.SetInt("gitnanwo.bestCombo", bestCombo); }
            if (st.score > bestScore) { bestScore = st.score; PlayerPrefs.SetInt("gitnanwo.bestScore", bestScore); PlayerPrefs.Save(); }
            GitSound.Play(why == "clear" ? "win" : "lose");
            SetVisible();
            MgfBridge.NotifyChanged();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (hitStop > 0) { hitStop -= dt; dt = 0; }
            bool nowLand = Screen.width >= 1024 && (float)Screen.width / Mathf.Max(1, Screen.height) >= 1.2f;
            if (nowLand != land)
            {
                land = nowLand;
                LayoutHud();
                FrameCam();
            }
            Oscillate(dt);
            Animate(dt);
            HandleInput(dt);
            if (ph == Ph.Play && !frozen && dt > 0 && !lockedThis)
            {
                runLeft -= dt;
                if (sheetTimed) sheetLeft -= dt;
                if (runLeft <= 0) FailTime();
                else if (sheetTimed && sheetLeft <= 0) { cur.ad = SnapTick(); Commit(false); }
            }
            if (ph == Ph.Practice)
            {
                idleT += dt;
                if (idleT > 8f) ghostScale = idleT > 12f ? 1.7f : 1.45f;
                if (idleT > 12f && skipG && skipG.alpha < 1f) skipG.alpha = 1f;
            }
            if (ph == Ph.Title) TitleDemo(dt);
        }

        void Oscillate(float dt)
        {
            if (cur == null || dt <= 0) return;
            if (lockedThis && ph != Ph.Title) return;
            bool move = ph == Ph.Title || ph == Ph.Practice || ph == Ph.Play;
            if (!move) return;
            float sec = ph == Ph.Title ? 0.62f : Rules.TickSec(cur.stage);
            float spd = 1f / Mathf.Max(0.2f, sec);
            tickVis += oscDir * spd * dt;
            if (tickVis >= cur.adHi) { tickVis = cur.adHi; oscDir = -1; }
            if (tickVis <= cur.adLo) { tickVis = cur.adLo; oscDir = 1; }
            int snap = SnapTick();
            if (snap != lastSnap)
            {
                lastSnap = snap;
                if (ph != Ph.End) GitSound.Play("tick", 0.22f);
                RefreshLiveLabels(snap);
            }
        }

        void TitleDemo(float dt)
        {
            titleT += dt;
            if (!titleDrop && SnapTick() == 4 && oscDir > 0 && titleT > 1.6f)
            {
                titleDrop = true; titleDropT = 0; dropSquash = 1.1f; splitT = 1.2f; slideT = 1.1f;
                GitSound.Play("drop", 0.5f); GitSound.Play("tear", 0.45f);
            }
            if (titleDrop)
            {
                titleDropT += dt;
                if (titleDropT > 1.8f)
                {
                    titleDrop = false; splitT = 0; slideT = 0; dropSquash = 0; titleT = 0;
                    tickVis = cur != null ? cur.adLo : 1f; oscDir = 1;
                }
            }
        }

        void HandleInput(float dt)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                if (ph == Ph.Title) { GitSound.Play("press"); StartRun(true); return; }
                if (ph == Ph.End) { StartRun(true); return; }
                if ((ph == Ph.Play || ph == Ph.Practice) && !lockedThis) Drop();
            }

            if (MgfPointer.Down)
            {
                st.taps++;
                downScreen = MgfPointer.Position;
                downTime = Time.unscaledTime;
                dragMoved = false;
                press = HitPress();
                if (ph == Ph.End) press = Press.EndCta;
                else if (ph == Ph.Title)
                {
                    if (press == Press.Cta) pulseTm = 0.16f;
                }
                else if (press == Press.Other || press == Press.Flag) Refuse(MgfPointer.Position);
                else if (press == Press.Play) pulseTm = 0.12f;
                MgfBridge.NotifyChanged();
            }

            if (MgfPointer.Held && press != Press.None)
            {
                if ((MgfPointer.Position - downScreen).sqrMagnitude > 120f) dragMoved = true;
            }

            if (MgfPointer.Up && press != Press.None)
            {
                if (ph == Ph.End) { StartRun(true); press = Press.None; return; }
                if (ph == Ph.Title) { GitSound.Play("press"); StartRun(true); press = Press.None; return; }
                if (press == Press.Skip && ph == Ph.Practice) { EndPractice(); press = Press.None; return; }
                if ((ph == Ph.Play || ph == Ph.Practice) && !lockedThis)
                {
                    if (press == Press.Play && !dragMoved) Drop();
                    else if (press == Press.Other || press == Press.Flag) { /* already refused on down */ }
                }
                press = Press.None;
            }
        }

        Press HitPress()
        {
            if (ph == Ph.Practice && skipG && skipG.alpha > 0.5f && skipRt
                && RectTransformUtility.RectangleContainsScreenPoint(skipRt, MgfPointer.Position, null))
                return Press.Skip;
            if (ph == Ph.Title && ctaRt
                && RectTransformUtility.RectangleContainsScreenPoint(ctaRt, MgfPointer.Position, null))
                return Press.Cta;
            if (land && (ph == Ph.Play || ph == Ph.Practice))
            {
                float pane = 1f - 380f / Mathf.Max(1, Screen.width);
                if (MgfPointer.Position.x / Mathf.Max(1, Screen.width) > pane) return Press.Other;
            }
            if (!cam) return Press.Play;
            if (MgfPointer.DownHit(cam, out var hit))
            {
                var n = hit.collider ? hit.collider.name : "";
                if (n.StartsWith("Flag") || n.StartsWith("Spike") || n.StartsWith("Pile")) return Press.Flag;
                if (n.StartsWith("Cta")) return Press.Cta;
                return Press.Play;
            }
            float ny = MgfPointer.Position.y / Mathf.Max(1, Screen.height);
            float nx = MgfPointer.Position.x / Mathf.Max(1, Screen.width);
            if (ny > 0.08f && ny < 0.92f) return Press.Play;
            if (nx < 0.08f || nx > 0.92f) return Press.Other;
            return Press.Other;
        }

        void Refuse(Vector2 screen)
        {
            refuseAt = screen; refuseT = 0.45f;
            jiggleT = 0.18f;
            SpawnRipple(screen);
            GitSound.Play("ripple");
            toast(ph == Ph.Practice || ph == Ph.Play ? "밀대가 있는 천을 눌러라" : "", 0.7f);
            PointAtMilldae();
        }
    }
}
