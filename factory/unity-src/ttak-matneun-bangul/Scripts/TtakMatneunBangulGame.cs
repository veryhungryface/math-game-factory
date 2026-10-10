// 딱 맞는 방울 — 타이틀 / 연습(고정 판) / 6라운드 구조 / 브리지. 화면은 BangulView·BangulUi(partial).
//
// 유일한 답 입력: 방울 씨앗을 끌어 판 위의 한 점에 놓고 뗀다 → 그 점을 중심으로 방울이 부풀어
// 가장 가까운 벽(안쪽)·못(바깥)에서 멈춘다 → BangulRules.Judge 가 정수로 「세 곳에 모두 닿았나」를 판정.
// 접기 띠(R3~)는 근거 선만 남긴다 — 씨앗을 옮기거나 정답 점을 찍지 않는다.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.TtakMatneunBangul
{
    public partial class TtakMatneunBangulGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int round;              // 1..6 (연습 = 0)
            public int rescued;
            public int firstTryCorrect;
            public int firstTryTotal;
            public int strips;
            public int attempts;
            public int pointerVersion;
            public int lines;
            public string kind = "";       // in | out
            public string shape = "";
            public string prompt = "";
            public string misconceptionId = "";
            public string seq = "";
            public bool repair;
            public bool onboarding;
            public bool frozen;
            public bool mastery;
            public int[] seedBoard = new int[0];   // 마지막으로 놓은 격자점
            public int[] triPx = new int[0];       // A,B,C 화면 픽셀(왼쪽 위 원점)
            public int[] boardPx = new int[0];     // 판 [x0,y0,x1,y1]
            public int[] seedPx = new int[0];      // 씨앗 쉼 자리
            public int[] stripPx = new int[0];     // 접기 띠 두루마리
            public int[] targetPx = new int[0];    // 접기 대상 9개 (x,y)…
            public int screenW, screenH;
        }

        enum Phase { Title, Practice, Play, End }
        enum Seq { None, Demo, Inflate, Good, Bad, Answer, Intro, Morph }
        enum Drag { None, Seed, StripEnd1, StripEnd2 }

        readonly State st = new State();
        readonly List<MgfProblem> bridgeBank = new List<MgfProblem>();
        readonly System.Random seedRng = new System.Random(20261011);

        Phase phase = Phase.Title;
        Seq seq = Seq.None;
        Drag drag = Drag.None;
        float seqT, seqDur;
        List<Board> deck;
        Board board;
        int roundIdx = -1, runSerial, combo;
        bool firstAttempt = true, practiceDone, lastOk, skipPracticeNextTime;
        int practiceFails;
        float timeLeft, idleT, hitStop, pressT;
        IP placed;
        Verdict lastVerdict;
        int stripPinned = -1;                // 첫 끝을 포갠 대상
        readonly List<int> lineKinds = new List<int>();   // kind*10 + which
        readonly int[] missCount = new int[8];
        readonly List<string> missIds = new List<string>();
        bool selfTest;

        void Awake()
        {
            MgfLook.Quality(90f);
            selfTest = Application.absoluteURL != null && Application.absoluteURL.Contains("selftest=1");
            coverMode = Application.absoluteURL != null && Application.absoluteURL.Contains("cover=1");
            var bank = BangulRules.BuildBank();
            for (int i = 0; i < bank.Count; i++)
            {
                var it = bank[i];
                bridgeBank.Add(new MgfProblem { id = it.id, prompt = it.prompt, choices = it.choices, answer = it.answer, answerNumeric = it.numeric, unitConcept = it.concept });
            }
            BuildWorld();
            BuildUi();
            Prewarm();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Start()
        {
            if (selfTest)
            {
                // ArtSource/validation/bot-selftest.mjs 가 이 줄을 받아 저장한다.
                Debug.Log("BANGUL_SELFTEST " + BangulBots.Run(200, 4242));
            }
        }

        void Prewarm()
        {
            var sb = new StringBuilder("딱맞는방울방울을틀에꼭맞춰라키워친구를태워중학교2학년삼각형의성질내심과외심씨앗을눌러시작최고첫시도구조완료숙련달성기준미달중단다시");
            sb.Append("세변에닿도록놓으시오꼭짓점동시에까지거리가같다→내심외심IO이등분선수직이등분선꼭지각밑각빗변중점둔각직각바깥안쪽부의접기띠포개대상표식");
            sb.Append("먼저닿은벽못에서멀어지게옮기한번더수리잠시기다리판위끌어삼각형의내부에이미접은선이다두개또는각의같은거리시간하트남은루루김서림기록");
            sb.Append("0123456789°∠△½+−=×÷·…♥♡※ABCDOIG cm다음실전마리연습점수으로까지는을를이가에서와과의로도만큼할때인고며면게지기");
            for (int i = 0; i < bridgeBank.Count; i++) sb.Append(bridgeBank[i].prompt);
            MgfText.Prewarm(sb.ToString());
        }

        // ───────────────────────────── 루프
        void Update()
        {
            float dt = Mathf.Min(.05f, Time.deltaTime);
            ApplyLayout(dt);
            HandleInput();
            if (hitStop > 0f) { hitStop -= dt; UpdateVisuals(0f); return; }

            if (seq != Seq.None)
            {
                seqT += dt;
                if (seqT >= seqDur) SeqDone();
            }
            if (phase == Phase.Play && (seq == Seq.None || seq == Seq.Inflate))
            {
                timeLeft -= dt;
                if (timeLeft <= 0f) { timeLeft = 0f; EndRun("time"); }
            }
            if ((phase == Phase.Practice || phase == Phase.Play) && seq == Seq.None && drag == Drag.None)
            {
                idleT += dt;
                if (idleT > 8f) { idleT = 0f; ReplayGuide(true); }
            }
            UpdateVisuals(dt);
        }

        // ───────────────────────────── 흐름
        void ShowTitle()
        {
            phase = Phase.Title; seq = Seq.None; drag = Drag.None;
            board = null; deck = null; roundIdx = -1;
            st.score = 0; st.lives = BangulRules.StartHearts; st.level = 1; st.solved = 0; st.round = 0;
            st.rescued = 0; st.firstTryCorrect = 0; st.firstTryTotal = 0; st.strips = BangulRules.StartStrips;
            st.onboarding = false; st.frozen = true; st.prompt = ""; st.kind = ""; st.shape = "";
            timeLeft = BangulRules.RunSeconds;
            SetScreen();
            Notify();
        }

        void BeginPractice(bool holding)
        {
            runSerial++;
            phase = Phase.Practice;
            practiceFails = 0;
            ResetRunStats();
            st.onboarding = true; st.frozen = true;
            board = BangulRules.Practice();
            roundIdx = -1;
            lineKinds.Clear();
            BindBoard(true);
            SetScreen();
            ShowGoal("방울을 키워 친구를 태워라");
            if (holding) { drag = Drag.Seed; StartSeedDrag(); }
            else StartSeq(Seq.Demo, 2.8f);
            Sfx.Play("whoosh", .25f);
            Notify();
        }

        void ResetRunStats()
        {
            st.score = 0; st.lives = BangulRules.StartHearts; st.level = 1; st.solved = 0; st.round = 0;
            st.rescued = 0; st.firstTryCorrect = 0; st.firstTryTotal = 0; st.strips = BangulRules.StartStrips;
            st.attempts = 0; st.repair = false; st.misconceptionId = ""; st.mastery = false;
            combo = 0; timeLeft = BangulRules.RunSeconds;
            for (int i = 0; i < missCount.Length; i++) missCount[i] = 0;
            missIds.Clear();
            ResetCreatures();
        }

        void BeginRun(bool fromHook)
        {
            runSerial++;
            var rng = new System.Random(unchecked(seedRng.Next() ^ Environment.TickCount ^ runSerial * 104729));
            deck = BangulRules.RunDeck(rng);
            phase = Phase.Play;
            ResetRunStats();
            st.onboarding = false; st.frozen = false;
            lineKinds.Clear();
            drag = Drag.None; stripPinned = -1;
            seq = Seq.None;
            SetScreen();
            LoadRound(0, fromHook);
            Notify();
        }

        void LoadRound(int i, bool instant)
        {
            roundIdx = i;
            var prev = board;
            board = deck[i];
            bool sameTriangle = prev != null && i == 5 && prev.round == 4;
            if (!sameTriangle) lineKinds.Clear();
            firstAttempt = true;
            st.attempts = 0; st.repair = false; st.misconceptionId = "";
            st.round = i + 1; st.level = board.band;
            BindBoard(!sameTriangle);
            ShowCreatureWaiting(i);
            ShowGoal(null);
            revealRt.gameObject.SetActive(false); revealLeft = 0f; revealPending = -1f;
            toastLeft = 0f; toastRt.gameObject.SetActive(false);
            stripRoot.gameObject.SetActive(board.strips);
            if (instant) { seq = Seq.None; FinishIntroVisuals(); }
            else StartSeq(sameTriangle ? Seq.Morph : Seq.Intro, sameTriangle ? .8f : .7f);
            idleT = 0f;
        }

        void BindBoard(bool rebuildFrame)
        {
            st.kind = board.kind == Kind.In ? "in" : "out";
            st.shape = board.shape.ToString().ToLowerInvariant();
            st.prompt = board.Instruction;
            st.lines = lineKinds.Count;
            ApplyBoardVisuals(rebuildFrame);
            RefreshPromptUi();
            RefreshHud();
            UpdateLayoutState();
        }

        void StartSeq(Seq s, float dur)
        {
            if (s == Seq.Demo) { demoBlocked = false; demoPopped = false; }
            seq = s; seqT = 0f; seqDur = dur;
            st.seq = s == Seq.None ? "" : s.ToString().ToLowerInvariant();
            OnSeqStart(s);
        }

        void SeqDone()
        {
            var s = seq;
            seq = Seq.None; st.seq = "";
            switch (s)
            {
                case Seq.Demo: OnDemoDone(); break;
                case Seq.Inflate: Resolve(lastVerdict); break;
                case Seq.Good: AfterGood(); break;
                case Seq.Bad: AfterBad(); break;
                case Seq.Answer: AfterAnswer(); break;
                case Seq.Intro: case Seq.Morph: FinishIntroVisuals(); break;
            }
            Notify();
        }

        /// <summary>테스트 훅이 연출을 기다리지 않고 다음 상태로 넘긴다(같은 SeqDone 경로).</summary>
        void FlushSeq()
        {
            for (int guard = 0; guard < 8 && seq != Seq.None; guard++) SeqDone();
            hitStop = 0f;
        }

        // ───────────────────────────── 답 입력: 씨앗 놓기
        void Place(IP p, bool instant)
        {
            if (board == null) return;
            var v = BangulRules.Judge(board, p);
            placed = p;
            st.seedBoard = new[] { p.x, p.y };
            if (v.refused)
            {
                // 안쪽 방울을 삼각형 밖에 놓음(또는 판 밖) — 막이 바로 꺼지고 시도로 세지 않는다
                PopSeedAt(p);
                Refuse(board.kind == Kind.In && BangulRules.OnBoard(p) ? "삼각형의 내부에 놓으시오" : "판 위에 놓으시오", true);
                Notify();
                return;
            }
            lastVerdict = v;
            st.onboarding = false;
            st.frozen = false;
            idleT = 0f;
            HideGuide();
            instantBubble = instant;
            BeginBubble(p, v);
            if (instant) { seq = Seq.None; Resolve(v); }
            else StartSeq(Seq.Inflate, .5f);
            Notify();
        }

        void Resolve(Verdict v)
        {
            bool practice = phase == Phase.Practice;
            st.attempts++;
            if (!practice && firstAttempt)
            {
                st.firstTryTotal++;
                if (v.ok) st.firstTryCorrect++;
            }
            lastOk = v.ok;
            if (v.ok)
            {
                int gain = practice ? 10 : firstAttempt ? 100 + 25 * combo : 40;
                st.score += gain;
                if (!practice)
                {
                    st.rescued++; st.solved = st.rescued;
                    combo = firstAttempt ? combo + 1 : 0;
                }
                st.misconceptionId = "";
                hitStop = .09f;
                zoomPunch = 1f; zoomFocus = G2S(placed);
                StartSeq(Seq.Good, 2.5f);
                Sfx.Play("harp", .32f);
            }
            else
            {
                string mis = BangulRules.Misconception(board, placed);
                st.misconceptionId = mis;
                combo = 0;
                if (!practice)
                {
                    st.lives--;
                    missIds.Add(mis);
                }
                else practiceFails++;
                StartSeq(Seq.Bad, 1.35f);
                Sfx.Play("rub", .4f);
            }
            RefreshHud();
            Notify();
        }

        void AfterGood()
        {
            SettleRescue();
            if (phase == Phase.Practice)
            {
                practiceDone = true;
                skipPracticeNextTime = true;
                ShowGoal("다음: 실전 — 비누 친구 6마리를 구조하라");
                BeginRun(false);
                return;
            }
            if (roundIdx + 1 >= deck.Count) { EndRun("done"); return; }
            LoadRound(roundIdx + 1, false);
        }

        void AfterBad()
        {
            ClearBubble();
            if (phase == Phase.Practice)
            {
                if (practiceFails >= 2) { StartSeq(Seq.Answer, 1.3f); return; }
                ShowToast("먼저 닿은 벽에서 멀어지게 옮겨 다시 놓으시오", 3f);
                ReturnSeed();
                return;
            }
            if (st.lives <= 0) { EndRun("hearts"); return; }
            if (firstAttempt)
            {
                firstAttempt = false;
                st.repair = true;
                ShowToast(board.kind == Kind.In ? "수리 1회 · 먼저 닿은 벽에서 멀어지게 옮기시오" : "수리 1회 · 가까운 못에서 멀어지게 옮기시오", 3f);
                ReturnSeed();
                return;
            }
            StartSeq(Seq.Answer, 1.3f);   // 수리도 실패 → 정답 상태를 보여 주고 넘어간다
        }

        void AfterAnswer()
        {
            if (phase == Phase.Practice)
            {
                ShowGoal("다음: 실전 — 비누 친구 6마리를 구조하라");
                skipPracticeNextTime = true;
                BeginRun(false);
                return;
            }
            LoseCreature(roundIdx);
            if (roundIdx + 1 >= deck.Count) { EndRun("done"); return; }
            LoadRound(roundIdx + 1, false);
        }

        bool endDone;

        void EndRun(string reason)
        {
            bool done = reason == "done";
            endDone = done;
            phase = Phase.End;
            seq = Seq.None; st.seq = ""; drag = Drag.None;
            st.mastery = done && st.firstTryCorrect >= BangulRules.MasteryFirst;
            st.frozen = true;
            SaveBest();
            SetScreen();
            ShowEnd(reason);
            Sfx.Play(done ? "win" : "lose", .4f);
            Notify();
        }

        void SaveBest()
        {
            try
            {
                if (st.firstTryCorrect > PlayerPrefs.GetInt("ttak.bestFirst", 0)) PlayerPrefs.SetInt("ttak.bestFirst", st.firstTryCorrect);
                if (st.score > PlayerPrefs.GetInt("ttak.bestScore", 0)) PlayerPrefs.SetInt("ttak.bestScore", st.score);
                PlayerPrefs.Save();
            }
            catch (Exception) { }
        }

        // ───────────────────────────── 입력
        void HandleInput()
        {
            Vector2 sp = MgfPointer.Position;
            // 눌림과 뗌이 같은 프레임에 올 수 있다(빠른 탭) — 눌림 처리 뒤에도 뗌을 반드시 처리한다.
            if (MgfPointer.Down) OnPointerDown(sp);
            if (MgfPointer.Held && !MgfPointer.Down)
            {
                pressT += Time.deltaTime;
                if (drag == Drag.Seed) MoveSeedDrag(sp);
                else if (drag == Drag.StripEnd1 || drag == Drag.StripEnd2) StripFollow(sp);
            }
            if (MgfPointer.Up) OnPointerUp(sp);
        }

        void OnPointerDown(Vector2 sp)
        {
            {
                st.pointerVersion++;
                pressT = 0f;
                idleT = 0f;
                if (phase == Phase.Title)
                {
                    TitleTouch(sp);
                    BeginPractice(true);
                    return;
                }
                if (phase == Phase.End)
                {
                    if (InReplay(sp)) { PressReplay(); skipPracticeNextTime = true; BeginRun(false); }
                    else { Ripple(sp); ShowToast("방울 씨앗을 누르면 다시 구조한다", 1.6f); PointAtReplay(); Notify(); }
                    return;
                }
                if (seq == Seq.Demo) { FlushDemo(); }
                if (seq != Seq.None)
                {
                    Ripple(sp);
                    ShowToast("방울이 움직이는 동안 잠시 기다리시오", 1.2f);
                    Notify();
                    return;
                }
                // 접기 띠
                if (board != null && board.strips && phase == Phase.Play)
                {
                    if (stripPinned >= 0) { drag = Drag.StripEnd2; StripFollow(sp); Notify(); return; }
                    if (NearStripRoll(sp))
                    {
                        if (st.strips <= 0) { RefuseStrip("접기 띠를 모두 썼다"); return; }
                        drag = Drag.StripEnd1; StripFollow(sp); Sfx.Play("fold", .18f); Notify(); return;
                    }
                }
                if (NearSeed(sp) || InBoard(sp))
                {
                    drag = Drag.Seed;
                    StartSeedDrag();
                    Notify();
                    return;
                }
                Ripple(sp);
                ShowToast("방울 씨앗을 끌어 판 위에 놓으시오", 1.8f);
                PointAtSeed();
                Sfx.Play("refuse", .12f);
                Notify();
                return;
            }
        }

        void OnPointerUp(Vector2 sp)
        {
            {
                if (drag == Drag.Seed && titleGrab && (pressT < .3f || !InBoard(sp)))
                {
                    // 타이틀에서 톡 친 것뿐이면 놓기로 세지 않고 시연을 보여 준다
                    titleGrab = false;
                    drag = Drag.None;
                    EndSeedDrag(false);
                    ReturnSeedInstant();
                    StartSeq(Seq.Demo, 2.8f);
                    Notify();
                }
                else if (drag == Drag.Seed)
                {
                    titleGrab = false;
                    drag = Drag.None;
                    IP p;
                    if (SeedBoardPoint(sp, out p)) { EndSeedDrag(true); Place(p, false); }
                    else { EndSeedDrag(false); ReturnSeed(); ShowToast("판 위에 놓으시오", 1.6f); Sfx.Play("refuse", .1f); }
                    Notify();
                }
                else if (drag == Drag.StripEnd1)
                {
                    drag = Drag.None;
                    int t = TargetAt(sp);
                    if (t >= 0)
                    {
                        stripPinned = t;
                        PinStrip(t);
                        ShowToast("다른 쪽 끝을 두 번째 대상에 포개시오", 2.6f);
                        Sfx.Play("tok", .2f);
                    }
                    else RefuseStrip("띠 끝을 꼭짓점이나 같은 거리 표식에 포개시오");
                    Notify();
                }
                else if (drag == Drag.StripEnd2)
                {
                    drag = Drag.None;
                    int t = TargetAt(sp);
                    int t1 = stripPinned;
                    stripPinned = -1;
                    LineKind lk; int which;
                    if (t < 0) RefuseStrip("띠 끝을 꼭짓점이나 같은 거리 표식에 포개시오");
                    else if (!BangulRules.FoldPair(t1, t, out lk, out which)) RefuseStrip("꼭짓점 두 개, 또는 한 각의 같은 거리 표식 두 개를 포개시오");
                    else if (lineKinds.Contains((int)lk * 10 + which)) RefuseStrip("이미 접은 선이다");
                    else
                    {
                        st.strips--;
                        lineKinds.Add((int)lk * 10 + which);
                        st.lines = lineKinds.Count;
                        AddFoldLine(lk, which, t1, t);
                        ShowToast(lk == LineKind.PerpBisector ? BangulRules.SideNames[which] + "의 수직이등분선" : "∠" + BangulRules.Names[which] + "의 이등분선", 2.2f);
                        Sfx.Play("fold", .3f);
                        RefreshHud();
                    }
                    Notify();
                }
            }
        }

        void RefuseStrip(string msg)
        {
            stripPinned = -1;
            ReturnStrip();
            ShowToast(msg, 2.4f);
            Sfx.Play("refuse", .14f);
            Notify();
        }

        void Refuse(string msg, bool sound)
        {
            ShowToast(msg, 2.2f);
            if (sound) Sfx.Play("pop", .25f);
        }

        void Notify()
        {
            st.phase = phase == Phase.Title ? "title" : phase == Phase.Practice ? "practice" : phase == Phase.Play ? "playing"
                : (endDone ? "clear" : "gameover");
            MgfBridge.NotifyChanged();
        }

        // ───────────────────────────── 테스트 훅(실제 정답·오답 경로와 같은 Place 를 탄다)
        public void TestStart() => BeginRun(true);

        public void TestAnswerCorrect()
        {
            if (phase != Phase.Play) BeginRun(true);
            FlushSeq();
            if (phase != Phase.Play) BeginRun(true);
            drag = Drag.None;
            Place(board.hit, true);
        }

        public void TestAnswerWrong()
        {
            if (phase != Phase.Play) BeginRun(true);
            FlushSeq();
            if (phase != Phase.Play) BeginRun(true);
            drag = Drag.None;
            Place(board.miss, true);
        }

        public string StateJson() => JsonUtility.ToJson(st);
        public string ProblemBankJson() => MgfJson.Bank(bridgeBank);
    }
}
