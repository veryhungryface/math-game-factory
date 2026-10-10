// 만두 묶기 (mandu-mukgi) — 중2 2학기 「삼각형의 성질」 [9수03-09]
// 움직이는 이등변삼각형 반죽에서 같은 두 각(또는 같은 두 변)을 한 획으로 이어 손을 떼면 봉합된다.
// 학생이 두 대상을 직접 지나고, 코드는 지나간 대상 집합(비트마스크)을 정수로 판정만 한다(자동 짝 선택·자석 없음).
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.ManduMukgi
{
    public partial class ManduMukgiGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        public sealed class DoughView
        {
            public string id;
            public string mode;      // angle | side
            public bool fix;
            public int pre;          // 단계3 미리 그려진 연결(비트마스크, 화면에 보이는 정보)
            public int lane;
            public bool failed;
            public int[] tx = new int[3];   // 대상 k 의 화면 좌표(왼쪽 위 원점, 0..1000)
            public int[] ty = new int[3];
            public string[] tid = new string[3];
        }

        [Serializable]
        sealed class State : MgfState
        {
            public string stage = "title";       // title | practice | bridge | play | end
            public bool onboarding;
            public int firstAttemptCorrect;
            public int firstAttemptResolved;
            public int[] firstByBand = new int[3];
            public int[] resolvedByBand = new int[3];
            public int repaired;
            public int expired;
            public int submissions;
            public int combo;
            public int pointerVersion;
            public string lastResult = "";
            public string misconceptionId = "";
            public string endReason = "";
            public bool mastered;
            public bool locked;      // 정오 근거 설명 중(입력 잠금) — 제출 직후에만 켜졌다가 꺼진다
            public string[] activeProblemIds = new string[0];
            public string[] selectedTargetIds = new string[0];
            public DoughView[] doughs = new DoughView[0];
        }

        enum Stage { Title, Practice, Play, End }

        readonly State st = new State();
        readonly List<DoughProblem> catalog = new List<DoughProblem>();
        readonly List<MgfProblem> bank = new List<MgfProblem>();
        List<DoughProblem> deck = new List<DoughProblem>();
        System.Random rng;
        Stage stage = Stage.Title;
        readonly Tray[] trays = { new Tray(0), new Tray(1) };

        int band = 1, deckNext, runSerial, practiceStep;
        readonly int[] bandSealed = new int[3];
        float runClock = ManduRules.RunSeconds;
        float spawnTimer, pauseT, lastCorrectAt = -99f, playTime;
        Action afterPause;
        bool onboardingRun, tutorialSeen, firstDemoShown, wowShown;
        int bestScore;
        float botSpeed = 1f;

        // 획(드래그) 상태
        int strokeTray = -1, strokeMask;
        readonly List<int> strokeOrder = new List<int>(4);
        readonly List<Vector2> trail = new List<Vector2>(128);
        bool strokeCrossed;
        Vector2 lastLocal;
        float strokePhase;

        void Awake()
        {
            // CreatePrimitive 가 붙이는 콜라이더 타입을 엔진 코드 스트리핑이 지우지 않게 참조만 남긴다(실행되지 않음).
            if (Time.frameCount < -1) { gameObject.AddComponent<SphereCollider>(); gameObject.AddComponent<CapsuleCollider>(); gameObject.AddComponent<MeshCollider>(); }
            MgfLook.Quality(60f);
            catalog.AddRange(ManduRules.Catalog());
            for (int i = 0; i < catalog.Count; i++) bank.Add(catalog[i].ToMgf());
            ReadUrl();
            try { bestScore = PlayerPrefs.GetInt("mandu-mukgi-best", 0); tutorialSeen = PlayerPrefs.GetInt("mandu-mukgi-tutorial", 0) == 1; } catch (Exception) { }
            rng = new System.Random(DateTime.Now.Year * 10000 + DateTime.Now.Month * 100 + DateTime.Now.Day);
            Prewarm();
            ManduSfx.Prewarm();
            BuildWorld();
            BuildUi();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void ReadUrl()
        {
            // 봇 자가 테스트 전용 배속(?bot=8). QA 와 일반 사용자는 영향 없음.
            try
            {
                string url = Application.absoluteURL ?? "";
                int i = url.IndexOf("bot=", StringComparison.Ordinal);
                if (i >= 0)
                {
                    int j = i + 4; var sb = new StringBuilder();
                    while (j < url.Length && (char.IsDigit(url[j]) || url[j] == '.')) sb.Append(url[j++]);
                    if (float.TryParse(sb.ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float v))
                        botSpeed = Mathf.Clamp(v, 1f, 12f);
                }
            }
            catch (Exception) { botSpeed = 1f; }
            Time.timeScale = botSpeed;
        }

        void Prewarm()
        {
            var sb = new StringBuilder("만두묶기같은각을이어라중2학년삼각형의성질실전90초첫판단10/12최고기록점봉인시간종료다시묶기봉합완료기준미달숙련스탬프잘못된연결크기길이두변이으시오고치시오모서리호에서시작하시오손잡이곳확인중반죽서로다른넘어갔다맞은편꼭지각밑각밑변이므로합동SASASA△≡∠°cm연습실전단계다음남은초판정입니다기다리시오여기를눌러시작닫힌다새로이으면교체된다틀린쌍이다세곳을모두이었다두곳만이미있던연결은놓치면깨진다지나쳤다수리20점100점연속봉합접시만두완성잘했다아직첫판단을다시보자처음부터");
            for (int i = 0; i < catalog.Count; i += 7) sb.Append(catalog[i].Prompt);
            MgfText.Prewarm(sb.ToString());
        }

        // ─────────────────────────────── 흐름 ───────────────────────────────

        void ShowTitle()
        {
            stage = Stage.Title;
            st.phase = "title"; st.stage = "title"; st.onboarding = false;
            ResetStats();
            HideAllTrays();
            SetCap(1);
            var t = trays[0];
            SpawnInto(t, ManduRules.Practice1(), 0, false, false, true);
            t.phase = Tray.Phase.Arriving; t.arriveT = -0.35f; // 타이틀 패가 뒤집히는 동안 미끄러져 들어온다
            SetScreenTitle();
            MgfBridge.NotifyChanged();
        }

        void ResetStats()
        {
            st.score = 0; st.lives = ManduRules.StartSeals; st.level = 1; st.solved = 0;
            st.firstAttemptCorrect = 0; st.firstAttemptResolved = 0;
            for (int i = 0; i < 3; i++) { st.firstByBand[i] = 0; st.resolvedByBand[i] = 0; bandSealed[i] = 0; }
            st.repaired = 0; st.expired = 0; st.submissions = 0; st.combo = 0;
            st.lastResult = ""; st.misconceptionId = ""; st.endReason = ""; st.mastered = false;
            runClock = ManduRules.RunSeconds; pauseT = 0f; afterPause = null; spawnTimer = 0f; lastCorrectAt = -99f; playTime = 0f;
            firstDemoShown = false; wowShown = false;
            EndStroke(false);
            ResetWorldRun();
        }

        void BeginRun(bool onboarding)
        {
            runSerial++;
            rng = new System.Random(DateTime.Now.Year * 10000 + DateTime.Now.Month * 100 + DateTime.Now.Day + runSerial * 7919);
            ResetStats();
            deck = ManduRules.Deck(catalog, rng);
            deckNext = 0; band = 1;
            onboardingRun = onboarding;
            st.onboarding = onboarding;
            st.phase = "playing";
            if (onboarding)
            {
                StartPractice(1);
            }
            else
            {
                HideAllTrays();
                StartBand(1);
            }
            ManduSfx.Play("lift", 0.4f);
            SetScreenPlay();
            MgfBridge.NotifyChanged();
        }

        void StartPractice(int step)
        {
            stage = Stage.Practice; practiceStep = step;
            st.stage = step == 1 ? "practice" : "bridge";
            SetCap(1);
            var t = trays[0];
            bool keep = step == 1 && t.prob != null && t.prob.id == "practice-1" && t.phase != Tray.Phase.Hidden;
            HideTray(trays[1]);
            if (!keep)
            {
                HideTray(t);
                var p = step == 1 ? ManduRules.Practice1() : step == 2 ? ManduRules.Practice2() : ManduRules.Practice3();
                int rot = step == 1 ? 0 : step == 2 ? 160 : 250;
                SpawnInto(t, p, rot, false, false, false);
            }
            t.life = t.maxLife = 999f;
            hintT = 0f; hintIdle = 0f;
            SetGoal(step == 1 ? "같은 두 각을 이으면 만두가 닫힌다" : step == 2 ? "같은 각의 맞은편 변도 같다 — 변 가운데 손잡이를 이으시오" : "잘못된 연결은 두고, 맞는 두 곳을 새로 이으면 교체된다");
            SetBanner(step == 1 ? "연습 · 시간이 멈춰 있다" : "개념 다리 연습 · 시간이 멈춰 있다");
            st.level = step;
            MgfBridge.NotifyChanged();
        }

        void StartBand(int n)
        {
            stage = Stage.Play; practiceStep = 0; band = n; st.level = n; st.stage = "play";
            SetCap(ManduRules.Cap[n - 1]);
            spawnTimer = 0.05f;
            SetGoal(n == 1 ? "같은 길이의 두 변을 찾고, 그 맞은편 두 각을 이으시오" : n == 2 ? "같은 크기의 두 각을 찾고, 그 맞은편 두 변을 이으시오" : "잘못된 연결을 버리고 맞는 두 곳을 새로 이으시오");
            SetBanner(n + "단계 · 실전 시계가 흐른다");
            MgfBridge.NotifyChanged();
        }

        int ActiveCount() { int c = 0; foreach (var t in trays) if (t.phase != Tray.Phase.Hidden) c++; return c; }
        int BandRemaining() { int end = band * ManduRules.PerBand; return Mathf.Max(0, end - deckNext); }

        void SpawnNext()
        {
            if (BandRemaining() <= 0) return;
            Tray free = null;
            foreach (var t in trays) if (t.phase == Tray.Phase.Hidden && (cap == 2 || t.lane == 0)) { free = t; break; }
            if (free == null) return;
            var p = deck[deckNext++];
            SpawnInto(free, p, rng.Next(360), rng.Next(2) == 1, rng.Next(2) == 1, false);
            free.life = free.maxLife = ManduRules.Life[band - 1];
            free.freeze = 1.2f;
            MgfBridge.NotifyChanged();
        }

        void SpawnInto(Tray t, DoughProblem p, int rot, bool reflect, bool swap, bool titleMode)
        {
            t.Configure(p, rot, reflect, swap);
            t.phase = Tray.Phase.Arriving; t.arriveT = 0f; t.phaseT = 0f;
            t.attempted = false; t.failed = false; t.firstCorrect = false; t.serial++;
            t.selectedMask = 0; t.lastMask = 0; t.lastMisconception = ""; t.drift = 0f;
            t.freeze = 1.2f;
            t.stand.gameObject.SetActive(true);
            PlaceTray(t, true);
            ManduSfx.Play("tick", 0.25f);
        }

        void HideTray(Tray t)
        {
            if (strokeTray == t.lane) EndStroke(false);
            t.phase = Tray.Phase.Hidden;
            t.stand.gameObject.SetActive(false);
            t.ClearHint();
            SnapshotDoughs();
        }

        void CompleteSeal(Tray t)
        {
            if (t.phase != Tray.Phase.Sealing) return;
            if (!launched[t.lane]) LaunchFlyer(t);
            launched[t.lane] = false;
            t.Fold(0f);
            t.face.localRotation = cam.transform.rotation;
            HideTray(t);
        }

        void HideAllTrays() { foreach (var t in trays) HideTray(t); }

        void EndRun(string reason)
        {
            if (stage == Stage.End) return;
            EndStroke(false);
            stage = Stage.End; st.stage = "end";
            st.endReason = reason;
            st.mastered = reason == "mastered";
            st.phase = st.mastered ? "clear" : "gameover";
            st.onboarding = false;
            pauseT = 0f; afterPause = null;
            foreach (var t in trays) if (t.phase != Tray.Phase.Hidden && t.phase != Tray.Phase.Sealing) t.ClearHint();
            if (st.score > bestScore) { bestScore = st.score; try { PlayerPrefs.SetInt("mandu-mukgi-best", bestScore); PlayerPrefs.Save(); } catch (Exception) { } }
            ManduSfx.Play(st.mastered ? "fanfare" : "fail", 0.55f);
            SetScreenEnd(reason);
            MgfBridge.NotifyChanged();
        }

        // ─────────────────────────────── 매 프레임 ───────────────────────────────

        float hintT, hintIdle;
        Vector2 lastScreen;

        void Update()
        {
            float dt = Mathf.Min(0.05f * botSpeed, Time.deltaTime);
            CheckLayout();
            HandleInput();

            if (pauseT > 0f)
            {
                pauseT -= dt;
                if (pauseT <= 0f) { pauseT = 0f; var a = afterPause; afterPause = null; a?.Invoke(); }
            }
            else if (stage == Stage.Play)
            {
                runClock -= dt; playTime += dt;
                if (runClock <= 0f) { runClock = 0f; EndRun("time"); }
                else
                {
                    foreach (var t in trays)
                    {
                        if (t.phase != Tray.Phase.Ready) continue;
                        if (strokeTray == t.lane && t.freeze > 0f) t.freeze -= dt;   // 첫 접촉 정지(최대 1.2초, 1회)
                        else t.life -= dt;
                        t.drift += dt;
                        if (t.life <= 0f) { Expire(t); break; }
                    }
                    if (stage == Stage.Play && pauseT <= 0f)
                    {
                        if (BandRemaining() > 0 && ActiveCount() < cap)
                        {
                            spawnTimer -= dt;
                            if (spawnTimer <= 0f) { SpawnNext(); spawnTimer = ActiveCount() < cap ? 2.4f : 0.8f; }
                        }
                        else if (BandRemaining() <= 0 && ActiveCount() == 0) BandComplete();
                    }
                }
            }
            if (stage == Stage.Practice && pauseT <= 0f) UpdatePracticeHint(dt);

            bool lk = pauseT > 0f;
            if (lk != st.locked) { st.locked = lk; MgfBridge.NotifyChanged(); }
            UpdateTrays(dt);
            UpdateWorld(dt);
            UpdateUi(dt);
        }

        void BandComplete()
        {
            if (band >= 3) { FinishAll(); return; }
            if (onboardingRun) StartPractice(band + 1);
            else StartBand(band + 1);
        }

        void FinishAll()
        {
            bool mastered = st.solved >= ManduRules.Total && st.firstAttemptCorrect >= ManduRules.MasteryTotal
                && st.firstByBand[0] >= ManduRules.MasteryBand && st.firstByBand[1] >= ManduRules.MasteryBand && st.firstByBand[2] >= ManduRules.MasteryBand;
            EndRun(mastered ? "mastered" : "unmastered");
        }

        void UpdatePracticeHint(float dt)
        {
            var t = trays[0];
            if (t.phase != Tray.Phase.Ready || strokeTray >= 0) { if (strokeTray >= 0) t.ClearHint(); return; }
            hintT += dt; hintIdle += dt;
            float big = hintIdle > 8f ? 1.45f : 1f;
            int from, to;
            if (practiceStep == 1) { from = 1; to = 2; }            // ∠B → ∠C
            else if (practiceStep == 2) { from = 1; to = 2; }       // AC → AB (∠B·∠C 의 맞은편 변)
            else { from = 0; to = 2; }                              // ∠A → ∠C (기존 ∠A–∠B 교체)
            bool demo = practiceStep == 1 && hintT < 1.8f;
            if (demo) t.DrawHint(-1, -1, 0f, 1f, true, hintT / 1.4f);
            else t.DrawHint(from, to, ((hintT - (practiceStep == 1 ? 1.8f : 0f)) % 1.4f) / 1.4f, big, false, 0f);
        }

        // ─────────────────────────────── 입력 ───────────────────────────────

        void HandleInput()
        {
            bool down = MgfPointer.Down;
            Vector2 pos = MgfPointer.Position;
            if (down)
            {
                st.pointerVersion++;
                hintIdle = 0f;
                Ripple(pos);
                MgfBridge.NotifyChanged();
            }

            if (stage == Stage.Title)
            {
                if (!down) return;
                var t0 = trays[0];
                if (t0.phase != Tray.Phase.Hidden && t0.ScreenToLocal(cam, pos, out var l0) && t0.InsidePlate(l0))
                {
                    if (t0.phase == Tray.Phase.Arriving) FinishArrival(t0);
                    BeginRun(true);
                    // 같은 누름이 첫 호 위였다면 그대로 획을 시작한다(타이틀 → 연습이 한 동작으로 이어진다).
                    TryStartStroke(pos);
                }
                else { Refuse("반죽의 깜빡이는 모서리를 눌러 시작하시오", -1); PulseTitleCta(); }
                return;
            }
            if (stage == Stage.End)
            {
                if (!down) return;
                if (HitRestart(pos)) BeginRun(false);
                else { Refuse("「다시 묶기」 패를 누르시오", -1); PulseRestart(); }
                return;
            }

            if (pauseT > 0f)
            {
                if (down) Refuse("봉합을 확인하는 중이다. 잠시 기다리시오", -1);
                if (MgfPointer.Up && strokeTray >= 0) EndStroke(false);
                return;
            }

            if (down) TryStartStroke(pos);

            if (strokeTray >= 0 && MgfPointer.Held) ContinueStroke(pos);

            if (strokeTray >= 0 && MgfPointer.Up)
            {
                ContinueStroke(pos);
                ReleaseStroke();
            }
        }

        void TryStartStroke(Vector2 pos)
        {
            if (strokeTray >= 0) return;
            Tray hitTray = null; Vector2 local = default;
            foreach (var t in trays)
            {
                if (t.phase == Tray.Phase.Hidden) continue;
                if (t.ScreenToLocal(cam, pos, out var l) && t.InsidePlate(l)) { hitTray = t; local = l; break; }
            }
            if (hitTray == null)
            {
                Refuse(TrayCount() > 0 ? "반죽 위의 " + (AnyAngleMode() ? "모서리(호)" : "변 손잡이") + "에서 시작하시오" : "다음 반죽이 들어오는 중이다", NearestTray(pos));
                return;
            }
            if (hitTray.phase != Tray.Phase.Ready)
            {
                if (hitTray.phase == Tray.Phase.Arriving) { FinishArrival(hitTray); }
                else { Refuse("이 반죽은 지금 봉합 중이다", hitTray.lane); return; }
            }
            int k = hitTray.NearestTarget(local);
            if (k < 0)
            {
                Refuse(hitTray.prob.AngleMode ? "각의 호(모서리)에서 시작해 다른 호까지 이으시오" : "변 가운데 손잡이에서 시작해 다른 손잡이까지 이으시오", hitTray.lane);
                WiggleTray(hitTray);
                return;
            }
            strokeTray = hitTray.lane;
            strokeMask = 1 << k; strokeOrder.Clear(); strokeOrder.Add(k);
            trail.Clear(); trail.Add(local); lastLocal = local; strokeCrossed = false; strokePhase = 0f;
            hitTray.ClearHint();
            LiftTray(hitTray);
            ManduSfx.Play("lift", 0.35f, 1f);
            MagpieClamp();
            RedrawStroke(hitTray);
            SyncSelection();
        }

        void ContinueStroke(Vector2 pos)
        {
            var t = trays[strokeTray];
            if (!t.ScreenToLocal(cam, pos, out var local)) return;
            // 다른 반죽 위로 넘어갔는지
            foreach (var o in trays)
            {
                if (o == t || o.phase == Tray.Phase.Hidden) continue;
                if (o.ScreenToLocal(cam, pos, out var ol) && o.InsidePlate(ol) && !t.InsidePlate(local)) strokeCrossed = true;
            }
            // 직전 점 → 현재 점을 잘게 나눠 대상 충돌(선분-원)
            float dist = Vector2.Distance(lastLocal, local);
            int steps = Mathf.Max(1, Mathf.CeilToInt(dist / 0.08f));
            bool changed = false;
            for (int i = 1; i <= steps; i++)
            {
                var p = Vector2.Lerp(lastLocal, local, (float)i / steps);
                int k = t.NearestTarget(p);
                if (k >= 0 && (strokeMask & (1 << k)) == 0)
                {
                    strokeMask |= 1 << k; strokeOrder.Add(k); changed = true;
                    ManduSfx.Play("pleat", 0.3f, 1.15f);
                }
            }
            if (dist > 0.05f) { trail.Add(local); lastLocal = local; changed = true; if (trail.Count > 120) trail.RemoveAt(1); }
            if (changed) { RedrawStroke(t); SyncSelection(); }
        }

        void RedrawStroke(Tray t)
        {
            t.DrawStroke(trail, strokeMask, new Color(1f, 1f, 1f, 0.92f), 0.11f, strokePhase);
        }

        void ReleaseStroke()
        {
            var t = trays[strokeTray];
            int mask = strokeMask; int count = DoughProblem.Bits(mask);
            bool crossed = strokeCrossed;
            var order = new List<int>(strokeOrder);
            EndStroke(true);
            if (crossed) { Refuse("같은 반죽의 두 곳을 이으시오", t.lane); SlackTrail(t); return; }
            if (count < 2)
            {
                Refuse(t.prob.AngleMode ? "두 각을 이어 놓으시오 — 한 호에서 다른 호까지 끌어 손을 떼시오" : "두 변을 이어 놓으시오 — 한 손잡이에서 다른 손잡이까지 끌어 손을 떼시오", t.lane);
                StubHint(t, order.Count > 0 ? order[0] : 0);
                return;
            }
            Submit(t, mask);
        }

        void EndStroke(bool keepTrail)
        {
            if (strokeTray >= 0 && !keepTrail) trays[strokeTray].ClearDyn();
            strokeTray = -1; strokeMask = 0; strokeOrder.Clear();
            if (!keepTrail) trail.Clear();
            SyncSelection();
        }

        void SyncSelection()
        {
            if (strokeTray < 0 || trays[strokeTray].prob == null) { if (st.selectedTargetIds.Length != 0) st.selectedTargetIds = new string[0]; return; }
            var t = trays[strokeTray];
            var ids = new string[strokeOrder.Count];
            for (int i = 0; i < ids.Length; i++) ids[i] = t.prob.TargetName(strokeOrder[i]);
            st.selectedTargetIds = ids;
            MgfBridge.NotifyChanged();
        }

        int TrayCount() { int c = 0; foreach (var t in trays) if (t.phase == Tray.Phase.Ready) c++; return c; }
        bool AnyAngleMode() { foreach (var t in trays) if (t.phase != Tray.Phase.Hidden && t.prob != null) return t.prob.AngleMode; return true; }

        int NearestTray(Vector2 pos)
        {
            int best = -1; float bd = float.MaxValue;
            foreach (var t in trays)
            {
                if (t.phase == Tray.Phase.Hidden) continue;
                var s = cam.WorldToScreenPoint(t.content.position);
                float d = (new Vector2(s.x, s.y) - pos).sqrMagnitude;
                if (d < bd) { bd = d; best = t.lane; }
            }
            return best;
        }

        // ─────────────────────────────── 판정 ───────────────────────────────

        void Submit(Tray t, int mask)
        {
            var p = t.prob;
            var verdict = p.Judge(mask);
            bool correct = verdict == Verdict.Correct;
            t.lastMask = mask;
            st.submissions++;
            string mis = p.MisconceptionId(mask);
            if (!correct && LowestTwo(t) == mask) mis += "+lowest-two-on-screen";
            t.lastMisconception = mis;
            st.misconceptionId = mis;
            st.lastResult = correct ? "correct" : "wrong";

            if (p.practice)
            {
                if (correct) { BeginSeal(t, true, practiceStep == 1); }
                else { BeginWrong(t, mask, false); }
                MgfBridge.NotifyChanged();
                return;
            }

            bool first = !t.attempted;
            if (first)
            {
                t.attempted = true;
                st.firstAttemptResolved++; st.resolvedByBand[p.band - 1]++;
                if (correct) { st.firstAttemptCorrect++; st.firstByBand[p.band - 1]++; t.firstCorrect = true; }
            }
            if (correct)
            {
                int gain = first ? 100 : 20;
                if (!first) st.repaired++;
                if (first && playTime - lastCorrectAt <= 6f) st.combo++; else st.combo = first ? 1 : 0;
                if (first) lastCorrectAt = playTime;
                int bonus = st.combo >= 3 ? 50 : st.combo == 2 ? 20 : 0;
                st.score += gain + bonus;
                st.solved++; bandSealed[p.band - 1]++;
                bool demo = !firstDemoShown;
                firstDemoShown = true;
                BeginSeal(t, false, demo);
                if (st.combo >= 3 && !wowShown) { wowShown = true; QueueWow(); SetBanner("3연속 봉합! 찜통이 열린다"); }
            }
            else
            {
                st.combo = 0;
                st.lives = Mathf.Max(0, st.lives - 1);
                t.failed = true;
                BeginWrong(t, mask, true);
            }
            MgfBridge.NotifyChanged();
        }

        void Expire(Tray t)
        {
            var p = t.prob;
            st.expired++;
            if (!t.attempted)
            {
                t.attempted = true;
                st.firstAttemptResolved++; st.resolvedByBand[p.band - 1]++;
            }
            st.combo = 0;
            st.lives = Mathf.Max(0, st.lives - 1);
            t.failed = true;
            t.lastMisconception = "expired";
            st.misconceptionId = "expired"; st.lastResult = "expired";
            EndStrokeIfOn(t);
            BeginWrong(t, 0, true);
            MgfBridge.NotifyChanged();
        }

        void EndStrokeIfOn(Tray t) { if (strokeTray == t.lane) EndStroke(false); }

        /// <summary>화면에서 가장 아래 두 대상(오개념 봇 정책·기록용).</summary>
        int LowestTwo(Tray t)
        {
            int hi = 0;
            for (int k = 1; k < 3; k++) if (t.tpos[k].y > t.tpos[hi].y) hi = k;
            return 7 ^ (1 << hi);
        }

        void BeginSeal(Tray t, bool practice, bool demo)
        {
            t.phase = Tray.Phase.Sealing; t.phaseT = 0f;
            t.SetSplit(true);
            SnapshotDoughs();   // 봉합 중인 반죽은 더 이상 조작 대상이 아니다
            sealDemo[t.lane] = demo;
            sealPractice[t.lane] = practice;
            var p = t.prob;
            string sub = p.Reason + (demo ? "\n△" + DoughProblem.V[p.apex] + DoughProblem.V[t.nLeft] + "D≡△" + DoughProblem.V[p.apex] + DoughProblem.V[t.nRight] + "D (" + (p.AngleMode ? "SAS" : "ASA") + " 합동)" : "");
            t.SetTag(practice ? "봉합! 연습 통과" : (t.attempted && !t.firstCorrect ? "수리 봉합 +20" : "봉합!"), sub, 1);
            hitStop = 0.07f;
            float hold = demo ? 2.1f : 0.85f;
            Pause(hold, () =>
            {
                if (practice)
                {
                    if (practiceStep == 1) { tutorialSeen = true; try { PlayerPrefs.SetInt("mandu-mukgi-tutorial", 1); PlayerPrefs.Save(); } catch (Exception) { } StartBand(1); }
                    else StartBand(practiceStep);
                }
            });
            CorrectFx(t, demo);
        }

        void BeginWrong(Tray t, int mask, bool real)
        {
            t.phase = Tray.Phase.Feedback; t.phaseT = 0f;
            var p = t.prob;
            string head = mask == 0 ? "시간 안에 잇지 못했다" : p.WrongExplain(mask);
            string sub = "정답: " + p.PairText(p.CorrectMask) + " · " + p.Reason;
            t.SetTag(head, sub, 2);
            WrongFx(t, mask, real);
            if (real && st.lives <= 0) { EndRun("seals"); return; }   // 봉인 0 → 즉시 종료·입력 잠금(되살리기 없음)
            Pause(real ? 1.5f : 1.6f, () => RepairReady(t, real));
        }

        void RepairReady(Tray t, bool real)
        {
            if (t.phase == Tray.Phase.Hidden) return;
            t.phase = Tray.Phase.Ready; t.phaseT = 0f;
            t.ClearReveal(); t.ClearDyn();
            if (real) { t.life = t.maxLife = ManduRules.Life[t.prob.band - 1]; }
            t.SetTag(t.prob.Fix ? t.prob.Instruction : (real ? "다시 이어 수리하시오 · " : "다시 하시오 · ") + t.prob.Instruction, "", 0);
            if (!real) { hintT = 1.8f; }
            SnapshotDoughs();
            MgfBridge.NotifyChanged();
        }

        void Pause(float sec, Action then)
        {
            // 시범·정오 근거 설명 동안 세계 시계와 반죽 수명은 함께 멈춘다.
            if (pauseT > 0f && afterPause != null) { var a = afterPause; afterPause = null; a(); }
            pauseT = sec; afterPause = then;
        }

        void FlushPause()
        {
            int guard = 0;
            while (pauseT > 0f && guard++ < 4) { pauseT = 0f; var a = afterPause; afterPause = null; a?.Invoke(); }
        }

        // ─────────────────────────────── 상태 스냅샷 ───────────────────────────────

        /// <summary>대상 화면 좌표는 반죽이 자리 잡을 때만 갱신한다(시간에 따라 계속 바뀌는 값을 상태에 넣지 않는다).</summary>
        void SnapshotDoughs()
        {
            var list = new List<DoughView>();
            var ids = new List<string>();
            foreach (var t in trays)
            {
                if (t.phase == Tray.Phase.Hidden || t.prob == null) continue;
                if (t.phase == Tray.Phase.Sealing) continue;
                var v = new DoughView { id = t.prob.id, mode = t.prob.AngleMode ? "angle" : "side", fix = t.prob.Fix, pre = t.prob.preMask, lane = t.lane, failed = t.failed };
                for (int k = 0; k < 3; k++)
                {
                    var s = t.LocalToScreen(cam, t.tpos[k]);
                    v.tx[k] = Mathf.RoundToInt(s.x / Mathf.Max(1, Screen.width) * 1000f);
                    v.ty[k] = Mathf.RoundToInt((1f - s.y / Mathf.Max(1, Screen.height)) * 1000f);
                    v.tid[k] = t.prob.TargetName(k);
                }
                list.Add(v); ids.Add(t.prob.id);
            }
            st.doughs = list.ToArray();
            st.activeProblemIds = ids.ToArray();
        }

        // ─────────────────────────────── 테스트 훅 ───────────────────────────────

        public void TestStart() { BeginRun(false); }

        Tray ReadyTrayForTest()
        {
            if (stage == Stage.Title || stage == Stage.End) BeginRun(false);
            FlushPause();
            if (stage == Stage.End) BeginRun(false);
            foreach (var t in trays) CompleteSeal(t);
            if (stage == Stage.Play && ActiveCount() == 0)
            {
                if (BandRemaining() <= 0) { BandComplete(); FlushPause(); }
                if (stage == Stage.End) BeginRun(false);
                if (ActiveCount() == 0) SpawnNext();
            }
            foreach (var t in trays) if (t.phase == Tray.Phase.Arriving) FinishArrival(t);
            foreach (var t in trays) if (t.phase == Tray.Phase.Ready) return t;
            return null;
        }

        public void TestAnswerCorrect()
        {
            var t = ReadyTrayForTest();
            if (t == null) return;
            EndStroke(false);
            Submit(t, t.prob.CorrectMask);   // 실제 손을 뗄 때와 같은 판정 함수
        }

        public void TestAnswerWrong()
        {
            var t = ReadyTrayForTest();
            if (t == null) return;
            EndStroke(false);
            Submit(t, ManduRules.WrongPairs(t.prob.apex)[0]);
        }

        public string StateJson() { return JsonUtility.ToJson(st); }
        public string ProblemBankJson() { return MgfJson.Bank(bank); }
    }
}
