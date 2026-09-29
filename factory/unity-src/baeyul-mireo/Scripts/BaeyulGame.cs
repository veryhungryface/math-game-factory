// 배율 밀어 — 활판 교정대에서 놋쇠 레일 손잡이를 밀어 도장의 배율을 닮음비에 맞춘 뒤, 흑철 플래튼을 끌어 내려
// 닮은 도형을 주홍 잉크로 찍는 90초 아케이드(중2 도형의 닮음).
//
// 답 입력 경로는 셋뿐이다: (1) 레일 손잡이를 끌어 레일 변의 길이(= 배율)를 정한다
//                          (2) 플래튼(또는 인화지)을 아래로 끌어 찍는다 = 「닮음이다」 확정
//                          (3) 인화지를 왼쪽으로 플릭해 스파이크에 꽂는다 = 「닮음이 아니다」 확정
// 판정은 BaeyulRules.cs 의 Judge(정수 교차곱·정수 도)뿐이다. 코드는 손잡이를 정답 칸에 스냅하지 않고,
// 정답 칸을 다른 색으로 켜지 않으며, 치울 장을 표시하지 않는다. 훅(TestAnswerCorrect/Wrong)도 같은 Commit() 을 탄다.
using System.Collections.Generic;
using Mgf;
using TMPro;
using UnityEngine;

namespace Mgf.BaeyulMireo
{
    public partial class BaeyulGame : MonoBehaviour, IMgfGame
    {
        [System.Serializable]
        class State : MgfState
        {
            public int sheet, of = 12, spares = 3, combo, maxCombo, firstTry, attempts, processed, touches, handle, railLo, railHi;
            public string kind = "", order = "", gauge = "", misconception = "";
            public bool onboarding, locked;
        }

        readonly State st = new State();
        enum Ph { Title, Practice, Play, End }
        enum Sp { Enter, Work, Stamp, Spike, Wrong, Timeout }
        Ph ph = Ph.Title;
        Sp sp = Sp.Enter;
        float spT;

        Sheet cur;
        List<Sheet> deck;
        int deckIdx = -1;
        readonly System.Random rng = new System.Random(20260930);
        readonly List<MgfProblem> bank = new List<MgfProblem>();

        bool tried, willRetry, practiceSeen, inWindowPrev, stampedShown, spikeAfterWrong, stained;
        Act lastAct;
        float runLeft, sheetLeft, cardT, idleT, hitStop, toastT, toastDur, arrowT, jiggleT, camPress;
        bool frozen;
        int obStep;
        float ghostScale = 1f, ghostT;
        float enterX, sheetDx, sheetScale = 1f, sheetRot, platenDrag, platenZ, platenShake;
        float handleVisMm = 40;
        float shownScore; int shownScoreInt = -1, lastTimeInt = -1;
        int best, bestScore;
        string endReason = "";
        float endT, chanceSum;
        Vector2 arrowFrom;

        enum Press { None, Handle, Platen, Sheet, Other, Cta, EndCta }
        Press press;
        Vector2 downScreen, downWorld;
        bool downWorldOk;
        bool cueWas;
        float railCueT, bobT; // 손잡이 ▲▼ 표시·들썩임(빈 탭이 화면을 바꾼다)
        float practiceT; // 연습 장 경과(초) — 순진한 탭만 해도 30초 안에 연습이 끝나게 보조한다
        float grabOffZ, platenGrab;
        bool committedDrag;

        float titleT, logoT;
        int demoLoop;

        // ─────────────────────────────── 부팅
        void Awake()
        {
            BaeyulSound.Init(gameObject);
            BuildBank();
            BuildWorld();
            BuildUi();
            Prewarm();
            best = PlayerPrefs.GetInt("baeyul.best", 0);
            bestScore = PlayerPrefs.GetInt("baeyul.bestScore", 0);
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb = new System.Text.StringBuilder("배율밀어연습은여기까지위아래로끌어목표한번에간다맞추고내려찍는다탭하시오손잡이를밀어찍어라찍어보기중학교2학년도형의닮음최고점수처리장한판90초주문12장연습시간이흐르지않는다현재비예비인화지원본고무판끌어내려찍기치우기고정인화완료실패시간종료소진다시첫시도정답률콤보갱신우연수준이제가사라진다기호순서가대응이다레일이재는변넓이의비닮음비구분하시오플래튼을아직비가다릅니다지금은로맞추시오됐다↓←→↑·∽△□²∠°:=,.()0123456789ABCDEFGH cm×≠");
            foreach (var p in bank) { sb.Append(p.prompt); sb.Append(p.answer); }
            var g = new SheetGen(new System.Random(5));
            for (int k = 0; k < 6; k++) foreach (var s in g.Deck()) { sb.Append(Words.RevealRight(s, Judge.RightAct(s))); sb.Append(Words.RevealWrong(s, Act.Stamp)); sb.Append(Words.RevealWrong(s, Act.Discard)); sb.Append(Words.Foot(s)); }
            MgfText.Prewarm(sb.ToString());
        }

        // ── 문제 은행: 게임 판과 같은 SheetGen.Deck() 에서 뽑고, 같은 Words.Prompt(= 티켓 문장)·Judge 로 정답을 만든다.
        void BuildBank()
        {
            var seen = new HashSet<string>();
            var g = new SheetGen(new System.Random(777));
            Add(SheetGen.Practice(), seen);
            for (int k = 0; k < 300 && bank.Count < 360; k++)
                foreach (var s in g.Deck()) { if (bank.Count >= 360) break; Add(s, seen); }
        }

        void Add(Sheet s, HashSet<string> seen)
        {
            string p = Words.Prompt(s);
            if (!seen.Add(p)) return;
            bank.Add(new MgfProblem
            {
                id = "bm" + (bank.Count + 1),
                prompt = p,
                choices = null,
                answer = Words.Answer(s),
                answerNumeric = s.kind == Kind.Build ? Judge.Target(s) / 10.0 : double.NaN,   // 구성 장: 레일 변의 길이(cm)
                unitConcept = Words.Concept(s)
            });
        }

        // ─────────────────────────────── IMgfGame
        public void TestStart() { StartRun(false); }

        public void TestAnswerCorrect()
        {
            if (ph == Ph.Title || ph == Ph.End) StartRun(false);
            if (ph == Ph.Practice) EndPractice();
            FinishPending();
            if (ph != Ph.Play) return;
            var a = Judge.RightAct(cur);
            if (cur.kind == Kind.Build) { cur.handle = Judge.Target(cur); handleVisMm = cur.handle; RefreshSheet(false); }
            Commit(a);
        }

        public void TestAnswerWrong()
        {
            if (ph == Ph.Title || ph == Ph.End) StartRun(false);
            if (ph == Ph.Practice) EndPractice();
            FinishPending();
            if (ph != Ph.Play) return;
            Commit(Judge.RightAct(cur) == Act.Stamp ? Act.Discard : Act.Stamp);
        }

        public string StateJson()
        {
            st.phase = ph == Ph.Title ? "title" : ph == Ph.End ? (endReason == "clear" ? "clear" : "gameover") : "playing";
            st.onboarding = ph == Ph.Practice;
            st.lives = st.spares;
            if (cur != null && ph != Ph.Title)
            {
                st.sheet = cur.no; st.handle = cur.handle; st.railLo = cur.railLo; st.railHi = cur.railHi;
                st.kind = cur.kind.ToString(); st.order = cur.order.ToString(); st.gauge = cur.Locked ? "" : Words.GaugeName(cur);
                st.misconception = cur.misconceptionId; st.locked = cur.Locked;
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
            for (int i = 0; i < 4; i++) { logo[i].rectTransform.localScale = Vector3.zero; logoGhost[i].rectTransform.localScale = Vector3.zero; }
            bestTxt.text = bestScore > 0 ? "최고 점수 " + bestScore + " · 처리 " + best + "장" : "한 판 90초 · 주문 12장";
            NewDemo();
            SetVisible();
        }

        void NewDemo()
        {
            var s = SheetGen.Practice();
            cur = s; SetupSheet(s, true);
        }

        void StartRun(bool withPractice)
        {
            st.score = 0; st.solved = 0; st.combo = 0; st.maxCombo = 0; st.firstTry = 0; st.attempts = 0; st.processed = 0;
            st.spares = Rules.Spares; st.lives = st.spares;
            shownScore = 0; shownScoreInt = -1; chanceSum = 0;
            runLeft = Rules.RunSec; frozen = false; cardT = 0; endReason = "";
            for (int i = 0; i < 3; i++) { spare[i].localRotation = Quaternion.identity; spareCrumple[i].enabled = false; spare[i].localScale = Vector3.one; }
            spiked = 0; for (int i = 0; i < spikeSheets.Length; i++) spikeSheets[i].gameObject.SetActive(false);
            deck = new SheetGen(rng).Deck();
            deckIdx = -1;
            HideToast();
            if (withPractice)
            {
                ph = Ph.Practice; obStep = 0; idleT = 0; practiceT = 0; ghostScale = 1f; ghostT = 0;
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
            if (cur.no == 5) ShowCard("이제 현재 비가 사라진다", "원본의 길이와 닮음비로\n레일이 재는 변의 길이를 구하시오", 2.4f);
            else if (cur.order == Order.Corr && cur.no == 6) ShowCard("기호 순서가 대응이다", Words.Sym(cur) + " 에서\n" + CorrLine(cur), 2.6f);
            else if (cur.kind == Kind.Judge && cur.no == 7) ShowCard("레일이 고정된 장", "두 도형이 닮은 도형이면 ↓ 찍고\n아니면 ← 치우시오", 2.4f);
            else if (cur.order == Order.Area && cur.no == 10) ShowCard("넓이 주문", "티켓의 「넓이의 비」와 「닮음비」를\n구분하시오", 2.4f);
            MgfBridge.NotifyChanged();
        }

        static string CorrLine(Sheet s)
        {
            var ol = Words.OL(s); var il = Words.IL(s); var sb = new System.Text.StringBuilder();
            for (int i = 0; i < s.N; i++) { if (i > 0) sb.Append(", "); sb.Append(ol[i]).Append("와 ").Append(il[s.corr[i]]); }
            return sb.Append("가 대응한다").ToString();
        }

        void SetupSheet(Sheet s, bool demo)
        {
            s.Reset();
            handleVisMm = s.Locked ? handleVisMm : s.handle;
            tried = false; willRetry = false; stampedShown = false; spikeAfterWrong = false; stained = false;
            inWindowPrev = !s.Locked && Judge.InWindow(s);
            sp = Sp.Enter; spT = 0; enterX = demo ? 0 : 30f; sheetDx = 0; sheetScale = 1f; sheetRot = 0; platenDrag = 0; platenShake = 0;
            impInk.Clear(); impInk.Apply(); fxInk.Clear(); fxInk.Apply();
            sheetLeft = s.limitSec;
            RefreshSheet(true);
            if (!demo)
            {
                ticketHead.text = s.no == 0 ? "연습 · 시간이 흐르지 않는다" : "주문 " + s.no.ToString("00") + " / 12" + StageNote(s);
                stampNo.text = s.no == 0 ? "연습" : s.no.ToString("00");
                rawOrder = s.no == 0 ? Words.OrderLine(s) : Words.OrderLine(s);
                SetDataLine(s);
                footTxt.text = Words.Foot(s);
                string hint = s.no == 0 ? "손잡이를 끌어 DE를 맞추시오" : "";
                SetHint(hint);
                MgfFx.Punch(ticketRt, 0.05f, 0.25f);
                BaeyulSound.Play("slide", 0.6f);
            }
        }

        static string StageNote(Sheet s) => s.kind == Kind.Judge ? " · 레일 고정" : s.stage == 1 ? "" : s.stage == 2 ? " · 현재 비 없음" : " · 레일 길이 표시 없음";

        void SetDataLine(Sheet s)
        {
            rawData = Words.DataLine(s);
            FitTicket();
        }

        string rawOrder = "", rawHint = "", rawData = "";
        void ReflowTicket()
        {
            if (cur == null || ph == Ph.Title) return;
            FitTicket();
        }

        void SetHint(string h)
        {
            rawHint = h;
            FitTicket();
        }

        bool ReadoutOn(Sheet s) => !s.Locked && (s.stage == 1 || s.no == 0);

        void RefreshSheet(bool full)
        {
            if (cur == null) return;
            if (full) { DrawPlate(cur, stampedShown); PlaceStops(cur); }
            UpdateKnobTag();                 // 이름표를 먼저 — 인화지 길이 글자가 이 자리를 피한다
            DrawGhost(cur, stampedShown);
            if (ReadoutOn(cur)) { string r = Words.Readout(cur); if (readTxt.text != r) readTxt.text = r; }
        }

        void UpdateKnobTag()
        {
            if (cur == null || cur.Locked) return;
            string t = cur.stage <= 2 || cur.no == 0 ? Words.GaugeName(cur) + "\n" + Words.Cm(cur.handle) + " cm" : Words.GaugeName(cur);
            if (knobTag.text != t) knobTag.text = t;
            knobTag.gameObject.SetActive(true);
            knobTag.rectTransform.sizeDelta = new Vector2(10, 3.4f);
            knobTag.rectTransform.pivot = new Vector2(1, 0.5f);
            knobTag.transform.localPosition = new Vector3(-2.4f, 1.6f, 0);
        }

        // ── 확정: 찍기 / 치우기. 학생 입력과 훅이 같은 함수를 탄다.
        void Commit(Act a)
        {
            if (sp != Sp.Work && sp != Sp.Enter) return;
            if (sp == Sp.Enter) { sp = Sp.Work; enterX = 0; }
            if (ph == Ph.Title) { lastAct = a; StartStamp(); return; }
            if (ph == Ph.Practice)
            {
                if (a == Act.Discard) { Refuse("지금은 손잡이로 닮음비를 맞추시오"); sheetDx = 0; return; }
                if (!Judge.InWindow(cur)) { BounceBack(); return; }
                lastAct = a; StartStamp();
                return;
            }
            bool ok = Judge.Correct(cur, a);
            bool first = !tried; tried = true;
            if (first) { st.attempts++; chanceSum += (float)Rules.Chance(cur); }
            lastAct = a;
            if (ok)
            {
                st.combo++; st.maxCombo = Mathf.Max(st.maxCombo, st.combo);
                int mult = Rules.Mult(st.combo);
                st.score += (a == Act.Stamp ? 100 : 80) * mult + (first ? 50 : 0);
                if (first) st.firstTry++;
                st.solved++; st.processed++;
                if (mult > 1 && (st.combo == 3 || st.combo == 6)) BaeyulSound.Play("combo", 0.8f);
                if (a == Act.Stamp) StartStamp(); else StartSpike(false);
            }
            else
            {
                st.combo = 0;
                LoseSpare();
                willRetry = first && st.spares > 0;
                if (!willRetry) st.processed++;
                sp = Sp.Wrong; spT = 0;
                ClearGuides();
                BaeyulSound.Play(a == Act.Stamp ? "smear" : "refuse", 1f);
                ShowToast(Words.RevealWrong(cur, a), 2.2f);
            }
            MgfBridge.NotifyChanged();
        }

        void LoseSpare()
        {
            int i = Rules.Spares - st.spares;
            st.spares = Mathf.Max(0, st.spares - 1); st.lives = st.spares;
            if (i >= 0 && i < 3) { spareCrumple[i].enabled = true; spare[i].localRotation = Quaternion.Euler(0, 0, -18); spare[i].localScale = new Vector3(0.86f, 0.8f, 1); MgfFx.Punch(spare[i], 0.25f, 0.3f); BaeyulSound.Play("crumple", 0.7f); }
        }

        void StartStamp()
        {
            sp = Sp.Stamp; spT = 0; platenShake = 0;
            ClearGuides();
            BaeyulSound.Play("slide", 0.5f, 0.7f);
        }

        void ClearGuides()
        {
            fxInk.Clear(); fxInk.Apply(); arrowT = 0;
            fingerT.gameObject.SetActive(false); pulseT.gameObject.SetActive(false);
        }

        void StartSpike(bool afterWrong)
        {
            ClearGuides();
            sp = Sp.Spike; spT = 0; spikeAfterWrong = afterWrong;
            BaeyulSound.Play("slide", 0.7f, 1.2f);
            if (!afterWrong && ph == Ph.Play) ShowToast(Words.RevealRight(cur, Act.Discard), 1.9f);
        }

        void BounceBack()
        {
            // 연습 중 비가 맞지 않았는데 찍었다: 플래튼이 튕겨 오르고 현재 비가 흔들린다(목숨 없음)
            platenDrag = 0; platenShake = 0.35f;
            BaeyulSound.Play("refuse", 0.8f);
            MgfFx.Punch(readRt, 0.12f, 0.4f);
            ShowToast("비가 다릅니다 · " + Words.Ratio(Judge.GaugeOrig(cur), cur.handle) + " ≠ " + Words.R(cur.m, cur.n), 1.6f);
            ghostT = 0; ghostScale = 1.3f;
        }

        void Refuse(string why)
        {
            jiggleT = 0.3f;
            BaeyulSound.Play("refuse", 0.8f);
            ShowToast(why, 1.5f);
            ghostT = 0; ghostScale = 1.4f;
        }

        void ShowCard(string big, string sub, float dur)
        {
            cardBig.text = big; cardSub.text = sub; cardT = dur; frozen = true;
            cardG.alpha = 1; MgfFx.Punch(cardRt, 0.06f, 0.3f);
        }

        void ShowToast(string s, float dur) { toastTxt.text = Wrap(s, toastRt.sizeDelta.x - 26, 14.5f); toastT = 0; toastDur = dur; toastG.alpha = 1; MgfFx.Punch(toastRt, 0.05f, 0.25f); }
        void HideToast() { toastG.alpha = 0; toastT = 9; toastDur = 0; }

        /// <summary>훅이 애니메이션 도중에 불렸을 때: 진행 중인 것을 즉시 끝낸다.</summary>
        void FinishPending()
        {
            for (int guard = 0; guard < 6 && ph == Ph.Play; guard++)
            {
                if (sp == Sp.Enter) { sp = Sp.Work; enterX = 0; }
                if (sp == Sp.Work) break;
                Resolve();
            }
            frozen = false; cardT = 0; cardG.alpha = 0; hitStop = 0;
        }

        void Resolve()
        {
            if (ph == Ph.Title) { demoLoop++; NewDemo(); sp = Sp.Work; return; }
            if (ph == Ph.Practice)
            {
                if (sp == Sp.Stamp) { EndPractice(); ShowCard("연습 끝", "주문 12장 · 90초 · 예비 인화지 3장\n틀리면 예비 인화지가 구겨진다", 2.2f); }
                return;
            }
            if (sp == Sp.Wrong)
            {
                if (st.spares <= 0) { EndRun("spent"); return; }
                if (willRetry)
                {
                    // 같은 장 한 번 더(콤보는 이미 끊겼고 예비는 돌아오지 않는다). 겹인화 얼룩은 흐리게 남는다
                    sp = Sp.Work; spT = 0; sheetDx = 0; platenDrag = 0; platenShake = 0;
                    stained = lastAct == Act.Stamp;
                    fxInk.Clear(); fxInk.Apply();
                    if (!stained) { impInk.Clear(); impInk.Apply(); }
                    else DrawImpression(cur, -999, 0, true, 0.4f);
                    sheetLeft = Mathf.Max(sheetLeft, 6f);
                    RefreshSheet(false);
                    return;
                }
                StartSpike(true);
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
            if (st.processed > best) { best = st.processed; PlayerPrefs.SetInt("baeyul.best", best); }
            bool newBest = st.score > bestScore;
            if (newBest) { bestScore = st.score; PlayerPrefs.SetInt("baeyul.bestScore", bestScore); }
            PlayerPrefs.Save();
            // 첫 시도 정답률이 우연 수준 이하면 처리 장수와 무관하게 「인화 실패」
            float rate = st.attempts > 0 ? (float)st.firstTry / st.attempts : 0f;
            float chance = st.attempts > 0 ? chanceSum / st.attempts : 0f;
            bool fail = st.attempts == 0 || rate <= chance;
            endHead.text = fail ? "인화 실패" : reason == "clear" ? "인화 완료" : reason == "time" ? "시간 종료" : "예비 인화지 소진";
            endHead.color = fail ? InkC : Verm;
            endBest.text = newBest && !fail ? "최고 점수 갱신" : "최고 점수 " + bestScore;
            endBest.color = newBest && !fail ? Verm : C(InkC, 0.75f);
            BaeyulSound.Play("end", 0.7f);
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
            if (!play) cardG.alpha = 0;
            sheetRoot.gameObject.SetActive(ph != Ph.End);
            UpdateReadoutVisible();
        }

        void UpdateReadoutVisible()
        {
            bool on = (ph == Ph.Practice || ph == Ph.Play) && cur != null && ReadoutOn(cur);
            if (readRt.gameObject.activeSelf != on) readRt.gameObject.SetActive(on);
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
            UpdateReadoutVisible();
        }

        void UpdateAmbient(float dt)
        {
            float t = Time.time;
            lampT.position = new Vector3(Mathf.Sin(t * 0.21f) * 12f, 11f, 4f + Mathf.Cos(t * 0.16f) * 7f);
            // 손잡이 위치(속도 상한으로 끌리는 표시값) · 숨 쉬는 손잡이
            if (cur != null && !cur.Locked)
            {
                float z = RailZf(handleVisMm) - railT.localPosition.z;
                float jx = jiggleT > 0 ? Mathf.Sin(jiggleT * 60f) * 0.25f : 0f;
                float bz = bobT > 0 ? Mathf.Sin(bobT * 38f) * bobT * 1.4f : 0f;
                knobT.localPosition = new Vector3(jx, 0, z + bz);
                float s = press == Press.Handle ? 1.12f : 1f + Mathf.Sin(t * 3f) * 0.03f;
                knobT.localScale = Vector3.Lerp(knobT.localScale, new Vector3(s, 1, s), 1f - Mathf.Exp(-dt * 18f));
            }
            if (jiggleT > 0) jiggleT -= dt;
            if (bobT > 0) bobT = Mathf.Max(0, bobT - dt);
            // 플래튼: 쉬는 자리 − 끌기/찍기 이동 + 흔들림
            float shakeX = platenShake > 0 ? Mathf.Sin(platenShake * 70f) * platenShake * 1.2f : 0f;
            if (platenShake > 0) platenShake = Mathf.Max(0, platenShake - dt);
            platenZ = platenRestZ - platenDrag;
            platenT.localPosition = new Vector3(zoneC.x + sheetOff.x + shakeX, 0, platenZ);
            float lblA = 0.65f + 0.35f * Mathf.Sin(t * 2.6f);
            platenLbl.color = C(Brass, (sp == Sp.Work && cur != null) ? lblA : 0.4f);
        }

        void UpdateToast(float dt)
        {
            if (toastDur <= 0) return;
            toastT += dt;
            if (toastT > toastDur) { toastG.alpha = Mathf.Max(0, 1 - (toastT - toastDur) / 0.3f); if (toastG.alpha <= 0) toastDur = 0; }
        }

        // ── 타이틀: 같은 교정대 위 데모(유령 손가락이 손잡이를 2:3 까지 → 플래튼이 쓸고 지나가며 주홍 잉크가 번진다)
        void UpdateTitle(float dt)
        {
            titleT += dt; logoT += dt;
            for (int i = 0; i < 4; i++)
            {
                float k = Mathf.Clamp01((logoT - 0.2f - i * 0.12f) / 0.3f);
                float s = k <= 0 ? 0 : (k < 1 ? 1.35f - 0.35f * BackOut(k) : 1f);
                float sq = k < 1 && k > 0 ? 1f + 0.18f * Mathf.Sin(k * Mathf.PI) : 1f;
                logo[i].rectTransform.localScale = new Vector3(s * sq, s / sq, 1);
                logoGhost[i].rectTransform.localScale = logo[i].rectTransform.localScale;
                if (k > 0 && k - dt / 0.3f <= 0) BaeyulSound.Play("tick", 0.4f, 0.6f + i * 0.1f);
            }
            logoInk.rectTransform.localScale = Vector3.one * (0.9f + 0.1f * Mathf.Clamp01((logoT - 0.5f) / 0.6f) + Mathf.Sin(Time.time * 1.3f) * 0.012f);
            float br = 1f + Mathf.Sin(Time.time * 2.4f) * 0.02f;
            ctaRt.localScale = Vector3.Lerp(ctaRt.localScale, Vector3.one * br * ctaBase, 1f - Mathf.Exp(-dt * 20f));
            ctaFace.offsetMin = new Vector2(0, press == Press.Cta ? 2 : 7); ctaFace.offsetMax = new Vector2(0, press == Press.Cta ? -5 : 0);
            float sh = (Time.time % 2.6f) / 2.6f;
            ctaShine.anchoredPosition = new Vector2(-40 + sh * 380, 0);
            // 데모 조작(입력 경로가 아니다 — 연출용 자동 재생)
            if (sp == Sp.Enter) { sp = Sp.Work; spT = 0; }
            if (sp == Sp.Work)
            {
                float t = spT;
                fingerT.gameObject.SetActive(t > 0.2f && t < 2.4f);
                float kz = Smooth(Mathf.Clamp01((t - 0.45f) / 1.2f));
                float mm = Mathf.Lerp(40, 60, kz);
                handleVisMm = mm;
                int q = Mathf.RoundToInt(mm / 5f) * 5;
                if (q != cur.handle) { cur.handle = q; RefreshSheet(false); BaeyulSound.Play("tick", 0.3f); }
                if (t < 1.8f) fingerT.position = new Vector3(railX + 0.4f, 3f, RailZf(handleVisMm) - 0.4f);
                else
                {
                    float k2 = Smooth(Mathf.Clamp01((t - 1.8f) / 0.5f));
                    fingerT.position = new Vector3(zoneC.x + 2f, 3f, platenRestZ - k2 * 3f);
                    platenDrag = k2 * 3f;
                }
                if (t >= 2.35f) { fingerT.gameObject.SetActive(false); lastAct = Act.Stamp; StartStamp(); }
            }
            HandleTitleTaps();
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) BeginGame();
        }

        void BeginGame()
        {
            BaeyulSound.Play("press", 0.8f);
            BaeyulSound.Play("thunk", 0.6f);
            fingerT.gameObject.SetActive(false);
            camPress = 1f;
            StartRun(!practiceSeen);
        }

        static float BackOut(float k) { float c1 = 1.70158f, c3 = c1 + 1; return 1 + c3 * Mathf.Pow(k - 1, 3) + c1 * Mathf.Pow(k - 1, 2); }
        static float Smooth(float k) => k * k * (3 - 2 * k);
        static float ElasticOut(float k) => k <= 0 ? 0 : k >= 1 ? 1 : Mathf.Pow(2, -10 * k) * Mathf.Sin((k * 10 - 0.75f) * (2 * Mathf.PI / 3)) + 1;

        void HandleTitleTaps()
        {
            if (MgfPointer.Down)
            {
                downScreen = MgfPointer.Position;
                press = Hit(ctaRt, downScreen) ? Press.Cta : Press.None;
                if (press == Press.None) { Ripple(downScreen, C(Verm, 0.7f)); BaeyulSound.Play("ripple"); st.touches++; MgfBridge.NotifyChanged(); }
            }
            if (MgfPointer.Up)
            {
                // 주 CTA 는 「찍어 보기」 하나지만, 타이틀 = 교정대라 어디를 눌러도 판이 열린다(무반응 탭 없음)
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
                if (cardT <= 0) frozen = false;
                cardG.alpha = Mathf.Clamp01(cardT / 0.25f);
            }
            bool ticking = ph == Ph.Play && !frozen && (sp == Sp.Work || sp == Sp.Enter);
            if (ticking)
            {
                runLeft -= dt;
                if (sp == Sp.Work) sheetLeft -= dt;
                if (runLeft <= 0) { runLeft = 0; EndRun("time"); return; }
                if (sp == Sp.Work && sheetLeft <= 0) { Timeout(); return; }
            }
            if (ph == Ph.Practice && sp == Sp.Work && !frozen && press == Press.None)
            {
                // 연습은 게임이 대신 맞춰 주지 않는다(3차 검수 high): 손잡이를 끌거나 레일 위·아래를 탭해 비를 맞추고,
                // 플래튼을 끌어 내리거나 탭해야 끝난다. 20초 안에 못 끝내면 풀어 주지 않고 조작 요약 카드만 남긴 채 본판으로 넘긴다(갇힘 방지).
                practiceT += dt;
                if (practiceT > 20f)
                {
                    EndPractice();
                    ShowCard("연습은 여기까지", "손잡이를 위아래로 끌어 비를 맞추고\n플래튼을 아래로 끌어 내려 찍는다", 2.6f);
                    return;
                }
            }
            if (sp == Sp.Work && !frozen) HandleWorkInput(dt);
            else if (MgfPointer.Down) { Ripple(MgfPointer.Position, C(InkC, 0.5f)); st.touches++; MgfBridge.NotifyChanged(); }
            UpdateGuides(dt);
        }

        void Timeout()
        {
            if (!tried) { st.attempts++; chanceSum += (float)Rules.Chance(cur); }
            tried = true;
            st.combo = 0; st.processed++;
            LoseSpare();
            sp = Sp.Timeout; spT = 0;
            press = Press.None;
            BaeyulSound.Play("crumple", 1f);
            ShowToast("장당 " + cur.limitSec + "초가 지났다 · 인화지가 구겨져 꽂혔다", 1.4f);
            MgfBridge.NotifyChanged();
        }

        float PxPerCm => Screen.height / (2f * cam.orthographicSize);
        float ScaleF => canvas ? canvas.scaleFactor : 1f;

        bool PointerWorld(out Vector2 w)
        {
            var ray = cam.ScreenPointToRay(MgfPointer.Position);
            var plane = new Plane(Vector3.up, Vector3.zero);
            w = default;
            if (!plane.Raycast(ray, out float d)) return false;
            var p = ray.GetPoint(d); w = new Vector2(p.x, p.z);
            return true;
        }

        bool OnRail(Vector2 w) => Mathf.Abs(w.x - railX) < Mathf.Max(3.0f, 44f * ScaleF / PxPerCm) && w.y > railZ0 - 2.2f && w.y < railZ1 + 2.2f;
        bool OnPlaten(Vector2 w) => Mathf.Abs(w.y - platenZ) < Mathf.Max(1.5f, 24f * ScaleF / PxPerCm) && Mathf.Abs(w.x - (zoneC.x + sheetOff.x)) < sheetW * 0.5f + 0.6f;
        bool OnSheet(Vector2 w) { var l = w - zoneC - sheetOff; return Mathf.Abs(l.x) < sheetW * 0.5f && Mathf.Abs(l.y) < sheetH * 0.5f; }

        void HandleWorkInput(float dt)
        {
            // 키보드: ↑/↓ 손잡이 한 칸 · Space 찍기 · ← 치우기
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) { Commit(Act.Stamp); return; }
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.Backspace)) { Commit(Act.Discard); return; }
            if (!cur.Locked && (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow)))
            {
                int d = Input.GetKeyDown(KeyCode.UpArrow) ? Rules.Step : -Rules.Step;
                SetHandle(Mathf.Clamp(cur.handle + d, cur.railLo, cur.railHi)); handleVisMm = cur.handle;
            }

            if (MgfPointer.Down && PointerWorld(out var w))
            {
                downScreen = MgfPointer.Position; idleT = 0; committedDrag = false;
                downWorld = w; downWorldOk = true;
                if (OnRail(w))
                {
                    if (cur.Locked) { press = Press.Other; Refuse("이 장은 레일이 고정되어 있다 · 닮음이면 ↓ 찍고, 아니면 ← 치우시오"); Ripple(downScreen, C(InkC, 0.7f)); }
                    else { press = Press.Handle; grabOffZ = Mathf.Abs(w.y - RailZf(handleVisMm)) < 2.2f ? RailZf(handleVisMm) - w.y : 0f; BaeyulSound.Play("tick", 0.5f); }
                }
                else if (OnPlaten(w)) { press = Press.Platen; platenGrab = w.y + platenDrag; }
                else if (OnSheet(w)) press = Press.Sheet;
                else press = Press.Other;
                st.touches++; MgfBridge.NotifyChanged();
            }
            if (MgfPointer.Held && press != Press.None && PointerWorld(out var hw))
            {
                if (press == Press.Handle)
                {
                    float target = Mathf.Clamp(RailMm(hw.y + grabOffZ), cur.railLo, cur.railHi);
                    handleVisMm = Mathf.MoveTowards(handleVisMm, target, Rules.HandleSpeed * dt);
                    int q = Mathf.Clamp(Mathf.RoundToInt(handleVisMm / Rules.Step) * Rules.Step, cur.railLo, cur.railHi);
                    if (q != cur.handle) SetHandle(q);
                }
                else if (press == Press.Platen && !committedDrag)
                {
                    platenDrag = Mathf.Clamp(platenGrab - hw.y, 0, 6f);
                    if (platenDrag > 3.2f) { committedDrag = true; press = Press.None; Commit(Act.Stamp); return; }
                }
                else if (press == Press.Sheet)
                {
                    float dx = (MgfPointer.Position.x - downScreen.x) / PxPerCm;
                    sheetDx = Mathf.Clamp(dx * 0.6f, -5f, 0.8f);
                }
            }
            if (MgfPointer.Up && press != Press.None)
            {
                var up = MgfPointer.Position;
                var dl = (up - downScreen) / ScaleF;
                var pr = press; press = Press.None;
                if (pr == Press.Handle)
                {
                    handleVisMm = cur.handle;
                    // 탭(끌지 않고 뗌): 손잡이가 한 칸(5 mm) 움직인다. 누르고만 떼면 아무 일도 없던 결함(첫 플레이 44초 정체).
                    // 레일의 빈 곳을 탭하면 그쪽으로 한 칸 · 손잡이 자체를 탭하면 연습 장에선 맞출 길이 쪽으로 한 칸,
                    // 본 주문에선 방향을 알려 주지 않도록 「끌어라」 안내만 한다(정답 누설 금지).
                    // Up 시점에 레이를 다시 쏘지 않는다 — 빗나가면 탭 한 칸이 통째로 사라지던 결함(2차 검수 high)
                    if (dl.magnitude < 18f && downWorldOk)
                    {
                        var tw = downWorld;
                        float kz = RailZf(cur.handle);
                        int dir = 0;
                        // 연습·본판 같은 규칙: 손잡이 위를 탭하면 한 칸 위, 아래를 탭하면 한 칸 아래(게임이 방향을 정해 주지 않는다)
                        if (Mathf.Abs(tw.y - kz) > 1.3f) dir = tw.y > kz ? 1 : -1;
                        if (dir != 0)
                        {
                            int q = Mathf.Clamp(cur.handle + dir * Rules.Step, cur.railLo, cur.railHi);
                            if (q != cur.handle) { SetHandle(q); handleVisMm = q; }
                            Ripple(up, C(Brass, 0.9f));
                            if (ph == Ph.Practice && !Judge.InWindow(cur)) { arrowT = 0; ghostT = 0; ghostScale = 1.5f; ShowToast("현재 비 " + Words.Ratio(Judge.GaugeOrig(cur), cur.handle) + " · 목표 " + Words.R(cur.m, cur.n) + " · 끌면 한 번에 간다", 1.4f); }
                        }
                        else
                        {
                            // 손잡이 자체를 탭: 위·아래 양쪽을 똑같이 가리키는 ▲▼ 표시 + 손잡이가 위아래로 크게 들썩인다(방향 누설 없음)
                            Ripple(up, C(InkC, 0.9f)); Ripple(up, C(Verm, 0.6f)); ghostT = 0; ghostScale = 1.5f;
                            railCueT = 3.2f; bobT = 0.5f; MgfFx.Punch(readRt, 0.1f, 0.3f);
                            ShowToast("손잡이를 위아래로 끌거나, 레일 위·아래를 탭하시오", 1.4f);
                        }
                    }
                }
                else if (pr == Press.Sheet || pr == Press.Other)
                {
                    if (dl.y < -55f && Mathf.Abs(dl.y) > Mathf.Abs(dl.x) * 1.2f) { Commit(Act.Stamp); return; }
                    if (dl.x < -60f && Mathf.Abs(dl.x) > Mathf.Abs(dl.y) * 1.2f) { Commit(Act.Discard); return; }
                    if (dl.magnitude < 18f && downWorldOk)
                    {
                        var tp = downWorld;
                        // 「← 치우기」 스파이크를 탭하면 치운다(끌기와 같은 확정 — 탭만 하는 사람도 막히지 않게)
                        if ((tp - spikeP).magnitude < 2.6f) { Ripple(up, C(InkC, 0.7f)); Commit(Act.Discard); return; }
                        Tap(tp, up);
                    }
                }
                else if (pr == Press.Platen)
                {
                    // 플래튼 탭 = 찍기(끌어 내리기와 같은 확정). 탭만 하는 순진 입력이 연습 장에 갇히던 결함의 두 번째 절반.
                    if (dl.magnitude < 14f)
                    {
                        Ripple(up, C(Brass, 0.9f));
                        // 연습 장에서 비가 안 맞은 채 플래튼을 탭하면: 튕기면서 손잡이가 한 칸 맞출 쪽으로 간다(갇힘 방지)
                        if (ph == Ph.Practice && !Judge.InWindow(cur)) { BounceBack(); railCueT = 3.2f; bobT = 0.5f; return; }
                        Commit(Act.Stamp); return;
                    }
                }
            }
            if (press != Press.Platen) platenDrag = Mathf.Lerp(platenDrag, 0, 1f - Mathf.Exp(-dt * 14f));
            if (press != Press.Sheet) sheetDx = Mathf.Lerp(sheetDx, 0, 1f - Mathf.Exp(-dt * 14f));
            idleT += dt;
        }

        void SetHandle(int q)
        {
            cur.handle = q;
            RefreshSheet(false);
            BaeyulSound.Play("tick", 0.5f, 0.85f + (q % 40) * 0.008f);
            MgfBridge.NotifyChanged();
            bool inW = Judge.InWindow(cur);
            if (ph == Ph.Practice)
            {
                int want = inW ? 1 : 0;
                if (want != obStep)
                {
                    obStep = want;
                    if (inW) BaeyulSound.Play("lock", 0.8f);
                    SetHint(inW ? "이제 플래튼을 아래로 끌어 내려 찍으시오 ↓" : "손잡이를 끌어 DE를 맞추시오");
                    ghostT = 0;
                }
            }
            inWindowPrev = inW;
        }

        void Tap(Vector2 w, Vector2 screen)
        {
            // 빈 곳·인화지 탭: 리플 + 손잡이(구성 장) 또는 플래튼·스파이크(판별 장) 쪽 화살표
            Ripple(screen, C(InkC, 0.7f));
            BaeyulSound.Play("ripple");
            arrowT = 0.8f; arrowFrom = w;
            if (!cur.Locked) { railCueT = 3.2f; bobT = 0.45f; }
            if (ph == Ph.Practice) { ghostT = 0; ghostScale = 1.3f; ShowToast("손잡이를 위아래로 끌어 " + Words.GaugeName(cur) + "를 맞추시오", 1.2f); }
            else if (!cur.Locked && sp == Sp.Work) ShowToast("손잡이로 " + Words.GaugeName(cur) + "를 맞춘 뒤 아래로 끌어 찍으시오", 1.2f);
            else if (cur.Locked) ShowToast("닮음이면 아래로 끌어 찍고, 아니면 왼쪽으로 밀어 치우시오", 1.2f);
        }

        /// <summary>유령 손가락·화살표·정답 윤곽(뭔가 누르면 항상 무언가가 움직인다).</summary>
        void UpdateGuides(float dt)
        {
            bool fxDirty = false;
            if (arrowT > 0 && sp == Sp.Work)
            {
                arrowT -= dt;
                Vector2 target = cur.Locked ? new Vector2(zoneC.x, platenZ) : new Vector2(railX, RailZf(handleVisMm));
                var d = target - arrowFrom; float L = d.magnitude;
                fxInk.Clear();
                if (L > 2f && arrowT > 0)
                {
                    var dir = d / L; float k = 1f - arrowT / 0.8f;
                    var o = arrowFrom - zoneC;
                    fxInk.Chevron(o + dir * (0.8f + k * 1.6f), dir, 0.9f, 0.16f, C32(InkC, 0.85f * (1 - k * 0.5f)));
                    fxInk.Chevron(o + dir * (1.8f + k * 1.6f), dir, 0.9f, 0.16f, C32(Verm, 0.7f * (1 - k * 0.5f)));
                }
                fxDirty = true;
            }
            bool pulse = false;
            if (ph == Ph.Practice && sp == Sp.Work && press == Press.None && cardT <= 0)
            {
                if (idleT > 8f) { idleT = 0; ghostT = 0; ghostScale = 1.4f; }
                ghostT += dt;
                if (obStep == 0)
                {
                    float cyc = ghostT % 2.2f, k = Smooth(Mathf.Clamp01((cyc - 0.3f) / 1.2f));
                    float z0 = RailZf(cur.handle), z1 = RailZf(Judge.Target(cur));
                    fingerT.gameObject.SetActive(cyc < 1.9f);
                    fingerT.position = new Vector3(railX + 0.5f, 3f, Mathf.Lerp(z0, z1, k) - 0.5f);
                    fingerT.localScale = Vector3.one * ghostScale * (cyc < 0.3f ? 1.15f - cyc * 0.5f : 1f);
                    fxInk.Clear();
                    var a = new Vector2(railX - 2.4f, z0) - zoneC; var b = new Vector2(railX - 2.4f, z1) - zoneC;
                    fxInk.Dashed(a, b, 0.35f, 0.25f, 0.1f * ghostScale, C32(InkC, 0.75f), -Time.time * 1.4f);
                    fxInk.Chevron(b - new Vector2(0, 0.2f) * Mathf.Sign(z1 - z0), new Vector2(0, Mathf.Sign(z1 - z0)), 0.7f * ghostScale, 0.12f, C32(InkC, 0.8f));
                    fxDirty = true;
                    pulse = true;
                    pulseT.position = new Vector3(railX, 2.2f, RailZf(handleVisMm));
                }
                else
                {
                    float cyc = ghostT % 1.6f, k = Smooth(Mathf.Clamp01(cyc / 1.0f));
                    fingerT.gameObject.SetActive(cyc < 1.2f);
                    fingerT.position = new Vector3(zoneC.x + 2.5f, 3f, platenRestZ - k * 4f);
                    fingerT.localScale = Vector3.one * ghostScale;
                    fxInk.Clear();
                    for (int i = 0; i < 3; i++)
                    {
                        float p2 = (Time.time * 1.6f + i / 3f) % 1f;
                        fxInk.Chevron(new Vector2(2.5f, platenRestZ - zoneC.y - 1.2f - p2 * 4f), Vector2.down, 1.2f * ghostScale, 0.16f, C32(InkC, 0.9f * (1 - p2)));
                    }
                    fxDirty = true;
                    pulse = true;
                    pulseT.position = new Vector3(zoneC.x + 2.5f, 2.2f, platenZ);
                }
            }
            else if (ph != Ph.Title) fingerT.gameObject.SetActive(false);
            // 손잡이 ▲▼: 빈 탭·손잡이 탭 직후, 또는 구성 장에서 6초 넘게 손잡이를 안 건드렸을 때 — 위·아래를 똑같이 가리킨다(정답 방향 누설 없음)
            if (railCueT > 0) railCueT -= dt;
            bool cue = cur != null && !cur.Locked && sp == Sp.Work && press == Press.None && cardT <= 0 && ph != Ph.Title
                       && (railCueT > 0 || (ph == Ph.Play && idleT > 6f)) && !(ph == Ph.Practice && obStep == 1);
            if (cue || cueWas)
            {
                if (!fxDirty) fxInk.Clear();
                if (cue)
                {
                    float kz = RailZf(handleVisMm) - zoneC.y, kx = railX - 1.9f - zoneC.x;
                    float bob = Mathf.Abs(Mathf.Sin(Time.time * 5f)) * 0.5f;
                    fxInk.Chevron(new Vector2(kx, kz + 2.3f + bob), Vector2.up, 1.5f, 0.32f, C32(InkC, 0.95f));
                    fxInk.Chevron(new Vector2(kx, kz + 3.4f + bob), Vector2.up, 1.5f, 0.32f, C32(InkC, 0.55f));
                    fxInk.Chevron(new Vector2(kx, kz - 2.3f - bob), Vector2.down, 1.5f, 0.32f, C32(Verm, 0.95f));
                    fxInk.Chevron(new Vector2(kx, kz - 3.4f - bob), Vector2.down, 1.5f, 0.32f, C32(Verm, 0.55f));
                    if (!pulse) { pulse = true; pulseT.position = new Vector3(railX, 2.2f, RailZf(handleVisMm)); }
                }
                fxDirty = true;
            }
            cueWas = cue;
            pulseT.gameObject.SetActive(pulse);
            if (pulse) { float k = (Time.time * 1.1f) % 1f; pulseT.localScale = Vector3.one * (1.3f + k * 1.6f); }
            if (fxDirty) fxInk.Apply();
        }

        // ── 인화지 애니메이션(입장 · 찍기 · 치우기 · 오답 · 시간 초과)
        float SweepBottomLocal => -sheetH * 0.5f + sheetOff.y - 0.5f;

        void UpdateSheetAnim(float dt)
        {
            spT += dt;
            float sheetSpikeX = 0, sheetSpikeZ = 0;
            switch (sp)
            {
                case Sp.Enter:
                    {
                        float k = Mathf.Clamp01(spT / 0.45f);
                        enterX = 30f * (1f - BackOut(k));
                        if (k >= 1) { sp = Sp.Work; spT = 0; enterX = 0; }
                        break;
                    }
                case Sp.Stamp:
                    {
                        // 0~0.24 플래튼이 교정 롤러처럼 위에서 아래로 쓸고 지나간다(지나간 곳까지 주홍 인상) → 0.08초 정지 →
                        // 화면이 눌렸다 탄성 복귀 · 잉크가 윤곽 밖으로 번지고 대응 빗금이 잠긴다 → 플래튼 복귀 → 인화지가 오른쪽으로 나간다
                        float travel = platenRestZ - (zoneC.y + SweepBottomLocal);
                        if (spT < 0.24f)
                        {
                            float k = spT / 0.24f; k = k * k;
                            platenDrag = Mathf.Lerp(Mathf.Min(platenDrag, travel), travel, k);
                            ComputeImage(cur, cur.handle);
                            DrawImpression(cur, platenZ - zoneC.y - 0.9f, 0, false, 1f);
                        }
                        else if (!stampedShown)
                        {
                            stampedShown = true; hitStop = 0.08f; camPress = 1f;
                            BaeyulSound.Play("thunk", 1f); BaeyulSound.Play("ink", 0.8f);
                            DrawImpression(cur, -999, 0, false, 1f);
                            RefreshSheet(true);                       // 빗금 잠김(원본·인상 둘 다) · 미리보기 윤곽 걷힘
                            DrawLockTicks();
                            if (ph == Ph.Play || ph == Ph.Practice)
                            {
                                BaeyulSound.Play("good", 0.8f);
                                ShowToast(ph == Ph.Practice ? "AB:DE = 4 cm:6 cm = 2:3\nBC:EF = 6 cm:9 cm = 2:3\n대응변의 비가 모두 같다 → 닮음비 2:3" : Words.RevealRight(cur, Act.Stamp), ph == Ph.Practice ? 2.6f : 1.9f);
                                if (ph == Ph.Practice) SetHint("");
                            }
                        }
                        else
                        {
                            float k = Mathf.Clamp01((spT - 0.24f) / 0.6f);
                            DrawImpression(cur, -999, 0.35f * Smooth(k), false, 1f);
                            if (spT > 0.4f) platenDrag = Mathf.Lerp(platenDrag, 0, 1f - Mathf.Exp(-dt * 8f));
                            float hold = ph == Ph.Practice ? 2.6f : ph == Ph.Title ? 1.6f : 1.5f;
                            if (spT > 0.84f + hold)
                            {
                                float e = Mathf.Clamp01((spT - 0.84f - hold) / 0.35f);
                                enterX = 34f * e * e;
                                if (e >= 1) Resolve();
                            }
                        }
                        break;
                    }
                case Sp.Spike:
                    {
                        // 인화지가 왼쪽 스파이크 파일로 날아가 꽂힌다
                        float k = Mathf.Clamp01(spT / 0.42f);
                        var d = spikeP - zoneC - sheetOff;
                        sheetSpikeX = d.x * k * k; sheetSpikeZ = d.y * k * k;
                        sheetScale = Mathf.Lerp(1f, 0.16f, k); sheetRot = -35f * k;
                        if (spT >= 0.42f && spT - dt < 0.42f) { BaeyulSound.Play("spike", 0.9f); SpikeOne(); }
                        if (spT > (spikeAfterWrong ? 0.6f : 1.3f)) Resolve2();
                        break;
                    }
                case Sp.Wrong:
                    {
                        if (lastAct == Act.Stamp)
                        {
                            // 어긋난 찍기: 플래튼이 비틀거리며 쓸고 지나가 겹인화가 남는다
                            float travel = platenRestZ - (zoneC.y + SweepBottomLocal);
                            if (spT < 0.26f)
                            {
                                float k = spT / 0.26f; k = k * k;
                                platenDrag = Mathf.Lerp(Mathf.Min(platenDrag, travel), travel, k);
                                platenShake = 0.25f;
                                ComputeImage(cur, cur.handle);
                                DrawImpression(cur, platenZ - zoneC.y - 0.9f, 0, true, 1f);
                            }
                            else
                            {
                                if (spT - dt < 0.26f) { camShake = 0.3f; DrawImpression(cur, -999, 0, true, 1f); }
                                platenDrag = Mathf.Lerp(platenDrag, 0, 1f - Mathf.Exp(-dt * 8f));
                            }
                        }
                        else
                        {
                            // 치우면 안 되는 장을 치웠다: 스파이크 쪽으로 끌려가다 튕겨 돌아온다
                            float k = Mathf.Clamp01(spT / 0.5f);
                            sheetDx = -5f * Mathf.Sin(k * Mathf.PI) * (1 - k * 0.4f);
                            if (spT - dt <= 0 && spT > 0) camShake = 0.22f;
                        }
                        if (spT > 0.5f && spT - dt <= 0.5f) ShowRightOutline();
                        if (spT > 2.3f) Resolve();
                        break;
                    }
                case Sp.Timeout:
                    {
                        float k = Mathf.Clamp01(spT / 0.6f);
                        var d = spikeP - zoneC - sheetOff;
                        sheetSpikeX = d.x * k * k; sheetSpikeZ = d.y * k * k;
                        sheetScale = Mathf.Lerp(1f, 0.14f, k); sheetRot = 80f * k;
                        if (spT >= 0.6f && spT - dt < 0.6f) SpikeOne();
                        if (spT > 1.2f) Resolve();
                        break;
                    }
            }
            sheetRoot.localPosition = new Vector3(zoneC.x + enterX + sheetDx + sheetSpikeX, 0, zoneC.y + sheetSpikeZ);
            sheetRoot.localRotation = Quaternion.Euler(0, sheetRot, 0);
            sheetRoot.localScale = new Vector3(sheetScale, 1, sheetScale);
        }

        void Resolve2()
        {
            sheetScale = 1f; sheetRot = 0;
            if (spikeAfterWrong) { NextSheet(); return; }
            Resolve();
        }

        void SpikeOne()
        {
            if (spiked < spikeSheets.Length) spikeSheets[spiked].gameObject.SetActive(true);
            spiked = Mathf.Min(spiked + 1, spikeSheets.Length);
            MgfFx.Punch(spikeT, 0.12f, 0.25f);
        }

        /// <summary>정답 윤곽(주홍 점선) — 오답 뒤 정답 상태를 보여 준다(구성 장).</summary>
        void ShowRightOutline()
        {
            fxInk.Clear();
            if (cur.kind == Kind.Build)
            {
                Place(OrigShape(cur), cur.N, Judge.Target(cur) / (float)Judge.GaugeOrig(cur), cur.rot, cur.flip, Vector2.zero, TV);
                for (int i = 0; i < cur.N; i++) fxInk.Dashed(TV[i], TV[(i + 1) % cur.N], 0.45f, 0.22f, i == cur.gauge ? 0.2f : 0.12f, C32(Verm, 0.95f), 0);
            }
            fxInk.Apply();
        }

        /// <summary>정답 순간: 인상 위에 대응변 빗금(원본과 같은 수) — 한 무늬로 잠긴다.</summary>
        void DrawLockTicks()
        {
            ghostInk.Clear();
            var cream = C32(Hex("FFE7D0"));
            if (cur.kind == Kind.Build) for (int i = 0; i < cur.N; i++) Ticks(ghostInk, IV[i], IV[(i + 1) % cur.N], TickCount(cur, i), cream);
            ghostInk.Outline(IV, cur.N, 0.08f, C32(Hex("7F1010")));
            AngleMarks(ghostInk, cur, IV, false, cream);
            ghostInk.Apply();
            if (cur.kind == Kind.Judge) return;
            for (int i = 0; i < 4; i++) iLen[i].gameObject.SetActive(false);
            gaugeLbl.gameObject.SetActive(false);
            if (cur.kind == Kind.Build)
                for (int i = 0; i < cur.N; i++)
                {
                    if (cur.shape == Shape.Rect && i >= 2) continue;
                    if (cur.shape == Shape.TriBH && i != 1) continue;
                    SetLen(iLen[i], IV, cur.N, i, Words.CmR((long)cur.os[i] * cur.handle, Judge.GaugeOrig(cur)) + " cm");
                }
        }

        // ── 끝 화면: 결과 카운트업 · 다시 하기 1탭
        string endStr;
        void UpdateEnd(float dt)
        {
            endT += dt;
            endG.alpha = Mathf.Clamp01(endT / 0.3f);
            endRt.localScale = Vector3.one * (0.9f + 0.1f * BackOut(Mathf.Clamp01(endT / 0.4f)));
            float k = Mathf.Clamp01((endT - 0.3f) / 1.0f);
            int proc = Mathf.RoundToInt(st.processed * k), ft = Mathf.RoundToInt(st.firstTry * k), sc = Mathf.RoundToInt(st.score * k);
            int pct = st.attempts > 0 ? Mathf.RoundToInt(100f * st.firstTry / st.attempts * k) : 0;
            int cpct = st.attempts > 0 ? Mathf.RoundToInt(100f * chanceSum / st.attempts) : 0;
            string s = "처리한 장  " + proc + " / 12\n첫 시도 정답  " + ft + " / " + st.attempts + "  (" + pct + "%)\n우연 수준  " + cpct + "%\n최장 콤보  " + st.maxCombo + "\n점수  " + sc;
            if (s != endStr) { endStr = s; endStats.text = s; }
            if (MgfPointer.Down)
            {
                downScreen = MgfPointer.Position;
                press = Hit(endCtaRt, downScreen) ? Press.EndCta : Press.None;
                if (press == Press.None) { Ripple(downScreen, C(InkC, 0.7f)); st.touches++; MgfBridge.NotifyChanged(); }
            }
            endCtaRt.localScale = Vector3.one * (press == Press.EndCta ? 0.94f : 1f + Mathf.Sin(Time.time * 2.4f) * 0.02f);
            if (MgfPointer.Up)
            {
                if (press == Press.EndCta && Hit(endCtaRt, MgfPointer.Position) && endT > 0.5f) { BaeyulSound.Play("press"); StartRun(false); }
                press = Press.None;
            }
            if (endT > 0.8f && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))) StartRun(false);
        }

        // ── HUD
        void UpdateHud(float dt)
        {
            if (ph == Ph.Title) return;
            int ti = Mathf.CeilToInt(runLeft);
            if (ti != lastTimeInt) { lastTimeInt = ti; timeTxt.text = ti + "<size=60%> 초</size>"; if (ti <= 10 && ph == Ph.Play) MgfFx.Punch(timeTxt.transform, 0.12f, 0.2f); }
            timeTxt.color = runLeft <= 10 ? Verm : InkC;
            timeBarFill.localScale = new Vector3(Mathf.Clamp01(runLeft / Rules.RunSec), 1, 1);
            sheetBarFill.localScale = new Vector3(cur != null && cur.limitSec > 0 ? Mathf.Clamp01(sheetLeft / cur.limitSec) : 1f, 1, 1);
            if (shownScore < st.score) shownScore = Mathf.Min(st.score, shownScore + Mathf.Max(60f, (st.score - shownScore) * 6f) * dt);
            else shownScore = st.score;
            int si = Mathf.RoundToInt(shownScore);
            if (si != shownScoreInt) { if (shownScoreInt >= 0 && si > shownScoreInt && si == st.score) MgfFx.Punch(scoreTxt.transform, 0.2f, 0.28f); shownScoreInt = si; scoreTxt.text = si.ToString(); }
            string cn = ph == Ph.Practice ? "연습" : "주문 " + Mathf.Min(12, deckIdx + 1) + " / 12";
            if (cn != counterTxt.text) counterTxt.text = cn;
            string cb = st.combo >= 2 ? "콤보 " + st.combo + (Rules.Mult(st.combo) > 1 ? "  ×" + Rules.Mult(st.combo) : "") : "";
            if (cb != comboTxt.text) comboTxt.text = cb;
        }

        // ── 카메라: 판형에 맞춰 교정대를 빈 화면 한가운데에 둔다(정사영이라 길이 왜곡 없음). 찍는 순간 눌렸다 탄성 복귀
        bool camInit = true;
        float baseOrtho = 16f, camShake;
        Vector2 camBase;
        void UpdateCamera(float dt)
        {
            float W = canvasRt.rect.width, H = canvasRt.rect.height, s = ScaleF;
            float top, bottom, left, right;
            if (ph == Ph.Title)
            {
                if (titleLand) { top = 16; bottom = 10; left = Mathf.Min(Mathf.Max(W * 0.4f, titlePosterW + 10), 560); right = 8; }
                else { top = 232; bottom = 168; left = 4; right = 4; }
            }
            else { top = insetTop; bottom = insetBottom; left = insetLeft; right = insetRight; }
            float x0 = wx0, x1 = wx1, z0 = wz0, z1 = wz1;

            float fw = Mathf.Max(50, (W - left - right) * s), fh = Mathf.Max(50, (H - top - bottom) * s);
            float k = Mathf.Min(fw / (x1 - x0), fh / (z1 - z0));
            float fcx = (left + (W - left - right) * 0.5f) * s, fcy = (bottom + (H - top - bottom) * 0.5f) * s;
            float ortho = Screen.height / (2f * k);
            float cx = (x0 + x1) * 0.5f - (fcx - Screen.width * 0.5f) / k;
            float cz = (z0 + z1) * 0.5f - (fcy - Screen.height * 0.5f) / k;
            float a = camInit ? 1f : 1f - Mathf.Exp(-dt * 10f);
            camInit = false;
            // 찍는 순간: 화면이 약 8 px 눌렸다가 탄성 복귀(ElasticOut)
            float press8 = 0f;
            if (camPress > 0) { camPress = Mathf.Max(0, camPress - dt / 0.55f); float e = 1f - camPress; press8 = (1f - ElasticOut(e)) * 8f / Mathf.Max(200f, Screen.height / s) ; }
            baseOrtho = Mathf.Lerp(baseOrtho, ortho, a);
            cam.orthographicSize = baseOrtho * (1f - press8);
            camBase = new Vector2(Mathf.Lerp(camBase.x, cx, a), Mathf.Lerp(camBase.y, cz, a));
            // 화면 흔들림은 오답 전용(정답 경로에는 쓰지 않는다)
            Vector2 sh = Vector2.zero;
            if (camShake > 0) { camShake = Mathf.Max(0, camShake - dt); float amp = camShake * 1.4f * baseOrtho / 16f; sh = new Vector2(Mathf.Sin(Time.time * 91f), Mathf.Cos(Time.time * 77f)) * amp; }
            cam.transform.position = new Vector3(camBase.x + sh.x, 60f, camBase.y + sh.y);
        }
    }
}
