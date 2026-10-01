// 석회 착지 — 우레탄 삼각 코너의 석회 칸을 한 칸 hop 하다 목표 선분비 칸에 착지하는 90초 홉 아케이드.
//
// 답 입력 경로: (1) 아래 칸 탭 = 1 cm hop  (2) 같은 칸 재탭 또는 나무 쐐기 탭(Enter) = 착지 확정.
// 판정은 Judge.Ok(정수 칸). 훅도 같은 Commit() 을 탄다. 하단 n지선다 없음. 코드는 정답 칸을 칠하지 않는다.
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;

namespace Mgf.SeokhoeChakji
{
    public partial class SeokhoeChakjiGame : MonoBehaviour, IMgfGame
    {
        [System.Serializable]
        class State : MgfState
        {
            public int combo, maxCombo, firstTry, attempts, taps, ad, of = 10, backs;
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
        float shownScore, titleT, endT, jiggleT, pulseTm, hopLock, hopAnim, landSquash, stampT, smearT, cardSlide, splitT;
        float shownCm, cmVel, swingAmp, shimmerT, dustT;
        int shownScoreInt = -1, lastTimeInt = -1;
        Vector2 refuseAt;
        int best, bestScore, bestCombo, backsLeft;
        string endReason = "";
        float ghostScale = 1f;
        int obStep;
        bool land;
        int demoAd = 1;
        float demoHop;
        int playSheetN;
        bool sheetTimed;
        RaycastHit lastHit;
        bool lastHitOk;

        enum Press { None, Block, Play, Cta, EndCta, Skip, Other }
        Press press;
        Vector2 downScreen, lastScreen;
        float downTime, horizAcc, vertAcc;
        bool dragMoved;

        static readonly System.Type[] keepTypes = { typeof(BoxCollider), typeof(SphereCollider), typeof(MeshCollider), typeof(CapsuleCollider) };

        void Awake()
        {
            SeokhoeSound.Init(gameObject);
            BuildBank();
            BuildWorld();
            BuildUi();
            Prewarm();
            _ = keepTypes[0];
            best = PlayerPrefs.GetInt("seokhoe.best", 0);
            bestScore = PlayerPrefs.GetInt("seokhoe.bestScore", 0);
            bestCombo = PlayerPrefs.GetInt("seokhoe.bestCombo", 0);
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb = new System.Text.StringBuilder(
                "석회착지칸을눌러내려가맞는칸에서쐐기를눌러라시작하기중학교2학년평행선과선분의길이의비최고착지콤보노란카드시간종료소진다시하기한판90초본판10회연습중점무게중심중선넓이닮음평행칸cm²△ABCDEGMN구하시오착지hop칸을눌러블록을눌러쐐기를눌러여기서한번더같은비의칸을이으면그줄이밑변과평행하다중점연결정리꼭짓점으로부터잘못걸린옮겨밑변의이등분세평행선오늘의삼각코너연속첫시도정답현재비칸탭블록탭착지부터까지의길이를나무쐐기콘크리트같은칸을다시누르거나");
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
            string p = Words.Prompt(s);
            if (!seen.Add(p + "|" + Words.Answer(s))) return;
            bank.Add(new MgfProblem
            {
                id = "sk" + (bank.Count + 1),
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
            cur.ad = cur.target;
            Commit(true);
        }

        public void TestAnswerWrong()
        {
            if (ph == Ph.Title || ph == Ph.End) StartRun(false);
            if (ph == Ph.Practice) EndPractice();
            FinishPending();
            if (ph != Ph.Play || cur == null) return;
            int w = cur.misTick > 0 && cur.misTick != cur.target ? cur.misTick : (cur.target == cur.adLo ? cur.adHi : cur.adLo);
            if (w == cur.target) w = Mathf.Clamp(cur.target - 1, cur.adLo, cur.adHi);
            cur.ad = w;
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
                st.backs = backsLeft;
            }
            return JsonUtility.ToJson(st);
        }

        public string ProblemBankJson() => MgfJson.Bank(bank);

        void ShowTitle()
        {
            ph = Ph.Title;
            titleT = 0; endReason = "";
            cur = SheetGen.Practice();
            cur.ab = 12; cur.ac = 12; cur.bc = 10; cur.target = 8; cur.m = 2; cur.n = 1;
            cur.ad = 1; cur.ad0 = 1; cur.adHi = 11; cur.de = Judge.De(cur, 8);
            if (cur.de < 0) cur.de = 6;
            demoAd = 1; cur.ad = 1;
            SetupSheet(cur, true);
            bestTxt.text = bestScore > 0 ? "최고 점수 " + bestScore + " · 착지 " + best + " · 콤보 " + bestCombo : "한 판 90초 · 착지 10회";
            SetVisible();
            MgfBridge.NotifyChanged();
        }

        void StartRun(bool withPractice)
        {
            CancelInvoke();
            st.score = 0; st.solved = 0; st.combo = 0; st.maxCombo = 0; st.firstTry = 0; st.attempts = 0;
            st.lives = Rules.Cards; st.level = 1; st.of = Rules.Lands;
            shownScore = 0; shownScoreInt = -1; lastTimeInt = -1;
            runLeft = Rules.RunSec; frozen = false; endReason = ""; stampT = 0; smearT = 0; splitT = 0;
            rng = new System.Random(System.DateTime.Now.Millisecond + 17);
            deck = new SheetGen(rng).Deck();
            deckIdx = -1;
            playSheetN = 0;
            HideToast();
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
            ghostScale = 1f;
            if (skipG) skipG.alpha = 0;
            NextSheet();
            SetVisible();
        }

        void NextSheet()
        {
            FinishPending();
            lockedThis = false; tried = false; idleT = 0; revealT = 0; stampT = 0; splitT = 0;
            playSheetN++;
            sheetTimed = playSheetN > Rules.UntimedSheets;
            sheetLeft = Rules.SheetSec;
            backsLeft = 2;
            int stage = st.solved < 4 ? 1 : st.solved < 7 ? 2 : 3;
            if (st.combo >= 3) stage = Mathf.Min(3, stage + 1);
            if (runLeft <= 60f && stage < 2) stage = 2;
            if (runLeft <= 30f) stage = 3;
            st.level = stage;
            deckIdx++;
            Sheet s = null;
            if (deck != null && deckIdx < deck.Count)
            {
                s = deck[deckIdx].Clone();
                s.no = st.solved + 1;
                s.stage = stage;
                if (stage >= 2) s.hideRatio = true;
            }
            if (s == null) s = new SheetGen(rng).NextFor(st.solved + 1, stage);
            cur = s;
            cur.Reset();
            shownCm = cur.ad;
            SetupSheet(cur, false);
            RefreshHud();
            MgfBridge.NotifyChanged();
        }

        void FinishPending()
        {
            if (revealT > 0) revealT = 0;
            if (stampT > 0) stampT = 0;
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
                    stampT = 1.6f; revealT = 1.6f; landSquash = 1.2f;
                    SeokhoeSound.Play("land"); SeokhoeSound.Play("stamp", 0.85f);
                    PunchBlock();
                    goalTxt.text = Words.RevealRight(cur);
                    cmGoal = cur.asked;
                    Invoke(nameof(EndPractice), 1.6f);
                }
                else
                {
                    revealT = 0.9f; jiggleT = 0.35f;
                    SeokhoeSound.Play("smear"); SeokhoeSound.Play("refuse");
                    ShowGhost(cur.target);
                    toast("4 cm 칸을 눌러 착지해라", 1.2f);
                    lockedThis = false;
                    RefreshBlock(true);
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
                if (cur.kind == Kind.Cent || cur.kind == Kind.Area) add += 80;
                st.score += add;
                st.solved++;
                hitStop = 0.09f; stampT = 1.15f; landSquash = 1.22f;
                if (cur.kind == Kind.Cent || cur.kind == Kind.Area) splitT = 1.15f;
                SeokhoeSound.Play("land"); SeokhoeSound.Play("stamp");
                if (st.combo >= 2) SeokhoeSound.Play("combo", 0.55f + 0.08f * Mathf.Min(3, st.combo));
                PunchBlock();
                goalTxt.text = Words.RevealRight(cur);
                cmGoal = cur.asked;
                if (st.solved >= Rules.Lands) Invoke(nameof(ClearRun), 1.2f);
                else Invoke(nameof(NextSheet), 1.2f);
            }
            else
            {
                st.combo = 0;
                st.lives--;
                revealT = 0.85f; smearT = 0.6f; jiggleT = 0.22f; cardSlide = 0.55f;
                SeokhoeSound.Play("smear"); SeokhoeSound.Play("card");
                ShowGhost(cur.target);
                toast(Words.RevealWrong(cur), 1.15f);
                if (st.lives <= 0) Invoke(nameof(FailCards), 0.9f);
                else Invoke(nameof(NextSheet), 0.95f);
            }
            RefreshHud();
            MgfBridge.NotifyChanged();
        }

        void ClearRun() { EndRun("clear"); }
        void FailCards() { EndRun("cards"); }
        void FailTime() { EndRun("time"); }

        void EndRun(string why)
        {
            CancelInvoke();
            FinishPending();
            ph = Ph.End; endReason = why; endT = 0;
            if (st.solved > best) { best = st.solved; PlayerPrefs.SetInt("seokhoe.best", best); }
            if (st.combo > bestCombo) { bestCombo = st.maxCombo; PlayerPrefs.SetInt("seokhoe.bestCombo", bestCombo); }
            if (st.score > bestScore) { bestScore = st.score; PlayerPrefs.SetInt("seokhoe.bestScore", bestScore); PlayerPrefs.Save(); }
            if (why == "clear") SeokhoeSound.Play("win"); else SeokhoeSound.Play("whistle");
            SetVisible();
            MgfBridge.NotifyChanged();
        }

        float cmGoal;

        void Update()
        {
            float dt = Time.deltaTime;
            if (hitStop > 0) { hitStop -= dt; dt = 0; }
            bool nowLand = (float)Screen.width / Mathf.Max(1, Screen.height) >= 1.2f;
            if (nowLand != land && cur != null)
            {
                land = nowLand;
                ApplyWideQuality();
                LayoutTri(cur);
                DrawStatic(cur);
                RefreshBlock(true);
                LayoutHud();
                FrameCam();
            }
            else land = nowLand;
            Animate(dt);
            HandleInput(dt);
            if (ph == Ph.Play && !frozen && dt > 0 && !lockedThis)
            {
                runLeft -= dt;
                if (sheetTimed) sheetLeft -= dt;
                if (runLeft <= 0) FailTime();
                else if (sheetTimed && sheetLeft <= 0) Commit(false);
            }
            if (ph == Ph.Practice)
            {
                idleT += dt;
                if (idleT > 12f)
                {
                    ghostScale = 1.7f;
                    if (skipG && skipG.alpha < 1f) skipG.alpha = 1f;
                    if (goalTxt) goalTxt.text = "4 cm 칸을 눌러 착지   ·   아니면 본판으로";
                }
                else if (idleT > 8f) ghostScale = 1.45f;
            }
        }

        void HandleInput(float dt)
        {
            if (hopLock > 0) hopLock -= dt;

            if ((ph == Ph.Play || ph == Ph.Practice) && !lockedThis)
            {
                if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.Space)) Hop(1);
                if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) Hop(-1);
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) TryLand();
            }

            if (MgfPointer.Down)
            {
                st.taps++;
                downScreen = lastScreen = MgfPointer.Position;
                downTime = Time.unscaledTime;
                horizAcc = 0; vertAcc = 0; dragMoved = false;
                press = HitPress();
                if (ph == Ph.End) press = Press.EndCta;
                else if (ph == Ph.Title) { /* wait up */ }
                else if (press == Press.Other) Refuse(MgfPointer.Position);
                else if (press == Press.Play) pulseTm = 0.18f;
                MgfBridge.NotifyChanged();
            }

            if (MgfPointer.Held && press != Press.None)
            {
                var p = MgfPointer.Position;
                var dlt = p - lastScreen;
                lastScreen = p;
                horizAcc += dlt.x; vertAcc += dlt.y;
                if ((p - downScreen).sqrMagnitude > 100f) dragMoved = true;
            }

            if (MgfPointer.Up && press != Press.None)
            {
                if (ph == Ph.End) { StartRun(true); press = Press.None; return; }
                if (ph == Ph.Title) { StartRun(true); press = Press.None; return; }
                if (press == Press.Skip && ph == Ph.Practice) { EndPractice(); press = Press.None; return; }
                if ((ph == Ph.Play || ph == Ph.Practice) && !lockedThis)
                {
                    if (vertAcc > 70f && Mathf.Abs(vertAcc) > Mathf.Abs(horizAcc)) Hop(-1);
                    else if (vertAcc < -70f && Mathf.Abs(vertAcc) > Mathf.Abs(horizAcc)) Hop(1);
                    else if (press == Press.Block && !dragMoved) TryLand();
                    else if (press == Press.Play && !dragMoved)
                    {
                        int rung = NearestRung();
                        if (ph == Ph.Practice && cur != null) PracticeTap(rung);
                        else
                        {
                            // 같은 칸 재탭 = 착지. 아래 칸 탭 = hop, 위 칸 = 후진.
                            bool landable = cur != null && cur.ad != cur.ad0 && cur.ad != cur.adHi;
                            if (landable && rung == cur.ad) TryLand();
                            else if (rung >= 0 && cur != null && rung < cur.ad) Hop(-1);
                            else Hop(1);
                        }
                    }
                }
                press = Press.None;
            }
        }

        void PracticeTap(int rung)
        {
            if (cur == null) return;
            // 연습: 4 cm 칸을 한 번 탭하면 그 칸으로 옮기고 바로 착지. 다른 칸은 점프.
            if (rung == cur.target)
            {
                cur.ad = cur.target;
                hopAnim = 0.12f; landSquash = 0.9f;
                RefreshBlock(true);
                TryLand();
                return;
            }
            if (rung == cur.ad && cur.ad != cur.ad0 && cur.ad != cur.adHi)
            {
                TryLand();
                return;
            }
            if (rung >= cur.adLo && rung <= cur.adHi && rung != cur.ad)
            {
                cur.ad = rung;
                hopLock = Rules.HopMin;
                hopAnim = 0.18f;
                landSquash = 0.82f;
                SeokhoeSound.Play("hop", 0.7f);
                dustT = 0.28f;
                RefreshBlock(true);
                if (cur.ad == cur.target) TryLand();
                else if (goalTxt) goalTxt.text = "4 cm 칸을 눌러 착지";
                MgfBridge.NotifyChanged();
                return;
            }
            Hop(1);
        }

        Press HitPress()
        {
            lastHitOk = false;
            if (ph == Ph.Practice && skipG && skipG.alpha > 0.5f && skipRt
                && RectTransformUtility.RectangleContainsScreenPoint(skipRt, MgfPointer.Position, null))
                return Press.Skip;
            if (!cam) return Press.Other;
            if (MgfPointer.DownHit(cam, out var hit))
            {
                lastHit = hit; lastHitOk = true;
                var n = hit.collider ? hit.collider.name : "";
                if (n.StartsWith("Block") || n.StartsWith("Start")) return Press.Block;
                if (n.StartsWith("Play") || n.StartsWith("Track") || n.StartsWith("Tri") || n.StartsWith("Rung") || n.StartsWith("Grass")) return Press.Play;
                if (n.StartsWith("Cta")) return Press.Cta;
            }
            float ny = MgfPointer.Position.y / Mathf.Max(1, Screen.height);
            if (ny > 0.16f && ny < 0.90f) return Press.Play;
            return Press.Other;
        }

        int NearestRung()
        {
            if (cur == null || !cam) return -1;
            Vector2 p;
            if (lastHitOk) p = new Vector2(lastHit.point.x, lastHit.point.z);
            else if (MgfPointer.OnPlane(cam, 0.05f, out var w)) p = new Vector2(w.x, w.z);
            else return -1;
            // 좌우가 아니라 A→밑변 축의 높이로 칸을 고른다. 같은 평행 줄 왼쪽을 눌러도 재탭=착지.
            var axis = ((B2 + C2) * 0.5f) - A2;
            float axisSq = axis.sqrMagnitude;
            if (axisSq < 1e-6f) return -1;
            float t = Vector2.Dot(p - A2, axis) / axisSq;
            int span = Mathf.Max(1, cur.Span);
            int cm = Mathf.RoundToInt(t * span);
            return Mathf.Clamp(cm, cur.adLo, cur.adHi);
        }

        void Hop(int dir)
        {
            if (cur == null || lockedThis) return;
            if (dir > 0 && hopLock > 0) { JiggleBlock(); SeokhoeSound.Play("refuse", 0.25f); return; }
            if (ph == Ph.Practice && dir > 0 && cur.ad == cur.target)
            {
                JiggleBlock();
                SeokhoeSound.Play("refuse", 0.35f);
                toast("4 cm 칸을 눌러 착지해라", 1.15f);
                return;
            }
            int nad = Mathf.Clamp(cur.ad + dir, cur.adLo, cur.adHi);
            if (nad == cur.ad)
            {
                JiggleBlock();
                SeokhoeSound.Play("refuse", 0.3f);
                toast(dir < 0 ? "더 이상 올라갈 칸이 없다" : "밑변 칸에는 착지할 수 없다", 0.9f);
                return;
            }
            if (dir < 0)
            {
                if (backsLeft <= 0 && ph == Ph.Play)
                {
                    JiggleBlock(); SeokhoeSound.Play("refuse", 0.35f);
                    toast("이 장은 후진 2회를 다 썼다", 1.0f);
                    return;
                }
                backsLeft--;
            }
            cur.ad = nad;
            hopLock = Rules.HopMin;
            hopAnim = 0.18f;
            landSquash = 0.82f;
            SeokhoeSound.Play("hop", 0.7f, 0.92f + 0.04f * (nad % 3));
            dustT = 0.28f;
            RefreshBlock(true);
            if (cur.ad != cur.ad0 && cur.ad != cur.adHi)
            {
                obStep = 1;
                if (goalTxt) goalTxt.text = ph == Ph.Practice
                    ? (cur.ad == cur.target ? "4 cm — 이 칸을 다시 누르거나 쐐기를 눌러 착지" : "4 cm 칸을 눌러 착지")
                    : "같은 칸을 다시 누르거나 쐐기를 눌러 착지";
            }
            MgfBridge.NotifyChanged();
        }

        void TryLand()
        {
            if (cur == null || lockedThis) return;
            if (cur.ad == cur.ad0)
            {
                JiggleBlock();
                SeokhoeSound.Play("refuse", 0.45f);
                toast(ph == Ph.Practice ? "4 cm 칸을 눌러 착지해라" : "꼭짓점 칸은 착지할 수 없다", 1.05f);
                return;
            }
            if (cur.ad == cur.adHi)
            {
                JiggleBlock();
                SeokhoeSound.Play("refuse", 0.45f);
                toast("밑변 칸은 착지할 수 없다", 1.0f);
                return;
            }
            Commit(Judge.Ok(cur));
        }

        void Refuse(Vector2 screen)
        {
            refuseT = 0.32f;
            refuseAt = screen;
            JiggleBlock();
            SeokhoeSound.Play("ripple");
            PointAtBlock();
            toast(ph == Ph.Practice ? "4 cm 칸을 눌러 착지해라" : "칸을 눌러 hop 한다", 0.85f);
        }

        void toast(string s, float dur) { toastTxt.text = s; toastT = 0; toastDur = dur; toastG.alpha = 1; }
        void HideToast() { toastT = 9; if (toastG) toastG.alpha = 0; }
    }
}
