// 유리칼 — 형광 재단 매트 위 두 대각선 막대의 교점 O 를 밀어 이등분·수직·같은 길이를 맞춘 뒤,
// 아크릴 장을 위쪽 브레이커로 밀어 십자로 쪼개거나(확정) 아래 고철 통으로 치운다(성립 불가 판정). 90초 아케이드.
//
// 답 입력 경로는 두 개뿐이다: (1) 점 O(유리칼 머리)·막대 끝 고리를 끌어 모양을 만든다  (2) 장을 위/아래로 민다.
// 판정은 YuriRules.cs 의 Judge(정수 mm·정수 도)뿐이다. 코드는 O 를 중점으로 스냅하거나 교각을 90°로 맞춰 주지 않는다.
// 훅(TestAnswerCorrect/Wrong)도 같은 Commit() 을 탄다.
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;

namespace Mgf.YuriKal
{
    public partial class YuriKalGame : MonoBehaviour, IMgfGame
    {
        [System.Serializable]
        class State : MgfState
        {
            public int sheet, of = 12, spares = 3, combo, maxCombo, firstTry, attempts, processed, touches;
            public int ao, co, bo, dO, ac, bd, angle;
            public string order = "", lockKind = "", misconception = "";
            public bool onboarding, locked;
        }

        readonly State st = new State();
        enum Ph { Title, Practice, Play, End }
        enum Sp { Enter, Work, Split, ScrapGood, Wrong, Timeout, Exit }
        Ph ph = Ph.Title;
        Sp sp = Sp.Enter;
        float spT;

        Sheet cur;
        List<Sheet> deck;
        int deckIdx = -1;
        System.Random rng = new System.Random(20260929);
        readonly List<MgfProblem> bank = new List<MgfProblem>();

        bool tried, willRetry, bisectLocked, rotHintShown, practiceSeen;
        Act lastAct;
        float runLeft, sheetLeft, cardT, idleT, hitStop, frostMatT, toastT, toastDur, caliperT, arrowT, jiggleT, trailT2, swingAmp;
        bool frozen;
        int obStep;               // 연습: 0 = O 를 옮겨라 · 1 = 위로 밀어라
        float ghostScale = 1f, ghostT;
        Vector2 sheetCenter, arrowFrom;
        float enterX, pushVis, dropZ, sep, sheetScale = 1f, frostK;
        float frostShiftNext;
        float shownScore; int shownScoreInt = -1, lastTimeInt = -1;
        int best, bestScore, bestCombo;
        string endReason = "";
        float endT;

        // 입력
        enum Press { None, O, Rot, Sheet, Cta, EndCta }
        Press press;
        Vector2 downScreen, grabOff;
        float downTime, thetaVis;
        bool movedO;

        // 타이틀 데모
        float titleT, logoT;
        int demoLoop;

        // ─────────────────────────────── 부팅
        void Awake()
        {
            YuriSound.Init(gameObject);
            BuildBank();
            BuildWorld();
            BuildUi();
            MgfText.Prewarm("유리칼교점을밀어쪼개라시작하기중학교2학년사각형의성질최고점수장콤보연속주문처리첫시도정답예비아크릴고철통브레이커이제위로장을밀어쪼개시오점O를이등분하시오지금은아직이아니다될수있었다두중점턱이겹치는곳으로그리고평행사변형직사각형마름모정사각형가가이되게면고아니면치우시오넓이인한쌍의대변이평행하고그길이가같다대각의크기가각각같다대각선이서로다른것을이등분한다수직이등분이면정사각형도직사각형이다마름모도평행사변형이다등변사다리꼴쌍은잠김안되면아래로쪼개기치우기막대를톡치면캘리퍼로잰다길이숫자가사라진다끝고리를돌려맞추시오연습끝초시간종료다시하기처리한첫시도최장소진모두했다전체의넓이같음수직이기만하면마름모가아니다평행한쌍은길이가같은쌍은그쌍이아니다치웠어야쪼갰어야이니이므로↑↓·∥⊥≠²∠△□=,.:0123456789ABCDO°/cm×→「」");
            best = PlayerPrefs.GetInt("yurikal.best", 0);
            bestScore = PlayerPrefs.GetInt("yurikal.bestScore", 0);
            bestCombo = PlayerPrefs.GetInt("yurikal.bestCombo", 0);
            ShowTitle();
            MgfBridge.Register(this);
        }

        // ── 문제 은행: 게임 판과 같은 SheetGen.Deck() 에서 뽑고, 같은 Words.Prompt(= 티켓 문장)·Judge 로 정답을 만든다.
        void BuildBank()
        {
            var seen = new HashSet<string>();
            var g = new SheetGen(new System.Random(777));
            Add(SheetGen.Practice(), seen);
            for (int k = 0; k < 200 && bank.Count < 360; k++)
                foreach (var s in g.Deck()) { if (bank.Count >= 360) break; Add(s, seen); }
        }

        void Add(Sheet s, HashSet<string> seen)
        {
            string p = Words.Prompt(s);
            if (!seen.Add(p)) return;
            bool scrap = Judge.RightAct(s) == Act.Scrap;
            bank.Add(new MgfProblem
            {
                id = "yk" + (bank.Count + 1),
                prompt = p,
                choices = null,
                answer = Words.Answer(s),
                answerNumeric = (!scrap && !s.Locked) ? s.L1 / 2 / 10.0 : double.NaN,   // 구성 장: 옮긴 뒤 AO(cm)
                unitConcept = Words.Concept(s)
            });
        }

        // ─────────────────────────────── IMgfGame
        public void TestStart() { StartRun(false); }

        public void TestAnswerCorrect()
        {
            if (ph == Ph.Title || ph == Ph.End) StartRun(false);
            if (ph == Ph.Practice) { EndPractice(); }
            FinishPending();
            var a = Judge.RightAct(cur);
            if (a == Act.Split && !cur.Locked) { Judge.SolveInto(cur); Recompute(cur); oVis = Ol; thetaVis = cur.theta; RefreshSheetVisual(true); }
            Commit(a);
        }

        public void TestAnswerWrong()
        {
            if (ph == Ph.Title || ph == Ph.End) StartRun(false);
            if (ph == Ph.Practice) { EndPractice(); }
            FinishPending();
            Commit(Judge.Correct(cur, Act.Split) ? Act.Scrap : Act.Split);
        }

        public string StateJson()
        {
            st.phase = ph == Ph.Title ? "title" : ph == Ph.End ? (endReason == "clear" ? "clear" : "gameover") : "playing";
            st.onboarding = ph == Ph.Practice;
            st.lives = st.spares;
            if (cur != null && ph != Ph.Title)
            {
                st.sheet = cur.no; st.ao = cur.s1; st.co = cur.CO; st.bo = cur.s2; st.dO = cur.DO; st.ac = cur.L1; st.bd = cur.L2; st.angle = cur.theta;
                st.order = cur.order.ToString(); st.lockKind = cur.lk.ToString(); st.locked = cur.Locked; st.misconception = cur.misconceptionId;
                st.level = cur.stage;
            }
            return JsonUtility.ToJson(st);
        }

        public string ProblemBankJson() => MgfJson.Bank(bank);

        // ─────────────────────────────── 판 흐름
        void ShowTitle()
        {
            ph = Ph.Title;
            titleT = 0; logoT = 0; demoLoop = 0;
            for (int i = 0; i < 3; i++) logo[i].rectTransform.localScale = Vector3.zero;
            bestTxt.text = bestScore > 0 ? "최고 점수 " + bestScore + " · 최장 콤보 " + bestCombo + " · 처리 " + best + "장" : "한 판 90초 · 주문 12장";
            NewDemo();
            SetVisible();
        }

        void NewDemo()
        {
            var s = new Sheet { no = 0, order = Order.Para, L1 = 100, L2 = 160, theta = 150, phi = 15 + (demoLoop % 3) * 45, stage = 1 };
            s.startS1 = 25; s.startS2 = 110; s.startTheta = 150;
            cur = s; SetupSheet(s, true);
        }

        void StartRun(bool withPractice)
        {
            st.score = 0; st.solved = 0; st.combo = 0; st.maxCombo = 0; st.firstTry = 0; st.attempts = 0; st.processed = 0;
            st.spares = Rules.Spares; st.lives = st.spares;
            shownScore = 0; shownScoreInt = -1;
            runLeft = Rules.RunSec; frozen = false; cardT = 0; frostShiftNext = 0; endReason = "";
            for (int i = 0; i < 3; i++) { slab[i].localRotation = Quaternion.identity; slabCrack[i].enabled = false; }
            deck = new SheetGen(rng).Deck();
            deckIdx = -1;
            rotHintShown = false;
            HideToast();
            if (withPractice)
            {
                ph = Ph.Practice; obStep = 0; idleT = 0; ghostScale = 1f; ghostT = 0;
                cur = SheetGen.Practice();
                SetupSheet(cur, false);
            }
            else
            {
                ph = Ph.Play;
                NextSheet();
            }
            SetVisible();
            MgfBridge.NotifyChanged();
        }

        void EndPractice()
        {
            practiceSeen = true;
            ph = Ph.Play;
            cardG.alpha = 0; cardT = 0; frozen = false;
            deckIdx = -1;
            NextSheet();
            SetVisible();
        }

        void NextSheet()
        {
            deckIdx++;
            if (deckIdx >= deck.Count) { EndRun("clear"); return; }
            cur = deck[deckIdx];
            SetupSheet(cur, false);
            if (cur.no == 5) ShowCard("이제 길이 숫자가 사라진다", "중점 턱과 1 cm 눈금을 보고 맞추시오\n막대를 톡 치면 캘리퍼로 잰다", 2.4f);
            MgfBridge.NotifyChanged();
        }

        void SetupSheet(Sheet s, bool demo)
        {
            s.Reset();
            tried = false; willRetry = false; bisectLocked = false;
            float fx = demo ? 0 : frostShiftNext;
            sheetCenter = demo ? new Vector2(0, 0.3f) : new Vector2(fx + Rand(-1.2f, 1.2f), Rand(-1.0f, 0.8f));
            frostShiftNext = 0;
            InitFrame(s);
            thetaVis = s.theta;
            SnapCounters(s);
            sp = Sp.Enter; spT = 0; enterX = 30f; pushVis = 0; dropZ = 0; sep = 0; sheetScale = 1f; frostK = 0; swingAmp = 0;
            frostR.enabled = false;
            glowR.enabled = false;
            for (int i = 0; i < 4; i++) areaLbl[i].gameObject.SetActive(false);
            caliperLbl.gameObject.SetActive(false);
            sheetLeft = s.limitSec;
            RefreshSheetVisual(true);
            headRoot.gameObject.SetActive(!s.Locked);
            rivet.gameObject.SetActive(s.Locked);
            rotHandle.gameObject.SetActive(s.canRotate);
            pulseT.gameObject.SetActive(false);
            if (!demo)
            {
                ticketHead.text = s.no == 0 ? "연습 · 시간이 흐르지 않는다" : "주문 " + s.no.ToString("00") + " / 12" + (s.stage == 1 ? "" : " · 길이 숫자 없음");
                rawOrder = s.no == 0 ? "□ABCD가 평행사변형이 되게 하시오." : Words.OrderLine(s);
                orderTxt.text = Wrap(rawOrder, ticketTextW, orderTxt.fontSize * 1.02f);
                SetDataLine(s);
                footTxt.text = s.no == 0 ? "↑ 이등분했으면 장을 위로 밀어 쪼갠다" : Words.FootLine(s);
                string hint = "";
                if (s.no == 0) hint = "점 O를 밀어 이등분하시오";
                else if (s.canRotate && !rotHintShown) { hint = "주황 고리를 돌려 ∠AOB를 맞추시오"; rotHintShown = true; }
                else if (s.no == 5) hint = "막대를 톡 치면 캘리퍼로 잰다";
                SetHint(hint);
                MgfFx.Punch(ticketRt, 0.05f, 0.25f);
                YuriSound.Play("slide", 0.6f);
            }
        }

        float Rand(float a, float b) => a + (float)rng.NextDouble() * (b - a);

        /// <summary>티켓 둘째 줄: 주어진 것 + (구성 장) 지금 교각 ∠AOB — 교각은 막대 위에 두면 길이 숫자와 겹친다.</summary>
        void SetDataLine(Sheet s)
        {
            string t = Words.DataLine(s);
            if (!s.Locked) t += (land ? "\n" : "  ·  ") + "∠AOB = " + s.theta + "°";
            t = Wrap(t, ticketTextW, 15f);
            if (t != dataTxt.text) dataTxt.text = t;
        }

        string rawOrder = "", rawHint = "";
        /// <summary>판형이 바뀌면(세로↔가로) 티켓 글을 새 폭으로 다시 줄바꿈한다.</summary>
        void ReflowTicket()
        {
            if (cur == null || ph == Ph.Title) return;
            orderTxt.text = Wrap(rawOrder, ticketTextW, orderTxt.fontSize * 1.02f);
            dataTxt.text = ""; SetDataLine(cur);
            hintTxt.text = Wrap(rawHint, ticketTextW, 14f);
        }

        void SetHint(string h)
        {
            rawHint = h;
            hintTxt.text = Wrap(h, ticketTextW, 14f);
            hintBg.enabled = h.Length > 0;
        }

        bool Numbers(Sheet s) => s.stage == 1;

        void RefreshSheetVisual(bool full)
        {
            Recompute(cur);
            RebuildPieces();
            RebuildInk(cur, Numbers(cur) && ph != Ph.Title || ph == Ph.Title);
            PlaceLabels(cur, Numbers(cur), false);
            var hp = new Vector3(Ol.x, TopY, Ol.y);
            if (full) headRoot.localPosition = hp;
            rivet.localPosition = new Vector3(Ol.x, TopY + 0.02f, Ol.y);
            rotHandle.localPosition = new Vector3(Dl.x, TopY, Dl.y);
            frostWeb.localPosition = new Vector3(Ol.x, TopY + 0.05f, Ol.y);
        }

        // ── 확정: 쪼개기(위) / 치우기(아래). 학생 입력과 훅이 같은 함수를 탄다.
        void Commit(Act a)
        {
            if (sp != Sp.Work && sp != Sp.Enter) return;
            if (sp == Sp.Enter) { sp = Sp.Work; enterX = 0; }
            if (ph == Ph.Practice)
            {
                if (a == Act.Scrap) { Refuse("지금은 점 O를 밀어 이등분하시오"); return; }
                if (!Judge.Satisfies(cur)) { BounceBack(); return; }
                lastAct = a; StartSplit();
                return;
            }
            bool ok = Judge.Correct(cur, a);
            bool first = !tried; tried = true;
            if (first) st.attempts++;
            lastAct = a;
            if (ok)
            {
                st.combo++; st.maxCombo = Mathf.Max(st.maxCombo, st.combo);
                int mult = Rules.Mult(st.combo);
                st.score += 100 * mult + (first ? 50 : 0);
                if (first) st.firstTry++;
                st.solved++; st.processed++;
                if (mult > 1 && (st.combo == 3 || st.combo == 5)) YuriSound.Play("combo", 0.8f);
                if (a == Act.Split) StartSplit(); else StartScrapGood();
            }
            else
            {
                st.combo = 0;
                LoseSpare();
                willRetry = first && st.spares > 0;
                if (!willRetry) st.processed++;
                sp = Sp.Wrong; spT = 0;
                YuriSound.Play("frost", 1f);
                ShowToast(Words.RevealWrong(cur, a), 2.0f);
                frostShiftNext = 2.2f; frostMatT = 0;
            }
            MgfBridge.NotifyChanged();
        }

        void LoseSpare()
        {
            int i = Rules.Spares - st.spares;
            st.spares = Mathf.Max(0, st.spares - 1); st.lives = st.spares;
            if (i >= 0 && i < 3) { slabCrack[i].enabled = true; slab[i].localRotation = Quaternion.Euler(0, 0, -16); MgfFx.Punch(slab[i], 0.25f, 0.3f); }
        }

        void StartSplit()
        {
            sp = Sp.Split; spT = 0;
            // 넓이 라벨(이등분일 때 네 조각이 같다 — sin θ 가 유리수면 숫자, 아니면 1/4)
            string tri = Judge.TriAreaText(cur);
            for (int i = 0; i < 4; i++) { areaLbl[i].text = tri != null ? tri + " cm²" : "1/4"; areaLbl[i].fontSize = tri != null ? 11f : 13f; areaLbl[i].gameObject.SetActive(false); }
            areaTarget = tri;
            if (ph != Ph.Title) ShowToast(ph == Ph.Practice ? "AO = CO = 4 cm · BO = DO = 6 cm\n두 대각선이 서로 다른 것을 이등분한다" : Words.RevealRight(cur, Act.Split), ph == Ph.Practice ? 2.4f : 1.9f);
            if (ph == Ph.Practice) SetHint("");
        }
        string areaTarget;

        void StartScrapGood()
        {
            sp = Sp.ScrapGood; spT = 0;
            YuriSound.Play("clunk", 1f);
            ShowToast(Words.RevealRight(cur, Act.Scrap), 1.9f);
        }

        void BounceBack()
        {
            // 연습 중 이등분 전에 밀었다: 장이 브레이커에서 미끄러져 돌아오고 길이 숫자가 0.8초 강조된다
            pushVis = 2.2f;
            YuriSound.Play("refuse", 0.8f);
            for (int i = 0; i < 4; i++) MgfFx.Punch(lenLbl[i].transform, 0.35f, 0.8f);
            ShowToast("AO = " + Words.Cm(cur.s1) + " cm, CO = " + Words.Cm(cur.CO) + " cm · 아직 이등분이 아니다", 1.6f);
        }

        void Refuse(string why)
        {
            jiggleT = 0.25f;
            YuriSound.Play("refuse", 0.8f);
            ShowToast(why, 1.4f);
            ghostT = 0; ghostScale = 1.4f;
        }

        void ShowCard(string big, string sub, float dur)
        {
            cardBig.text = big; cardSub.text = sub; cardT = dur; frozen = true;
            cardG.alpha = 1; MgfFx.Punch(cardRt, 0.06f, 0.3f);
        }

        void ShowToast(string s, float dur) { toastTxt.text = s; toastT = 0; toastDur = dur; toastG.alpha = 1; MgfFx.Punch(toastRt, 0.05f, 0.25f); }
        void HideToast() { toastG.alpha = 0; toastT = 9; toastDur = 0; }

        /// <summary>훅이 애니메이션 도중에 불렸을 때: 진행 중인 것을 즉시 끝낸다.</summary>
        void FinishPending()
        {
            for (int guard = 0; guard < 4 && ph == Ph.Play; guard++)
            {
                if (sp == Sp.Enter) { sp = Sp.Work; enterX = 0; }
                if (sp == Sp.Work) break;
                Resolve();
            }
            frozen = false; cardT = 0; cardG.alpha = 0;
        }

        void Resolve()
        {
            if (ph == Ph.Title) { demoLoop++; NewDemo(); return; }
            if (ph == Ph.Practice)
            {
                if (sp == Sp.Split) { practiceSeen = true; ShowCard("연습 끝", "주문 12장 · 90초 · 예비 아크릴 3장\n틀리면 예비 장에 금이 간다", 2.0f); EndPractice(); frozen = true; cardT = 2.0f; cardG.alpha = 1; }
                return;
            }
            if (sp == Sp.Wrong)
            {
                if (st.spares <= 0) { EndRun("spent"); return; }
                if (willRetry)
                {
                    sp = Sp.Work; spT = 0; pushVis = 0; dropZ = 0; swingAmp = 0; sheetScale = 1f;
                    frostK = 0.45f; frostR.enabled = true;
                    sheetLeft = Mathf.Max(sheetLeft, 5f);
                    RefreshSheetVisual(true);
                    return;
                }
                NextSheet();
                return;
            }
            if (sp == Sp.Timeout && st.spares <= 0) { EndRun("spent"); return; }
            NextSheet();
        }

        void EndRun(string reason)
        {
            endReason = reason;
            ph = Ph.End; endT = 0;
            HideToast();
            if (st.processed > best) { best = st.processed; PlayerPrefs.SetInt("yurikal.best", best); }
            bool newBest = st.score > bestScore;
            if (newBest) { bestScore = st.score; PlayerPrefs.SetInt("yurikal.bestScore", bestScore); }
            if (st.maxCombo > bestCombo) { bestCombo = st.maxCombo; PlayerPrefs.SetInt("yurikal.bestCombo", bestCombo); }
            PlayerPrefs.Save();
            endHead.text = reason == "clear" ? "주문 12장 처리" : reason == "time" ? "시간 종료" : "예비 아크릴 소진";
            endBest.text = newBest ? "최고 점수 갱신" : "최고 점수 " + bestScore;
            endBest.color = newBest ? Mag : C(InkC, 0.75f);
            YuriSound.Play("end", 0.7f);
            SetVisible();
            MgfBridge.NotifyChanged();
        }

        void SetVisible()
        {
            bool play = ph == Ph.Practice || ph == Ph.Play;
            titleRt.gameObject.SetActive(ph == Ph.Title);
            hudRt.gameObject.SetActive(play || ph == Ph.End);
            ticketRt.gameObject.SetActive(play);
            rackRt.gameObject.SetActive(play);
            endRt.gameObject.SetActive(ph == Ph.End);
            timeTxt.gameObject.SetActive(ph != Ph.Practice);
            if (ph == Ph.End) endG.alpha = 0;
            if (!play) { cardG.alpha = 0; }
            sheetRoot.gameObject.SetActive(ph != Ph.End);
        }

        // ─────────────────────────────── 매 프레임
        void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            Layout();
            UpdateCamera(dt);
            UpdateAmbient(dt);
            UpdateRipples(dt);
            UpdateToast(dt);
            if (hitStop > 0) { hitStop -= dt; UpdateHud(dt); return; }
            switch (ph)
            {
                case Ph.Title: UpdateTitle(dt); break;
                case Ph.Practice: case Ph.Play: UpdatePlay(dt); break;
                case Ph.End: UpdateEnd(dt); break;
            }
            UpdateSheetAnim(dt);
            UpdateHud(dt);
        }

        void UpdateAmbient(float dt)
        {
            float t = Time.time;
            lampT.position = new Vector3(Mathf.Sin(t * 0.23f) * 11f, 9f, 6f + Mathf.Cos(t * 0.17f) * 5f);
            // 유리칼 머리가 숨 쉬듯 맥동(잡혀 있지 않을 때)
            if (headRoot.gameObject.activeSelf)
            {
                float s = press == Press.O ? 1.12f : 1f + Mathf.Sin(t * 3.2f) * 0.035f;
                if (jiggleT > 0) { jiggleT -= dt; }
                float jx = jiggleT > 0 ? Mathf.Sin(jiggleT * 60f) * 0.18f : 0f;
                var target = new Vector3(Ol.x + jx, TopY, Ol.y);
                headRoot.localPosition = Vector3.Lerp(headRoot.localPosition, target, 1f - Mathf.Exp(-dt * 30f));
                headRoot.localScale = Vector3.Lerp(headRoot.localScale, HeadScale * new Vector3(s, press == Press.O ? 0.8f : 1f, s), 1f - Mathf.Exp(-dt * 18f));
                wheelT.localRotation = Quaternion.Euler(0, 90 - cur.phi, 90);
            }
            if (trailT2 > 0)
            {
                trailT2 -= dt; trailT.gameObject.SetActive(trailT2 > 0);
                trailT.localRotation = Quaternion.Euler(0, (0.25f - trailT2) * 1440f, 0);
                trailT.localScale = Vector3.one * (1f + (0.25f - trailT2) * 1.2f);
            }
        }

        void UpdateToast(float dt)
        {
            if (toastDur <= 0) return;
            toastT += dt;
            if (toastT > toastDur) { toastG.alpha = Mathf.Max(0, 1 - (toastT - toastDur) / 0.3f); if (toastG.alpha <= 0) toastDur = 0; }
        }

        // ── 타이틀: 같은 매트 위 데모(유령 손이 O 를 중점 턱에 붙이고 → 위로 → 십자 스냅 → 같은 넓이)
        void UpdateTitle(float dt)
        {
            titleT += dt; logoT += dt;
            if (StepCounters(cur, dt)) PlaceLabels(cur, true, false);
            for (int i = 0; i < 3; i++)
            {
                float k = Mathf.Clamp01((logoT - 0.25f - i * 0.1f) / 0.35f);
                float s = k <= 0 ? 0 : BackOut(k);
                logo[i].rectTransform.localScale = Vector3.one * s;
                logo[i].rectTransform.localRotation = Quaternion.Euler(0, 0, (1 - k) * (i - 1) * 14f);
            }
            float br = 1f + Mathf.Sin(Time.time * 2.4f) * 0.025f;
            ctaRt.localScale = Vector3.Lerp(ctaRt.localScale, Vector3.one * (press == Press.Cta ? 0.93f : br), 1f - Mathf.Exp(-dt * 20f));
            // 데모 조작(입력 경로가 아니다 — 연출용 자동 재생)
            if (sp == Sp.Work)
            {
                float t = spT;
                fingerT.gameObject.SetActive(t > 0.2f && t < 1.9f);
                if (t > 0.35f && t < 1.6f)
                {
                    float k = Smooth(Mathf.Clamp01((t - 0.35f) / 1.1f));
                    int s1 = Mathf.RoundToInt(Mathf.Lerp(cur.startS1, cur.L1 / 2, k) / 5f) * 5, s2 = Mathf.RoundToInt(Mathf.Lerp(cur.startS2, cur.L2 / 2, k) / 5f) * 5;
                    if (s1 != cur.s1 || s2 != cur.s2) { cur.s1 = s1; cur.s2 = s2; RefreshSheetVisual(false); }
                }
                fingerT.localPosition = new Vector3(Ol.x + 0.3f, TopY + 1.2f, Ol.y - 0.3f);
                if (t > 1.7f && t < 2.1f) { pushVis = Mathf.Lerp(0, 2.4f, (t - 1.7f) / 0.4f); fingerT.gameObject.SetActive(false); }
                if (t >= 2.1f) { fingerT.gameObject.SetActive(false); lastAct = Act.Split; StartSplit(); }
            }
            HandleTaps(dt);
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) BeginGame();
        }

        void BeginGame()
        {
            YuriSound.Play("press", 0.8f);
            fingerT.gameObject.SetActive(false);
            StartRun(!practiceSeen);
        }

        static float BackOut(float k) { float c1 = 1.70158f, c3 = c1 + 1; return 1 + c3 * Mathf.Pow(k - 1, 3) + c1 * Mathf.Pow(k - 1, 2); }
        static float Smooth(float k) => k * k * (3 - 2 * k);

        void HandleTaps(float dt)
        {
            if (MgfPointer.Down)
            {
                downScreen = MgfPointer.Position;
                press = Hit(ctaRt, downScreen) ? Press.Cta : Press.None;
                if (press == Press.None) { Ripple(downScreen, C(InkC, 0.7f)); YuriSound.Play("ripple"); st.touches++; MgfBridge.NotifyChanged(); }
            }
            if (MgfPointer.Up)
            {
                // 주 CTA 는 「시작하기」 하나지만, 타이틀 = 플레이 매트라 어디를 눌러도 판이 열린다(무반응 탭 없음)
                if (press == Press.Cta ? Hit(ctaRt, MgfPointer.Position) : titleT > 0.6f) BeginGame();
                press = Press.None;
            }
        }

        // ── 플레이
        void UpdatePlay(float dt)
        {
            if (cardT > 0)
            {
                cardT -= dt;
                if (cardT <= 0) { frozen = false; }
                cardG.alpha = Mathf.Clamp01(cardT / 0.25f);
            }
            bool ticking = ph == Ph.Play && !frozen && (sp == Sp.Work || sp == Sp.Enter);
            if (ticking)
            {
                runLeft -= dt;
                if (sp == Sp.Work) sheetLeft -= dt;
                if (runLeft <= 0) { runLeft = 0; EndRun("time"); return; }
                if (sp == Sp.Work && sheetLeft <= 0) Timeout();
            }
            if (frostMatT < 1.5f) { frostMatT += dt; }
            matFrostR.enabled = frostMatT < 1.5f;
            if (matFrostR.enabled) matFrostMat.color = new Color(1, 1, 1, 1f - frostMatT / 1.5f);

            if (sp == Sp.Work) HandleWorkInput(dt);
            else if (MgfPointer.Down) { Ripple(MgfPointer.Position, C(InkC, 0.5f)); st.touches++; MgfBridge.NotifyChanged(); }

            UpdateGuides(dt);
        }

        void Timeout()
        {
            if (!tried) st.attempts++;
            tried = true;
            st.combo = 0; st.processed++;
            LoseSpare();
            sp = Sp.Timeout; spT = 0;
            press = Press.None;
            YuriSound.Play("frost", 0.8f);
            ShowToast("장당 " + cur.limitSec + "초가 지났다 · 장이 컨베이어에서 떨어졌다", 1.4f);
            frostShiftNext = 2.2f; frostMatT = 0;
            MgfBridge.NotifyChanged();
        }

        float PxPerCm => Screen.height / (2f * cam.orthographicSize);
        float ScaleF => canvas ? canvas.scaleFactor : 1f;

        bool PointerLocal(out Vector2 local)
        {
            var ray = cam.ScreenPointToRay(MgfPointer.Position);
            var plane = new Plane(Vector3.up, new Vector3(0, TopY, 0));
            local = default;
            if (!plane.Raycast(ray, out float d)) return false;
            var w = ray.GetPoint(d);
            var l = sheetRoot.InverseTransformPoint(w);
            local = new Vector2(l.x, l.z);
            return true;
        }

        void HandleWorkInput(float dt)
        {
            // 키보드: ↑/Space 쪼개기 · ↓ 치우기
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.Space)) { Commit(Act.Split); return; }
            if (Input.GetKeyDown(KeyCode.DownArrow)) { Commit(Act.Scrap); return; }

            float hitO = Mathf.Max(1.25f, 28f * ScaleF / PxPerCm);
            float hitR = Mathf.Max(1.15f, 26f * ScaleF / PxPerCm);
            if (MgfPointer.Down && PointerLocal(out var lp))
            {
                downScreen = MgfPointer.Position; downTime = Time.time; movedO = false; idleT = 0;
                if (!cur.Locked && (lp - Ol).magnitude < hitO) { press = Press.O; grabOff = Ol - lp; oVis = Ol; YuriSound.Play("tick", 0.6f); }
                else if (cur.canRotate && (lp - Dl).magnitude < hitR) { press = Press.Rot; thetaVis = cur.theta; }
                else press = Press.Sheet;
                st.touches++; MgfBridge.NotifyChanged();
            }
            if (MgfPointer.Held && press != Press.None && PointerLocal(out var hp))
            {
                if (press == Press.O)
                {
                    var target = hp + grabOff;
                    oVis = Vector2.MoveTowards(oVis, target, Rules.OSpeed * dt);
                    Oblique(oVis, out double a, out double b);
                    int s1 = Judge.SnapS(a, cur.L1), s2 = Judge.SnapS(b, cur.L2);
                    if (s1 != cur.s1 || s2 != cur.s2)
                    {
                        cur.s1 = s1; cur.s2 = s2; movedO = true;
                        RefreshSheetVisual(false);
                        YuriSound.Play("tick", 0.5f, 0.9f + (s1 + s2) % 7 * 0.03f);
                        MgfBridge.NotifyChanged();
                        CheckLock();
                    }
                }
                else if (press == Press.Rot)
                {
                    var d = hp - Ol;
                    float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                    float th = Mathf.DeltaAngle(cur.phi, ang);
                    if (th < 0) th = th < -90 ? Judge.RotMax : Judge.RotMin;
                    thetaVis = Mathf.MoveTowards(thetaVis, th, Rules.RotSpeed * dt);
                    int nt = Judge.SnapTheta(thetaVis);
                    if (nt != cur.theta)
                    {
                        cur.theta = nt; Rotated(cur); RefreshSheetVisual(false); SetDataLine(cur);
                        YuriSound.Play("rot", 0.5f);
                        MgfBridge.NotifyChanged();
                        CheckLock();
                    }
                }
                else if (press == Press.Sheet)
                {
                    float dy = (MgfPointer.Position.y - downScreen.y) / PxPerCm;
                    pushVis = Mathf.Clamp(dy * 0.55f, -2.6f, 2.6f);
                }
            }
            if (MgfPointer.Up && press != Press.None)
            {
                var up = MgfPointer.Position;
                var dl = (up - downScreen) / ScaleF;
                var pr = press; press = Press.None;
                if (pr == Press.O) { oVis = Ol; if (!movedO) TapO(); }
                else if (pr == Press.Sheet)
                {
                    if (dl.y > 55f && Mathf.Abs(dl.y) > Mathf.Abs(dl.x) * 1.2f) { Commit(Act.Split); return; }
                    if (dl.y < -55f && Mathf.Abs(dl.y) > Mathf.Abs(dl.x) * 1.2f) { Commit(Act.Scrap); return; }
                    if (dl.magnitude < 16f && PointerLocal(out var tp)) Tap(tp, up);
                }
            }
            if (press != Press.Sheet) pushVis = Mathf.Lerp(pushVis, 0, 1f - Mathf.Exp(-dt * 14f));

            // 길이 숫자 카운트(1단계)
            if (Numbers(cur) && StepCounters(cur, dt)) PlaceLabels(cur, true, false);
            idleT += dt;
        }

        void CheckLock()
        {
            // 1단계(숫자 보임)에서만: 두 쌍이 동시에 같아지는 순간 유리칼 머리가 한 번 딸깍 잠긴다
            bool b = Judge.Bisect(cur);
            if (b && !bisectLocked && Numbers(cur)) { YuriSound.Play("lock", 0.9f); trailT2 = 0.25f; }
            bisectLocked = b;
            if (ph == Ph.Practice)
            {
                int want = b ? 1 : 0;
                if (want != obStep)
                {
                    obStep = want;
                    SetHint(b ? "이제 장을 위로 밀어 쪼개시오 ↑" : "점 O를 밀어 이등분하시오");
                    ghostT = 0;
                }
            }
        }

        void TapO()
        {
            // 누르기만 하고 안 끌었다: O 가 흔들리고 유령 경로가 즉시 다시 나온다
            jiggleT = 0.22f; ghostT = 0; ghostScale = 1.2f;
            Ripple(MgfPointer.Position, C(Orange, 0.8f));
            YuriSound.Play("ripple");
            if (ph == Ph.Play) ShowToast(cur.Locked ? "이 장은 점 O가 잠겨 있다 · 위로 쪼개거나 아래로 치우시오" : "점 O를 누른 채 끌어 옮기시오", 1.2f);
        }

        void Tap(Vector2 local, Vector2 screen)
        {
            // 막대 반쪽 근처 → 캘리퍼(길이 재기) · 빈 곳 → 리플 + O 쪽 화살표
            Vector2[] v = { Al, Bl, Cl, Dl };
            int[] mm = { cur.s1, cur.s2, cur.CO, cur.DO };
            string[] nm = { "AO", "BO", "CO", "DO" };
            int bestI = -1; float bestD = 0.8f;
            for (int i = 0; i < 4; i++)
            {
                float d = DistSeg(local, v[i], Ol);
                if (d < bestD && (local - Ol).magnitude > 0.9f) { bestD = d; bestI = i; }
            }
            if (bestI >= 0 && !cur.Locked)
            {
                var p = (v[bestI] + Ol) * 0.5f;
                var dd = Ol - v[bestI]; var n = new Vector2(-dd.y, dd.x).normalized;
                p -= n * 0.9f;
                caliperLbl.transform.localPosition = new Vector3(p.x, TopY + 0.3f, p.y);
                caliperLbl.text = nm[bestI] + " = " + Words.Cm(mm[bestI]) + " cm";
                caliperLbl.gameObject.SetActive(true); caliperT = 1.6f;
                caliperSeg = bestI;
                YuriSound.Play("caliper", 0.8f);
                Ripple(screen, C(Orange, 0.8f));
                return;
            }
            Ripple(screen, C(InkC, 0.7f));
            YuriSound.Play("ripple");
            if (!cur.Locked) { arrowT = 0.7f; arrowFrom = local; }
            if (ph == Ph.Practice) { ghostT = 0; ghostScale = 1.3f; }
        }
        int caliperSeg = -1;

        static float DistSeg(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a; float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(1e-5f, ab.sqrMagnitude));
            return (a + ab * t - p).magnitude;
        }

        /// <summary>유령 손가락·점선 경로·화살표·캘리퍼 조(뭔가 누르면 항상 무언가가 움직인다).</summary>
        void UpdateGuides(float dt)
        {
            guide.Clear();
            bool any = false;
            if (caliperT > 0)
            {
                caliperT -= dt;
                if (caliperT <= 0) { caliperLbl.gameObject.SetActive(false); caliperSeg = -1; }
                else if (caliperSeg >= 0)
                {
                    Vector2[] v = { Al, Bl, Cl, Dl };
                    var a = v[caliperSeg]; var d = Ol - a; var n = new Vector2(-d.y, d.x).normalized * -0.45f;
                    var oc = C32(Orange);
                    guide.Seg(a + n, Ol + n, 0.08f, oc); guide.Seg(a, a + n * 1.3f, 0.07f, oc); guide.Seg(Ol, Ol + n * 1.3f, 0.07f, oc);
                    any = true;
                }
            }
            if (arrowT > 0 && sp == Sp.Work)
            {
                arrowT -= dt;
                var d = (Ol - arrowFrom); float L = d.magnitude;
                if (L > 1.5f)
                {
                    var dir = d / L; float k = 1f - arrowT / 0.7f;
                    guide.Chevron(arrowFrom + dir * (0.6f + k * 1.2f), dir, 0.8f, 0.14f, C32(InkC, 0.85f * (1 - k * 0.5f)));
                    guide.Chevron(arrowFrom + dir * (1.4f + k * 1.2f), dir, 0.8f, 0.14f, C32(InkC, 0.6f * (1 - k * 0.5f)));
                    any = true;
                }
            }
            // 오답 뒤: 정답 상태를 잠깐 보여 준다(구성 장이고 만들 수 있었을 때)
            if (sp == Sp.Wrong && !cur.Locked && Judge.Possible(cur) && spT > 0.3f)
            {
                var P = P0; // 이등분점 = P0 + (L1/2)u1 + (L2/2)u2 — 현재 교각 기준
                var tgt = P + (cur.L1 / 20f) * u1 + (cur.L2 / 20f) * u2;
                var oc = C32(Orange, 0.95f);
                guide.Ring(tgt, 0.75f, 0.1f, oc);
                var ut = cur.canRotate ? Dir(cur.phi + 90) : u2;
                guide.Dashed(tgt - u1 * (cur.L1 / 20f), tgt + u1 * (cur.L1 / 20f), 0.35f, 0.25f, 0.07f, oc, 0);
                guide.Dashed(tgt - ut * (cur.L2 / 20f), tgt + ut * (cur.L2 / 20f), 0.35f, 0.25f, 0.07f, oc, 0);
                any = true;
            }
            // 연습(온보딩): 조작 대상을 가리킨다
            bool pulse = false;
            if (ph == Ph.Practice && sp == Sp.Work && press == Press.None && cardT <= 0)
            {
                if (idleT > 8f) { idleT = 0; ghostT = 0; ghostScale = 1.4f; }
                ghostT += dt;
                if (obStep == 0)
                {
                    var tgt = P0 + (cur.L1 / 20f) * u1 + (cur.L2 / 20f) * u2;
                    var oc = C32(InkC, 0.75f);
                    guide.Dashed(Ol, tgt, 0.3f, 0.22f, 0.08f * ghostScale, oc, -Time.time * 1.2f);
                    var dir = (tgt - Ol).normalized;
                    guide.Chevron(tgt - dir * 0.2f, dir, 0.55f * ghostScale, 0.1f, oc);
                    float cyc = ghostT % 2.0f, k = Smooth(Mathf.Clamp01((cyc - 0.3f) / 1.2f));
                    var f = Vector2.Lerp(Ol, tgt, k);
                    fingerT.gameObject.SetActive(cyc < 1.8f);
                    fingerT.localPosition = new Vector3(f.x + 0.25f, TopY + 1.5f, f.y - 0.25f);
                    fingerT.localScale = Vector3.one * ghostScale * (cyc < 0.3f ? 1.15f - cyc * 0.5f : 1f);
                    pulse = true;
                }
                else
                {
                    // 위로 밀기: 장 위에서 브레이커 쪽으로 흐르는 셰브론
                    var c = C32(InkC, 0.8f);
                    for (int i = 0; i < 3; i++)
                    {
                        float ph2 = (Time.time * 1.6f + i / 3f) % 1f;
                        guide.Chevron(new Vector2(0, 2.5f + ph2 * 4f) - new Vector2(sheetCenter.x, 0) * 0, Vector2.up, 1.1f * ghostScale, 0.16f, C32(InkC, 0.9f * (1 - ph2)));
                    }
                    float cyc = ghostT % 1.6f, k = Smooth(Mathf.Clamp01(cyc / 1.0f));
                    fingerT.gameObject.SetActive(cyc < 1.2f);
                    fingerT.localPosition = new Vector3(2.2f, TopY + 1.5f, -1.5f + k * 5f);
                    fingerT.localScale = Vector3.one * ghostScale;
                    guide.Dashed(new Vector2(2.2f, -1.5f), new Vector2(2.2f, 3.5f), 0.3f, 0.22f, 0.07f, c, -Time.time * 1.2f);
                }
                any = true;
            }
            else if (ph != Ph.Title) fingerT.gameObject.SetActive(false);
            if (ph == Ph.Play && sp == Sp.Work && cur.canRotate && cur.no > 0 && hintTxt.text.Length > 0 && press == Press.None)
            {
                float k = (Time.time * 1.2f) % 1f;
                guide.Ring(Dl, 0.8f + k * 0.8f, 0.08f, C32(Orange, 1 - k));
                any = true;
            }
            pulseT.gameObject.SetActive(pulse);
            if (pulse)
            {
                float k = (Time.time * 1.1f) % 1f;
                pulseT.localPosition = new Vector3(Ol.x, TopY + 0.02f, Ol.y);
                pulseT.localScale = Vector3.one * (1.0f + k * 1.3f);
            }
            if (any || guideHad) guide.Apply();
            guideHad = any;
        }
        bool guideHad = true;

        // ── 장 애니메이션(입장 · 쪼개기 · 치우기 · 오답 · 시간 초과)
        void UpdateSheetAnim(float dt)
        {
            spT += dt;
            switch (sp)
            {
                case Sp.Enter:
                    {
                        float k = Mathf.Clamp01(spT / 0.45f);
                        enterX = 30f * (1f - BackOut(k));
                        if (k >= 1) { sp = Sp.Work; spT = 0; enterX = 0; }
                        break;
                    }
                case Sp.Split:
                    {
                        // 0~0.18 브레이커로 빨려 들어감 → 0.15초 정적(hit-stop) → 흰 칼금 번짐 + 십자 스냅 → 네 조각이 매트 결을 따라 미끄러짐
                        if (spT < 0.18f) pushVis = Mathf.Lerp(pushVis, 2.4f, spT / 0.18f);
                        else if (sep == 0 && spT >= 0.18f)
                        {
                            sep = 0.0001f; hitStop = 0.15f;
                            YuriSound.Play("snap", 1f);
                            FlashScore();
                            if (ph == Ph.Title) { for (int i = 0; i < 3; i++) MgfFx.Punch(logo[i].rectTransform, 0.12f, 0.3f); }
                            else camKick = 0.04f;
                        }
                        else
                        {
                            float k = Mathf.Clamp01((spT - 0.18f) / 0.5f);
                            sep = BackOut(k);
                            pushVis = Mathf.Lerp(2.4f, 0.6f, k);
                            float g = Mathf.Clamp01(1f - (spT - 0.18f) / 0.45f);
                            glowMat.color = new Color(1, 1, 1, g); glowR.enabled = g > 0;
                            // 넓이 카운트업
                            float ka = Mathf.Clamp01((spT - 0.45f) / 0.6f);
                            for (int i = 0; i < 4; i++)
                            {
                                bool on = spT > 0.45f;
                                if (areaLbl[i].gameObject.activeSelf != on) areaLbl[i].gameObject.SetActive(on);
                                if (on) areaLbl[i].transform.localScale = Vector3.one * (0.6f + 0.4f * BackOut(Mathf.Clamp01((spT - 0.45f) / 0.3f)));
                            }
                            if (areaTarget != null && spT > 0.45f && ka < 1f)
                            {
                                double tv = double.Parse(areaTarget, System.Globalization.CultureInfo.InvariantCulture);
                                string s = (tv * ka).ToString(tv % 1 == 0 ? "0" : "0.0", System.Globalization.CultureInfo.InvariantCulture) + " cm²";
                                if (s != areaCountStr) { areaCountStr = s; for (int i = 0; i < 4; i++) areaLbl[i].text = s; YuriSound.Play("blip", 0.25f, 1f + ka * 0.5f); }
                            }
                            else if (areaTarget != null && ka >= 1f && areaCountStr != areaTarget + " cm²") { areaCountStr = areaTarget + " cm²"; for (int i = 0; i < 4; i++) areaLbl[i].text = areaCountStr; }
                            float hold = ph == Ph.Practice ? 2.3f : ph == Ph.Title ? 1.6f : 1.6f;
                            if (spT > 0.7f + hold)
                            {
                                float e = Mathf.Clamp01((spT - 0.7f - hold) / 0.35f);
                                enterX = -32f * e * e;
                                if (e >= 1) { areaCountStr = null; Resolve(); }
                            }
                        }
                        ApplySeparation();
                        break;
                    }
                case Sp.ScrapGood:
                    {
                        float k = Mathf.Clamp01(spT / 0.4f);
                        dropZ = Mathf.Lerp(0, BinZ - sheetCenter.y + 0.5f, k * k);
                        sheetScale = Mathf.Lerp(1f, 0.35f, k);
                        if (spT > 0.4f && spT - dt <= 0.4f) MgfFx.Punch(binT, 0.06f, 0.25f);
                        if (spT > 1.6f) Resolve();
                        break;
                    }
                case Sp.Wrong:
                    {
                        // 서리 거미줄이 O 에서 퍼지고 장이 한 번 비틀린다(Swing). 정답 상태를 잠깐 보여 준 뒤 다시 한 번 기회.
                        if (spT < 0.18f) pushVis = lastAct == Act.Split ? Mathf.Lerp(0, 2.2f, spT / 0.18f) : Mathf.Lerp(0, -2.2f, spT / 0.18f);
                        else pushVis = Mathf.Lerp(pushVis, 0, 1f - Mathf.Exp(-dt * 10f));
                        if (spT >= 0.18f)
                        {
                            frostR.enabled = true;
                            frostK = Mathf.Min(1f, frostK + dt * 4f);
                            float w = spT - 0.18f;
                            swingAmp = 9f * Mathf.Exp(-w * 4f) * Mathf.Sin(w * 18f);
                        }
                        if (spT > 2.1f)
                        {
                            if (!willRetry && st.spares > 0)
                            {
                                float e = Mathf.Clamp01((spT - 2.1f) / 0.35f);
                                dropZ = Mathf.Lerp(0, BinZ - sheetCenter.y + 0.5f, e * e); sheetScale = Mathf.Lerp(1, 0.35f, e);
                                if (e >= 1) Resolve();
                            }
                            else Resolve();
                        }
                        break;
                    }
                case Sp.Timeout:
                    {
                        float k = Mathf.Clamp01(spT / 0.6f);
                        dropZ = -18f * k * k; swingAmp = k * 25f;
                        if (spT > 1.2f) Resolve();
                        break;
                    }
            }
            frostWeb.localScale = new Vector3(frostK, 1, frostK);
            if (frostR.enabled) frostMat.color = new Color(1, 1, 1, Mathf.Clamp01(frostK));
            sheetRoot.localPosition = new Vector3(sheetCenter.x + enterX, 0, sheetCenter.y + pushVis + dropZ);
            sheetRoot.localRotation = Quaternion.Euler(0, swingAmp, 0);
            sheetRoot.localScale = new Vector3(sheetScale, 1, sheetScale);
        }
        string areaCountStr;

        void ApplySeparation()
        {
            Vector2[] q = { Al, Bl, Cl, Dl };
            for (int i = 0; i < 4; i++)
            {
                var c = (q[i] + q[(i + 1) % 4] + Ol) / 3f;
                var d = (c - Ol).normalized;
                var p = c + d * (1.5f * sep);
                pieceT[i].localPosition = new Vector3(p.x, 0, p.y);
                pieceT[i].localRotation = Quaternion.Euler(0, (i % 2 == 0 ? 1 : -1) * 4f * sep, 0);
            }
            bool on = sep < 0.05f;
            ink.mesh.bounds = on ? new Bounds(Vector3.zero, new Vector3(200, 10, 200)) : new Bounds(new Vector3(9999, 0, 0), Vector3.one);
            headRoot.gameObject.SetActive(on && !cur.Locked);
            rivet.gameObject.SetActive(on && cur.Locked);
            rotHandle.gameObject.SetActive(on && cur.canRotate);
            for (int i = 0; i < 4; i++) { lenLbl[i].gameObject.SetActive(on && Numbers(cur) && !cur.Locked); vLbl[i].gameObject.SetActive(on); }
            angLbl.gameObject.SetActive(false);
        }

        void FlashScore()
        {
            glow.Clear();
            var c = new Color32(255, 255, 255, 255);
            glow.Seg(Al, Cl, 0.35f, c); glow.Seg(Bl, Dl, 0.35f, c);
            glow.Seg(Al, Cl, 0.9f, new Color32(255, 250, 220, 90)); glow.Seg(Bl, Dl, 0.9f, new Color32(255, 250, 220, 90));
            glow.Apply();
            glowR.enabled = true; glowMat.color = Color.white;
        }

        // ── 끝 화면: 결과 카운트업 · 다시 하기 1탭
        void UpdateEnd(float dt)
        {
            endT += dt;
            endG.alpha = Mathf.Clamp01(endT / 0.3f);
            endRt.localScale = Vector3.one * (0.9f + 0.1f * BackOut(Mathf.Clamp01(endT / 0.4f)));
            float k = Mathf.Clamp01((endT - 0.3f) / 1.0f);
            int proc = Mathf.RoundToInt(st.processed * k), ft = Mathf.RoundToInt(st.firstTry * k), sc = Mathf.RoundToInt(st.score * k);
            string s = "처리한 장  " + proc + " / 12\n첫 시도 정답  " + ft + " / " + st.attempts + "\n최장 콤보  " + st.maxCombo + "\n점수  " + sc;
            if (s != endStr) { endStr = s; endStats.text = s; }
            float br = 1f + Mathf.Sin(Time.time * 2.4f) * 0.025f;
            endCtaRt.localScale = Vector3.one * (press == Press.EndCta ? 0.93f : br);
            if (MgfPointer.Down)
            {
                downScreen = MgfPointer.Position;
                press = Hit(endCtaRt, downScreen) ? Press.EndCta : Press.None;
                if (press == Press.None) { Ripple(downScreen, C(InkC, 0.7f)); st.touches++; MgfBridge.NotifyChanged(); }
            }
            if (MgfPointer.Up)
            {
                if (press == Press.EndCta && Hit(endCtaRt, MgfPointer.Position) && endT > 0.5f) { YuriSound.Play("press"); StartRun(false); }
                press = Press.None;
            }
            if (endT > 0.8f && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))) StartRun(false);
        }
        string endStr;

        // ── HUD
        void UpdateHud(float dt)
        {
            if (ph == Ph.Title) return;
            int ti = Mathf.CeilToInt(runLeft);
            if (ti != lastTimeInt) { lastTimeInt = ti; timeTxt.text = ti + "<size=60%> 초</size>"; if (ti <= 10 && ph == Ph.Play) MgfFx.Punch(timeTxt.transform, 0.12f, 0.2f); }
            timeTxt.color = runLeft <= 10 ? Mag : InkC;
            timeBarFill.localScale = new Vector3(Mathf.Clamp01(runLeft / Rules.RunSec), 1, 1);
            sheetBarFill.localScale = new Vector3(cur != null && cur.limitSec > 0 ? Mathf.Clamp01(sheetLeft / cur.limitSec) : 1f, 1, 1);
            if (shownScore < st.score) shownScore = Mathf.Min(st.score, shownScore + Mathf.Max(60f, (st.score - shownScore) * 6f) * dt);
            else shownScore = st.score;
            int si = Mathf.RoundToInt(shownScore);
            if (si != shownScoreInt) { if (shownScoreInt >= 0 && si > shownScoreInt && si == st.score) MgfFx.Punch(scoreTxt.transform, 0.18f, 0.25f); shownScoreInt = si; scoreTxt.text = si.ToString(); }
            string cn = ph == Ph.Practice ? "연습" : "주문 " + Mathf.Min(12, deckIdx + 1) + " / 12";
            if (cn != counterTxt.text) counterTxt.text = cn;
            string cb = st.combo >= 2 ? "콤보 " + st.combo + (Rules.Mult(st.combo) > 1 ? "  ×" + Rules.Mult(st.combo) : "") : "";
            if (cb != comboTxt.text) comboTxt.text = cb;
        }

        // ── 카메라: 판형(세로/가로)에 맞춰 플레이 영역을 빈 화면 한가운데에 둔다(정사영이라 길이 왜곡 없음)
        const float HeadScale = 1.7f;
        float camKick;
        void UpdateCamera(float dt)
        {
            float W = canvasRt.rect.width, H = canvasRt.rect.height, s = ScaleF;
            float top, bottom, left, right;
            float x0, x1, z0, z1;
            if (ph == Ph.Title)
            {
                if (titleLand) { top = 20; bottom = 10; left = W * 0.52f; right = 10; x0 = -11f; x1 = 11f; z0 = BinZ - 1.5f; z1 = BreakerZ + 1.3f; }
                else { top = 225; bottom = 170; left = 8; right = 8; x0 = -10.5f; x1 = 10.5f; z0 = -9f; z1 = 9.5f; }
            }
            else
            {
                top = insetTop; bottom = insetBottom; left = insetLeft; right = insetRight;
                x0 = -10f; x1 = 10f; z0 = BinZ - 1.5f; z1 = BreakerZ + 1.3f;
            }
            float fw = Mathf.Max(50, (W - left - right) * s), fh = Mathf.Max(50, (H - top - bottom) * s);
            float k = Mathf.Min(fw / (x1 - x0), fh / (z1 - z0));
            float fcx = (left + (W - left - right) * 0.5f) * s, fcy = (bottom + (H - top - bottom) * 0.5f) * s;
            float ortho = Screen.height / (2f * k);
            float cx = (x0 + x1) * 0.5f - (fcx - Screen.width * 0.5f) / k;
            float cz = (z0 + z1) * 0.5f - (fcy - Screen.height * 0.5f) / k;
            float a = 1f - Mathf.Exp(-dt * 10f);
            if (camInit) a = 1f;
            camInit = false;
            camKick = Mathf.Max(0, camKick - dt * 0.2f);
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, ortho * (1f - camKick), a);
            var p = cam.transform.position;
            cam.transform.position = new Vector3(Mathf.Lerp(p.x, cx, a), 40f, Mathf.Lerp(p.z, cz, a));
        }
        bool camInit = true;
    }
}
