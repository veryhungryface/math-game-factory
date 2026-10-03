// 제곱 얹기 — 직각삼각형 세 변 쟁반에 넓이판을 끌어 얹고 나무못으로 잠그는 90초 기와골 퍼즐.
//
// 답 입력 경로: (1) 기왓장을 쟁반으로 드래그  (2) 그 쟁반의 나무못을 탭해 잠금
//               역 판별은 꼭짓점 도장 탭 또는 지그를 폐기 홈으로.
// 판정은 Judge.LockOk / StampOk / DiscardOk (정수). 훅도 같은 Commit/Stamp/DiscardJig 를 탄다.
// 하단 n지선다 없음. 코드는 정답 쟁반으로 기왓장을 밀어 넣지 않는다.
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;

namespace Mgf.JegopEonki
{
    public partial class JegopEonkiGame : MonoBehaviour, IMgfGame
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
        System.Random rng = new System.Random(20261002);
        readonly List<MgfProblem> bank = new List<MgfProblem>();

        class Tile
        {
            public int area;
            public string mis = "";
            public Transform root;
            public Vector3 dockPos;
            public int onTray = -1; // -1 dock, -2 discard, 0..2 tray
            public bool locked, cracked;
            public float squash = 1f, yaw;
            public TextMeshPro label;
            public Transform grid;
        }

        readonly Tile[] tiles = new Tile[3];
        readonly int[] trayArea = new int[3];
        readonly bool[] trayLocked = new bool[3];

        bool frozen, tried, lockedThis;
        float runLeft, sheetLeft, hitStop, idleT, ghostT, revealT, refuseT, toastT, toastDur;
        float shownScore, titleT, endT, jiggleT, pulseTm, swingT, trailT, crackT, slideT, stampT;
        float shownN, nVel; int nGoal, shownScoreInt = -1, lastTimeInt = -1;
        int best, bestScore, bestCombo;
        string endReason = "";
        int obStep;
        int ghostBeat; // 0: 9→빗변, 1: 16→빗변, 2: 나무못
        bool land;
        int playSheetN;
        bool sheetTimed;
        bool afterWrong;
        int dragTile = -1;
        int selTile = -1; // 탭으로 고른 기왓장 — 쟁반·폐기 홈을 탭하면 그리로 간다(드래그 대체 경로)
        Vector3 dragOff;
        int lastRippleFrame = -1;

        enum Press { None, Tile, Latch, Stamp, Discard, Jig, Cta, EndCta, Skip, Other }
        Press press;
        int pressIdx;
        Vector2 downScreen;
        float downTime;
        bool dragMoved;

        void Awake()
        {
            JegopSound.Init(gameObject);
            BuildBank();
            BuildWorld();
            BuildUi();
            Prewarm();
            best = PlayerPrefs.GetInt("jegop.best", 0);
            bestScore = PlayerPrefs.GetInt("jegop.bestScore", 0);
            bestCombo = PlayerPrefs.GetInt("jegop.bestCombo", 0);
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb = new System.Text.StringBuilder(
                "제곱얹기넓이판을변에얹고잠가라시작하기중학교2학년피타고라스정리최고잠금콤보작업대열기다시하기한판90초본판10회연습빗변직각도장폐기금간기와쇠꽂이쟁반나무못기왓장구하시오cm²△ABC∠잠금x>0이므로연속첫시도정답길이의합넓이판정별세변가장긴변맞은편꼭짓점공방닫힘시간종료소진끌어얹어라먼저얹어라직각삼각형이다아니다정오기와골삼나무삼각지그작업대탭하면칸기왓장이얹힌다폐기홈을버린다이0123456789");
            foreach (var p in bank) { sb.Append(p.prompt); sb.Append(p.answer); }
            var g = new SheetGen(new System.Random(3));
            for (int i = 0; i < 8; i++)
                foreach (var s in g.Deck())
                {
                    sb.Append(Words.Prompt(s));
                    sb.Append(Words.RevealRight(s));
                    sb.Append(Words.RevealWrong(s));
                    sb.Append(Words.Goal(s));
                }
            MgfText.Prewarm(sb.ToString());
        }

        void BuildBank()
        {
            var seen = new HashSet<string>();
            Add(SheetGen.Practice(), seen);
            var g = new SheetGen(new System.Random(777));
            foreach (var s in g.Exhaust()) Add(s, seen);
            int guard = 0;
            while (bank.Count < 360 && guard++ < 8000)
                Add(g.Any(), seen);
        }

        void Add(Sheet s, HashSet<string> seen)
        {
            if (s == null) return;
            string p = Words.Prompt(s);
            string a = Words.Answer(s);
            if (!seen.Add(p + "|" + a)) return;
            var ch = Words.Choices(s);
            bool has = false;
            for (int i = 0; i < ch.Length; i++) if (ch[i] == a) has = true;
            if (!has) return;
            double num = s.kind == Kind.Reverse ? (s.reverseTrue ? 1 : 0) : s.asked;
            bank.Add(new MgfProblem
            {
                id = "je" + (bank.Count + 1),
                prompt = p,
                choices = ch,
                answer = a,
                answerNumeric = num,
                unitConcept = Words.Concept(s)
            });
        }

        public void TestStart() { StartRun(false); }

        public void TestAnswerCorrect()
        {
            if (ph == Ph.Title || ph == Ph.End) StartRun(false);
            if (ph == Ph.Practice) { EndPractice(); return; }
            FinishPending();
            CancelInvoke();
            lockedThis = false;
            if (ph != Ph.Play || cur == null) return;
            if (cur.kind == Kind.Reverse)
            {
                if (cur.reverseTrue) Stamp(cur.rightVertex);
                else DiscardJig();
                return;
            }
            ClearTilesOnto(-1);
            int side = Mathf.Clamp(cur.targetSide, 0, 2);
            int need = cur.targetArea > 0 ? cur.targetArea : cur.Sq(side);
            for (int i = 0; i < 3; i++)
            {
                if (tiles[i] == null) continue;
                tiles[i].onTray = -1;
                tiles[i].locked = false;
            }
            if (tiles[0] != null)
            {
                tiles[0].area = need;
                PlaceOnTray(0, side, false);
            }
            LockTray(side);
        }

        public void TestAnswerWrong()
        {
            if (ph == Ph.Title || ph == Ph.End) StartRun(false);
            if (ph == Ph.Practice) EndPractice();
            FinishPending();
            CancelInvoke();
            lockedThis = false;
            if (ph != Ph.Play || cur == null) return;
            if (cur.kind == Kind.Reverse)
            {
                if (cur.reverseTrue) DiscardJig();
                else Stamp(0);
                return;
            }
            int wrong = cur.targetSide == 0 ? 1 : 0;
            ClearTilesOnto(-1);
            if (tiles[0] != null)
            {
                tiles[0].area = 1;
                PlaceOnTray(0, wrong, false);
            }
            LockTray(wrong);
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
            titleT = 0; endReason = "";
            cur = SheetGen.Practice();
            SetupSheet(cur, true);
            bestTxt.text = bestScore > 0
                ? "최고 점수 " + bestScore + "  ·  잠금 " + best + "  ·  콤보 " + bestCombo
                : "한 판 90초  ·  잠금 10회";
            SetVisible();
            MgfBridge.NotifyChanged();
        }

        void StartRun(bool withPractice)
        {
            CancelInvoke();
            selTile = -1;
            FinishPending();
            lockedThis = false;
            tried = false;
            hitStop = 0; revealT = 0; refuseT = 0; swingT = 0; crackT = 0; slideT = 0; stampT = 0;
            st.score = 0; st.solved = 0; st.combo = 0; st.maxCombo = 0; st.firstTry = 0; st.attempts = 0;
            st.lives = Rules.Lives; st.level = 1; st.of = Rules.Locks; st.taps = 0;
            shownScore = 0; shownScoreInt = -1; lastTimeInt = -1;
            runLeft = Rules.RunSec; frozen = false; endReason = "";
            afterWrong = false;
            rng = new System.Random(System.DateTime.Now.Millisecond + 17);
            deck = new SheetGen(rng).Deck();
            deckIdx = -1;
            playSheetN = 0;
            HideToast();
            ResetSpike();
            if (withPractice)
            {
                ph = Ph.Practice; obStep = 0; ghostBeat = 0; idleT = 0; ghostT = 0; frozen = true;
                ghostScale = 1f;
                if (skipG) { skipG.alpha = 0; skipG.blocksRaycasts = false; }
                cur = SheetGen.Practice();
                SetupSheet(cur, false);
                PracticeGhost();
                RefreshHypLabel();
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
            selTile = -1;
            ph = Ph.Play;
            st.score = 0; st.solved = 0; st.combo = 0; st.lives = Rules.Lives;
            if (skipG) { skipG.alpha = 0; skipG.blocksRaycasts = false; }
            HideGhost();
            if (hypLbl) hypLbl.gameObject.SetActive(false);
            NextSheet();
            SetVisible();
        }

        void NextSheet()
        {
            FinishPending();
            selTile = -1;
            lockedThis = false; tried = false; idleT = 0; revealT = 0; slideT = 0; swingT = 0; stampT = 0;
            playSheetN++;
            sheetTimed = playSheetN > Rules.UntimedSheets;
            int stage = st.solved < 3 ? 1 : st.solved < 7 ? 2 : 3;
            if (runLeft <= 60f && stage < 2) stage = 2;
            if (runLeft <= 30f) stage = 3;
            st.level = stage;
            sheetLeft = Rules.SheetLimit(stage);
            deckIdx++;
            Sheet s = null;
            if (afterWrong && stage >= 2)
            {
                s = new SheetGen(rng).NextFor(st.solved + 1, 2);
                afterWrong = false;
            }
            else if (deck != null && deckIdx < deck.Count)
            {
                s = deck[deckIdx].Clone();
                s.no = st.solved + 1;
                s.stage = stage;
                if (stage >= 2) s.ghostOn = false;
            }
            if (s == null) s = new SheetGen(rng).NextFor(st.solved + 1, stage);
            cur = s;
            shownN = 0; nVel = 0; nGoal = 0;
            SetupSheet(cur, false);
            RefreshHud();
            MgfBridge.NotifyChanged();
        }

        void FinishPending()
        {
            selTile = -1;
            if (revealT > 0) revealT = 0;
            HideGhost();
        }

        void ClearTilesOnto(int tray)
        {
            for (int i = 0; i < 3; i++)
            {
                if (tiles[i] == null) continue;
                tiles[i].onTray = tray;
                tiles[i].locked = false;
                tiles[i].cracked = false;
            }
            RecountTrays();
        }

        void PlaceOnTray(int ti, int tray, bool sound)
        {
            if (ti < 0 || ti > 2 || tiles[ti] == null) return;
            if (trayLocked[tray]) return;
            tiles[ti].onTray = tray;
            tiles[ti].locked = false;
            RecountTrays();
            SnapTile(ti);
            if (sound) JegopSound.Play("place", 0.8f);
        }

        void RecountTrays()
        {
            trayArea[0] = trayArea[1] = trayArea[2] = 0;
            for (int i = 0; i < 3; i++)
            {
                if (tiles[i] == null) continue;
                int t = tiles[i].onTray;
                if (t >= 0 && t <= 2) trayArea[t] += tiles[i].area;
            }
            RefreshTrayLabels();
        }

        void LockTray(int tray)
        {
            if (cur == null || lockedThis) return;
            if (ph != Ph.Play && ph != Ph.Practice) return;
            if (cur.kind == Kind.Reverse)
            {
                Refuse("도장 또는 폐기로 판별하라");
                JiggleLatch(tray);
                return;
            }
            if (trayLocked[tray])
            {
                JiggleLatch(tray);
                JegopSound.Play("refuse");
                return;
            }
            int area = trayArea[tray];
            if (area <= 0)
            {
                JiggleLatch(tray);
                JegopSound.Play("refuse");
                toast("기왓장을 먼저 얹어라", 0.9f);
                return;
            }
            bool ok = Judge.LockOk(cur, tray, area);
            bool done = Judge.Completes(cur, tray, area);
            if (ph == Ph.Practice)
            {
                PracticeLock(tray, area, ok, done);
                return;
            }
            if (done)
            {
                trayLocked[tray] = true;
                LockTilesOn(tray);
                Commit(true, tray, area);
                return;
            }
            if (ok)
            {
                // 맞는 변을 잠갔지만 이 장의 목표 쟁반은 아님 — 판은 남기고 계속.
                trayLocked[tray] = true;
                LockTilesOn(tray);
                swingT = 0.28f; swingTray = tray;
                JegopSound.Play("latch", 0.7f);
                PulseTray(tray);
                RefreshHud();
                MgfBridge.NotifyChanged();
                return;
            }
            CrackOn(tray);
            Commit(false, tray, area);
        }

        int swingTray;

        void PracticeLock(int tray, int area, bool ok, bool done)
        {
            // 발문(AB 위 정사각형 25)과 같게: 9+16을 빗변 쟁반에 얹고 그 나무못을 누른다.
            if (done || (ok && tray == 2 && area == 25))
            {
                trayLocked[2] = true;
                LockTilesOn(2);
                obStep = 1;
                lockedThis = true;
                nGoal = 25; shownN = 0;
                revealT = 1.7f;
                swingT = 0.32f; swingTray = 2;
                JegopSound.Play("latch");
                JegopSound.Play("kiln");
                if (goalTxt) goalTxt.text = Words.RevealRight(cur);
                HideGhost();
                Invoke(nameof(EndPractice), 1.75f);
                RefreshHud();
                MgfBridge.NotifyChanged();
                return;
            }
            JiggleLatch(tray);
            JegopSound.Play("refuse");
            if (tray == 2 && area > 0 && area < 25)
                toast("9와 16을 함께 얹고 잠가라", 1.1f);
            else
                toast("빗변 쟁반에 9와 16을 얹고 잠가라", 1.1f);
            PracticeGhost();
            RefreshHud();
            MgfBridge.NotifyChanged();
        }

        int TileIndexByArea(int area)
        {
            for (int i = 0; i < 3; i++)
                if (tiles[i] != null && tiles[i].area == area) return i;
            return 0;
        }

        bool TrayHas(int tray, int area)
        {
            for (int i = 0; i < 3; i++)
                if (tiles[i] != null && tiles[i].onTray == tray && tiles[i].area == area) return true;
            return false;
        }

        void PracticeGhost()
        {
            if (ph != Ph.Practice || fingerT == null) return;
            if (!TrayHas(2, 9))
            {
                ghostBeat = 0;
                ShowGhost(TileIndexByArea(9), 2);
            }
            else if (!TrayHas(2, 16))
            {
                ghostBeat = 1;
                ShowGhost(TileIndexByArea(16), 2);
            }
            else
            {
                ghostBeat = 2;
                ShowGhostLatch(2);
            }
        }

        void LockTilesOn(int tray)
        {
            for (int i = 0; i < 3; i++)
                if (tiles[i] != null && tiles[i].onTray == tray) tiles[i].locked = true;
        }

        void CrackOn(int tray)
        {
            crackT = 0.4f;
            jiggleT = 0.32f;
            for (int i = 0; i < 3; i++)
                if (tiles[i] != null && tiles[i].onTray == tray) tiles[i].cracked = true;
        }

        void Stamp(int vertex)
        {
            if (cur == null || lockedThis) return;
            if (ph != Ph.Play && ph != Ph.Practice) return;
            if (cur.kind != Kind.Reverse)
            {
                Refuse("넓이판을 쟁반에 얹어라");
                return;
            }
            stampT = 0.35f;
            stampVertex = vertex;
            JegopSound.Play("stamp");
            bool ok = Judge.StampOk(cur, vertex);
            Commit(ok, -1, 0);
        }

        int stampVertex;

        void DiscardJig()
        {
            if (cur == null || lockedThis) return;
            if (ph != Ph.Play && ph != Ph.Practice) return;
            if (cur.kind != Kind.Reverse)
            {
                // 기왓장 폐기는 DiscardTile 에서 처리
                return;
            }
            bool ok = Judge.DiscardOk(cur);
            JegopSound.Play(ok ? "place" : "crack");
            Commit(ok, -1, 0);
        }

        void DiscardTile(int ti)
        {
            if (ti < 0 || tiles[ti] == null || tiles[ti].locked) return;
            tiles[ti].onTray = -2;
            RecountTrays();
            SnapTile(ti);
            JegopSound.Play("place", 0.55f);
            MgfBridge.NotifyChanged();
        }

        void Commit(bool ok, int tray, int area)
        {
            if (lockedThis && ph == Ph.Play) return;
            CancelInvoke();
            lockedThis = true;
            st.attempts++;
            if (!tried) { tried = true; if (ok) st.firstTry++; }

            if (ph == Ph.Practice)
            {
                RefreshHud();
                MgfBridge.NotifyChanged();
                return;
            }
            if (ph != Ph.Play) return;

            if (ok)
            {
                st.combo++;
                if (st.combo > st.maxCombo) st.maxCombo = st.combo;
                int add = 100 * Rules.Mult(st.combo);
                if (cur.kind == Kind.Reverse) add += 40;
                st.score += add;
                st.solved++;
                hitStop = 0.09f;
                revealT = cur.kind == Kind.AreaHyp ? 1.65f : 1.15f;
                swingT = 0.32f; swingTray = tray >= 0 ? tray : 2;
                if (cur.kind == Kind.AreaHyp) slideT = 1.1f;
                JegopSound.Play("latch");
                if (cur.kind == Kind.AreaHyp) JegopSound.Play("kiln", 0.8f);
                if (st.combo >= 2) JegopSound.Play("combo", 0.5f + 0.08f * Mathf.Min(3, st.combo));
                nGoal = cur.kind == Kind.AreaHyp || cur.askedUnit == "cm²" ? cur.targetArea : cur.asked;
                goalTxt.text = Words.RevealRight(cur);
                float hold = cur.kind == Kind.AreaHyp ? 1.65f : 1.15f;
                if (st.solved >= Rules.Locks) Invoke(nameof(ClearRun), hold);
                else Invoke(nameof(NextSheet), hold);
            }
            else
            {
                st.combo = 0;
                st.lives--;
                afterWrong = true;
                revealT = 0.85f; jiggleT = 0.28f; crackT = 0.45f;
                JegopSound.Play("crack"); JegopSound.Play("impale");
                ImpaleTile();
                toast(Words.RevealWrong(cur), 1.2f);
                if (st.lives <= 0) EndRun("flags");
                else Invoke(nameof(NextSheet), 0.95f);
            }
            RefreshHud();
            MgfBridge.NotifyChanged();
        }

        void ClearRun() { EndRun("clear"); }

        void EndRun(string why)
        {
            CancelInvoke();
            FinishPending();
            lockedThis = false;
            frozen = true;
            ph = Ph.End; endReason = why; endT = 0;
            if (st.solved > best) { best = st.solved; PlayerPrefs.SetInt("jegop.best", best); }
            if (st.maxCombo > bestCombo) { bestCombo = st.maxCombo; PlayerPrefs.SetInt("jegop.bestCombo", bestCombo); }
            if (st.score > bestScore) { bestScore = st.score; PlayerPrefs.SetInt("jegop.bestScore", bestScore); PlayerPrefs.Save(); }
            JegopSound.Play(why == "clear" ? "win" : "lose");
            SetVisible();
            MgfBridge.NotifyChanged();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (hitStop > 0) { hitStop -= dt; dt *= 0.15f; }
            bool nowLand = Screen.width >= 1024 && (float)Screen.width / Mathf.Max(1, Screen.height) >= 1.2f;
            if (nowLand != land)
            {
                land = nowLand;
                LayoutHud();
                FrameCam();
                LayoutDock();
                if (ph == Ph.Practice)
                {
                    RefreshHypLabel();
                    PracticeGhost();
                }
            }
            Animate(dt);
            HandleInput(dt);
            if (ph == Ph.Play && !frozen && dt > 0 && !lockedThis)
            {
                runLeft -= dt;
                if (sheetTimed) sheetLeft -= dt;
                if (runLeft <= 0) EndRun("time");
                else if (sheetTimed && sheetLeft <= 0) Commit(false, -1, 0);
            }
            if (ph == Ph.Practice)
            {
                idleT += dt;
                if (idleT > 8f)
                {
                    ghostScale = idleT > 12f ? 1.7f : 1.45f;
                    if (!fingerT || !fingerT.gameObject.activeSelf) PracticeGhost();
                }
                // 첫 잠금 성공 전에는 '본판으로'를 띄우지 않는다(스킵이 독 타일 입력을 가로채던 사고).
                if (skipG && skipG.alpha > 0f) { skipG.alpha = 0; skipG.blocksRaycasts = false; }
            }
            if (ph == Ph.Title) TitleDemo(dt);
            TickScore(dt);
        }

        void TickScore(float dt)
        {
            if (shownScore < st.score)
            {
                shownScore = Mathf.MoveTowards(shownScore, st.score, Mathf.Max(80f, (st.score - shownScore) * 6f) * dt);
                int v = Mathf.RoundToInt(shownScore);
                if (v != shownScoreInt) { shownScoreInt = v; if (scoreTxt) scoreTxt.text = v.ToString(); JegopSound.Play("counter", 0.35f); }
            }
            if (nGoal > 0 && shownN < nGoal)
            {
                shownN = Mathf.MoveTowards(shownN, nGoal, Mathf.Max(18f, (nGoal - shownN) * 5f) * dt);
                RefreshCountLabel();
            }
        }

        void TitleDemo(float dt)
        {
            titleT += dt;
            jigYaw += dt * 8f;
            if (titleT > 1.4f && slideT <= 0)
            {
                slideT = 1.3f;
                JegopSound.Play("slide", 0.35f);
            }
            if (titleT > 3.4f)
            {
                titleT = 0; slideT = 0;
                SetupSheet(SheetGen.Practice(), true);
            }
        }

        float jigYaw;
        float ghostScale = 1f;

        void HandleInput(float dt)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                if (ph == Ph.Title) { JegopSound.Play("press"); StartRun(true); return; }
                if (ph == Ph.End) { StartRun(true); return; }
            }
            if (Input.GetKeyDown(KeyCode.Delete) && dragTile < 0 && ph == Ph.Play)
            {
                for (int i = 0; i < 3; i++)
                    if (tiles[i] != null && tiles[i].onTray < 0 && !tiles[i].locked) { DiscardTile(i); break; }
            }

            if (MgfPointer.Down)
            {
                st.taps++;
                downScreen = MgfPointer.Position;
                downTime = Time.unscaledTime;
                dragMoved = false;
                dragTile = -1;
                press = HitPress(out pressIdx);
                if (ph == Ph.End) press = Press.EndCta;
                else if (ph == Ph.Title)
                {
                    if (press != Press.Cta) press = Press.Cta;
                    pulseTm = 0.16f;
                }
                else if (press == Press.Tile && pressIdx >= 0)
                {
                    var t = tiles[pressIdx];
                    if (t != null && !t.locked && (ph == Ph.Play || ph == Ph.Practice))
                    {
                        if (ph == Ph.Practice && obStep == 0 && t.area != 9 && t.area != 16 && t.area != 7)
                        {
                            Refuse("9칸 또는 16칸 기왓장을 집어라");
                            PracticeGhost();
                            press = Press.Other;
                        }
                        else
                        {
                            dragTile = pressIdx;
                            t.squash = 1.18f;
                            JegopSound.Play("pick", 0.7f);
                            if (MgfPointer.OnPlane(cam, 0.42f, out var p)) dragOff = t.root.position - p;
                            else dragOff = Vector3.zero;
                        }
                    }
                }
                else if ((press == Press.Other || press == Press.Jig) && selTile < 0) Refuse(MgfPointer.Position);
                MgfBridge.NotifyChanged();
            }

            if (MgfPointer.Held && dragTile >= 0 && tiles[dragTile] != null)
            {
                if ((MgfPointer.Position - downScreen).sqrMagnitude > 80f) dragMoved = true;
                if (MgfPointer.OnPlane(cam, 0.55f, out var p))
                {
                    tiles[dragTile].root.position = p + dragOff + Vector3.up * 0.28f;
                    trailT = 0.18f;
                    HighlightHover(p);
                }
            }
            else if (MgfPointer.Held && press != Press.None)
            {
                if ((MgfPointer.Position - downScreen).sqrMagnitude > 120f) dragMoved = true;
            }

            if (MgfPointer.Up && press != Press.None)
            {
                if (ph == Ph.End) { StartRun(true); press = Press.None; dragTile = -1; return; }
                if (ph == Ph.Title) { JegopSound.Play("press"); StartRun(true); press = Press.None; dragTile = -1; return; }
                if (press == Press.Skip && ph == Ph.Practice) { EndPractice(); press = Press.None; dragTile = -1; return; }

                if (dragTile >= 0)
                {
                    int fromTray = tiles[dragTile] != null ? tiles[dragTile].onTray : -1;
                    if (!dragMoved && fromTray >= 0 && selTile >= 0 && selTile != dragTile
                        && (ph == Ph.Play || ph == Ph.Practice) && !lockedThis)
                    {
                        // 고른 기왓장이 있으면 쟁반 위 판을 탭해도 잠금이 아니라 '그 쟁반에 얹기'다.
                        SnapTile(dragTile);
                        dragTile = -1;
                        press = Press.Latch; pressIdx = fromTray;
                        PlaceSelected();
                    }
                    else if (!dragMoved && fromTray >= 0 && (ph == Ph.Play || ph == Ph.Practice) && !lockedThis)
                    {
                        // 쟁반 위 기왓장을 짧게 탭 = 그 쟁반 나무못 잠금 (못이 장에 가려져도).
                        int ti = dragTile;
                        dragTile = -1;
                        SnapTile(ti);
                        LockTray(fromTray);
                    }
                    else if (!dragMoved && fromTray == -1 && (ph == Ph.Play || ph == Ph.Practice) && !lockedThis)
                    {
                        // 독 기왓장을 짧게 탭 = 고르기. 다음 탭(쟁반·폐기 홈)으로 얹는다.
                        int ti = dragTile;
                        dragTile = -1;
                        selTile = selTile == ti ? -1 : ti;
                        idleT = 0;
                        if (selTile >= 0)
                        {
                            JegopSound.Play("pick", 0.9f);
                            toast(cur != null && cur.kind == Kind.Reverse
                                ? "폐기 홈을 탭하면 이 삼각형을 버린다"
                                : "쟁반을 탭하면 " + tiles[ti].area + "칸 기왓장이 얹힌다", 1.0f);
                            if (ph == Ph.Practice)
                            {
                                if (tiles[ti].area == 7) ShowGhostAt(troughT.position + Vector3.up * 0.4f);
                                else ShowGhostAt(jig.TransformPoint(trayPos[2]) + Vector3.up * 0.4f);
                            }
                        }
                        else if (ph == Ph.Practice) PracticeGhost();
                    }
                    else
                    {
                        DropDrag(dragTile);
                        dragTile = -1;
                    }
                }
                else if (selTile >= 0 && !dragMoved && (ph == Ph.Play || ph == Ph.Practice) && !lockedThis)
                {
                    PlaceSelected();
                }
                else if ((ph == Ph.Play || ph == Ph.Practice) && !lockedThis)
                {
                    if (press == Press.Latch && !dragMoved) LockTray(pressIdx);
                    else if (press == Press.Stamp && !dragMoved) Stamp(pressIdx);
                    else if (press == Press.Discard && !dragMoved && cur != null && cur.kind == Kind.Reverse) DiscardJig();
                }
                ClearHover();
                if (ph == Ph.Practice && selTile >= 0 && tiles[selTile] != null && tiles[selTile].area != 7)
                {
                    // 9·16을 골라 둔 동안 빗변 쟁반에 자리 표시를 띄운다.
                    hoverTray = 2;
                    RefreshTrayLabels();
                }
                press = Press.None;
            }
        }

        Press HitPress(out int idx)
        {
            idx = -1;
            if (ph == Ph.Practice && skipG && skipG.alpha > 0.5f && skipRt
                && RectTransformUtility.RectangleContainsScreenPoint(skipRt, MgfPointer.Position, null))
                return Press.Skip;
            if (ph == Ph.Title && ctaRt
                && RectTransformUtility.RectangleContainsScreenPoint(ctaRt, MgfPointer.Position, null))
                return Press.Cta;
            if (!cam) return Press.Other;
            if (MgfPointer.DownHit(cam, out var hit))
            {
                Transform t = hit.collider.transform;
                string n = t.name;
                for (int k = 0; k < 6 && t != null; k++, t = t.parent)
                {
                    n = t.name;
                    if (n.StartsWith("Tile"))
                    {
                        if (int.TryParse(n.Substring(4), out idx)) return Press.Tile;
                    }
                    if (n.StartsWith("Latch"))
                    {
                        if (int.TryParse(n.Substring(5), out idx)) return Press.Latch;
                    }
                    if (n.StartsWith("Stamp"))
                    {
                        if (int.TryParse(n.Substring(5), out idx)) return Press.Stamp;
                    }
                    if (n.StartsWith("Discard") || n == "Trough") return Press.Discard;
                    if (n.StartsWith("Jig") || n.StartsWith("Tri")) return Press.Jig;
                }
                return Press.Other;
            }
            return Press.Other;
        }

        void PlaceSelected()
        {
            int ti = selTile;
            selTile = -1;
            if (ti < 0 || tiles[ti] == null || tiles[ti].locked) return;
            Vector3 target;
            bool hit = false;
            if (press == Press.Latch && pressIdx >= 0 && pressIdx <= 2)
            {
                target = jig.TransformPoint(trayPos[pressIdx]); hit = true;
            }
            else if (press == Press.Discard)
            {
                target = troughT.position; hit = true;
            }
            else if (MgfPointer.OnPlane(cam, 0.42f, out var p))
            {
                target = p;
                hit = NearestTray(p, 1.35f) >= 0 || InDiscard(p);
            }
            else target = Vector3.zero;
            if (!hit)
            {
                if (ph == Ph.Practice && obStep == 0)
                {
                    // 연습: 빗나간 탭이 고른 기왓장을 내려놓지 않는다 — 목표 쟁반만 다시 가리킨다.
                    selTile = ti;
                    SpawnRipple(MgfPointer.Position);
                    JegopSound.Play("ripple");
                    toast(tiles[ti].area == 7 ? "폐기 홈을 탭하라" : "빛나는 빗변 쟁반을 탭하라", 0.9f);
                    if (tiles[ti].area == 7) ShowGhostAt(troughT.position + Vector3.up * 0.4f);
                    else ShowGhostAt(jig.TransformPoint(trayPos[2]) + Vector3.up * 0.4f);
                    return;
                }
                // 빈 바닥 탭 = 고르기 취소
                SnapTile(ti);
                if (ph == Ph.Practice) PracticeGhost();
                return;
            }
            tiles[ti].root.position = new Vector3(target.x, tiles[ti].root.position.y, target.z);
            DropDrag(ti);
            MgfBridge.NotifyChanged();
        }

        void DropDrag(int ti)
        {
            if (tiles[ti] == null) return;
            Vector3 p = tiles[ti].root.position;
            int tray = NearestTray(p, 1.35f);
            if (tray >= 0 && !trayLocked[tray])
            {
                if (ph == Ph.Practice && obStep == 0)
                {
                    int a = tiles[ti].area;
                    if ((a == 9 || a == 16) && tray != 2)
                    {
                        Refuse("빗변 쟁반에 9와 16을 얹어라");
                        tiles[ti].onTray = -1;
                        SnapTile(ti);
                        PracticeGhost();
                        return;
                    }
                    if (a == 7)
                    {
                        Refuse("7은 길이의 합이다. 폐기 홈으로");
                        tiles[ti].onTray = -1;
                        SnapTile(ti);
                        PracticeGhost();
                        return;
                    }
                }
                PlaceOnTray(ti, tray, true);
                tiles[ti].squash = 0.82f;
                idleT = 0;
                if (ph == Ph.Practice) PracticeGhost();
                return;
            }
            if (InDiscard(p))
            {
                if (ph == Ph.Practice && obStep == 0)
                {
                    if (tiles[ti].area == 7)
                    {
                        DiscardTile(ti);
                        idleT = 0;
                        PracticeGhost();
                        return;
                    }
                    Refuse("9와 16은 빗변 쟁반에 얹어라");
                    tiles[ti].onTray = -1;
                    SnapTile(ti);
                    PracticeGhost();
                    return;
                }
                if (cur != null && cur.kind == Kind.Reverse)
                {
                    tiles[ti].onTray = -1;
                    SnapTile(ti);
                    DiscardJig();
                    return;
                }
                DiscardTile(ti);
                return;
            }
            tiles[ti].onTray = -1;
            SnapTile(ti);
        }

        void Refuse(Vector2 screen)
        {
            refuseAt = screen; refuseT = 0.4f;
            SpawnRipple(screen);
            JegopSound.Play("ripple");
            if (ph == Ph.Practice || ph == Ph.Play)
            {
                PointAtDock();
                if (Time.frameCount != lastRippleFrame)
                {
                    lastRippleFrame = Time.frameCount;
                    toast(ph == Ph.Practice
                        ? "9를 탭하고 빗변 쟁반을 탭하라 (끌어도 된다)"
                        : (cur != null && cur.kind == Kind.Reverse ? "꼭짓점 도장 또는 폐기 홈" : "기왓장을 집어 쟁반에 얹어라"), 0.8f);
                }
            }
        }

        void Refuse(string why)
        {
            JiggleLatch(0);
            JegopSound.Play("refuse");
            toast(why, 0.9f);
        }
    }
}
