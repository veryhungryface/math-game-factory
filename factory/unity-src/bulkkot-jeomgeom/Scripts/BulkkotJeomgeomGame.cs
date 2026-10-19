// 불꽃 점검 — 상태 머신과 유일한 답 입력(고리 드래그 → 도화선 당기기).
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.BulkkotJeomgeom
{
    public partial class BulkkotJeomgeomGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int firstCorrect;
            public int firstAttempts;
            public bool firstSubmitted;
            public int submissionCount;
            public int roundId;
            public string mode = "sum";
            public string[] canonicalCaseIds = new string[0];
            public string[] currentCaseIds = new string[0];
            public int selectedShell = -1;
            public int selectedRing = -1;
            public int[] ringIndices = new int[0];
            public int pointerVersion;
            public int screenW;
            public int screenH;
            public int[] cardPx = new int[0]; // 각 카드: centerX, upperY, lowerY (화면 왼쪽 아래 원점)
            public int[] fusePx = new int[2];
            public int duplicateCount;
            public int missingCount;
            public int firstFailedRounds;
            public bool onboarding;
            public bool frozen;
            public string prompt = "";
            public string misconceptionId = "";
            public string lastAction = "boot";
        }

        enum Phase { Title, Practice, Playing, Reveal, End }

        readonly State st = new State();
        readonly List<CaseProblem> problemModels = new List<CaseProblem>();
        readonly List<MgfProblem> bank = new List<MgfProblem>();
        readonly List<string> roster = new List<string>();
        List<CaseProblem> deck;
        CaseProblem current;
        System.Random rng;
        Phase phase = Phase.Title;
        int roundIndex;
        int firstFailedRounds;
        int runSerial;
        int selectedShell = -1;
        int selectedRing = -1;
        int keyboardCard;
        int keyboardRing;
        bool firstSubmission;
        bool revealCorrect;
        bool revealPractice;
        string endReason = "";
        float timeLeft;
        float revealClock;
        float idleClock;
        float hitStop;
        Vector2 pointerDown;
        bool ringDragging;
        bool fuseDragging;
        int dragCard = -1;
        int dragRing = -1;

        void Awake()
        {
            MgfLook.Quality(34f);
            // CreatePrimitive가 WebGL IL2CPP 스트리핑 뒤에도 각 기본 콜라이더를 만들 수 있게 타입을 보존한다.
            var colliderTypes = new GameObject("PrimitiveColliderTypes");
            colliderTypes.AddComponent<SphereCollider>(); colliderTypes.AddComponent<CapsuleCollider>();
            colliderTypes.AddComponent<BoxCollider>(); Destroy(colliderTypes);
            problemModels.AddRange(BulkkotRules.BuildBank());
            bank.AddRange(BulkkotRules.ToBank(problemModels));
            BuildWorld();
            BuildUi();
            Prewarm();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb = new StringBuilder("불꽃점검고리를돌려빠진경우를채워라중학교2학년경우의수합의법칙곱의법칙실전연습안전등남은시간첫제출정확통과다시점검도화선을아래로당기시오모양꼬리통별원세모꽃달마름모직선물결점선갈래고리지그재그중복누락같은경우한번만");
            for (int i = 0; i < bank.Count; i++) { sb.Append(bank[i].prompt); sb.Append(bank[i].unitConcept); }
            MgfText.Prewarm(sb.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(.05f, Time.deltaTime);
            HandlePointer();
            HandleKeyboard();
            if (hitStop > 0f) hitStop -= dt;
            else if (phase == Phase.Practice)
            {
                idleClock += dt;
                if (idleClock >= 8f) { idleClock = 0f; ReplayGuide(true); }
            }
            else if (phase == Phase.Playing)
            {
                timeLeft = Mathf.Max(0f, timeLeft - dt);
                if (timeLeft <= 0f) EndRun("time");
            }
            else if (phase == Phase.Reveal)
            {
                revealClock += dt;
                if (revealClock >= (revealCorrect ? 1.55f : 1.30f)) FinishReveal();
            }
            AnimateWorld(dt);
            UpdateUi(dt);
            FitCamera();
        }

        void ShowTitle()
        {
            phase = Phase.Title; current = null; deck = null; roster.Clear(); roundIndex = 0;
            st.score = 0; st.lives = BulkkotRules.StartLives; st.level = 1; st.solved = 0;
            st.firstCorrect = 0; st.firstAttempts = 0; st.firstSubmitted = false;
            st.submissionCount = 0; st.roundId = 0; st.misconceptionId = ""; st.lastAction = "title";
            firstFailedRounds = 0; endReason = ""; timeLeft = BulkkotRules.RunSeconds;
            ResetWorldForTitle(); ShowPhaseUi(); MgfBridge.NotifyChanged();
        }

        void ResetRun()
        {
            runSerial++;
            rng = new System.Random(unchecked(Environment.TickCount ^ runSerial * 104729));
            deck = BuildRunDeck(rng);
            roundIndex = 0; firstFailedRounds = 0; selectedShell = -1; selectedRing = -1;
            st.score = 0; st.lives = BulkkotRules.StartLives; st.level = 1; st.solved = 0;
            st.firstCorrect = 0; st.firstAttempts = 0; st.submissionCount = 0;
            st.misconceptionId = ""; timeLeft = BulkkotRules.RunSeconds; endReason = "";
            ResetWorldForRun();
        }

        List<CaseProblem> BuildRunDeck(System.Random random)
        {
            var result = new List<CaseProblem>(6);
            PickProblem(result, random, false, 1, 5, 8, 1);
            PickProblem(result, random, false, 1, 5, 8, 1);
            PickProblem(result, random, true, 2, 6, 12, 1);
            PickProblem(result, random, true, 2, 6, 12, 1);
            PickProblem(result, random, false, 3, 8, 8, 2);
            PickProblem(result, random, true, 3, 8, 12, 2);
            return result;
        }

        void PickProblem(List<CaseProblem> result, System.Random random, bool product, int band, int minN, int maxN, int faults)
        {
            int start = random.Next(problemModels.Count);
            for (int step = 0; step < problemModels.Count; step++)
            {
                var p = problemModels[(start + step) % problemModels.Count];
                if (p.product != product || p.band != band || p.Count < minN || p.Count > maxN || p.corruptionCount != faults || result.Contains(p)) continue;
                result.Add(p); return;
            }
            // 은행 조건이 바뀌어도 런타임은 정본 생성기를 그대로 사용한다.
            result.Add(BulkkotRules.Make(random.Next(10000, 999999), product, product ? 2 : 4, product ? 4 : 4, faults, band));
        }

        void StartPractice()
        {
            ResetRun();
            phase = Phase.Practice; current = BulkkotRules.Practice(); firstSubmission = true; idleClock = 0f;
            CopyInitialRoster(); BindCurrent(); PrepareRosterVisual(true); ShowPhaseUi(); ReplayGuide(false);
            MgfSfx.Play("whoosh", .28f); MgfBridge.NotifyChanged();
        }

        void StartRunDirect()
        {
            ResetRun(); phase = Phase.Playing; LoadRound(); ShowPhaseUi(); MgfBridge.NotifyChanged();
        }

        void BeginRunAfterPractice()
        {
            st.score = 0; st.solved = 0; st.lives = BulkkotRules.StartLives;
            st.firstCorrect = 0; st.firstAttempts = 0; st.submissionCount = 0;
            roundIndex = 0; firstFailedRounds = 0; timeLeft = BulkkotRules.RunSeconds;
            phase = Phase.Playing; ShowToast("실전 시작 · 첫 제출 5판 이상을 정확하게 점검하시오", 3f);
            LoadRound(); ShowPhaseUi();
        }

        void LoadRound()
        {
            if (roundIndex >= BulkkotRules.RoundCount)
            {
                EndRun(st.firstCorrect >= BulkkotRules.RequiredFirstCorrect ? "clear" : "mastery"); return;
            }
            current = deck[roundIndex]; firstSubmission = true; selectedShell = -1; selectedRing = -1;
            CopyInitialRoster(); BindCurrent(); PrepareRosterVisual(false); ShowPhaseUi();
            if (roundIndex == 2) ShowToast("이제 모양과 꼬리를 각각 하나씩 확인하시오 · 2×3은 6가지", 3.2f);
            else if (roundIndex == 4) ShowToast("마지막 두 판은 빠진 경우가 두 개다", 2.8f);
            MgfBridge.NotifyChanged();
        }

        void CopyInitialRoster()
        {
            roster.Clear();
            if (current != null) roster.AddRange(current.initialCaseIds);
            // 은행 생성 시드와 카드 위치가 상관되어 고정 칸 연타가 결함 위치를 예측하지 못하도록,
            // 실전 배치는 실행 덱 RNG의 다음 상태로 다시 균등 셔플한다. 연습은 안내와 맞춘 고정 배치다.
            if (phase == Phase.Playing && rng != null)
            {
                for (int i = roster.Count - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    string t = roster[i]; roster[i] = roster[j]; roster[j] = t;
                }
            }
        }

        void BindCurrent()
        {
            st.roundId = phase == Phase.Practice ? 0 : roundIndex + 1;
            st.level = phase == Phase.Practice ? 0 : Mathf.Clamp(roundIndex / 2 + 1, 1, 3);
            st.mode = current != null && current.product ? "product" : "sum";
            st.prompt = current == null ? "" : current.prompt;
            st.canonicalCaseIds = current == null ? new string[0] : (string[])current.canonicalCaseIds.Clone();
            st.currentCaseIds = roster.ToArray();
            st.selectedShell = selectedShell; st.selectedRing = selectedRing;
            st.firstSubmitted = !firstSubmission;
            st.firstFailedRounds = firstFailedRounds;
            st.duplicateCount = current == null ? 0 : BulkkotRules.DuplicateCount(roster);
            st.missingCount = current == null ? 0 : BulkkotRules.MissingCount(current, roster);
            st.ringIndices = new int[roster.Count * 2];
            for (int i = 0; i < roster.Count; i++)
            {
                bool prod; int a, b; BulkkotRules.TryDecode(roster[i], out prod, out a, out b);
                st.ringIndices[i * 2] = a; st.ringIndices[i * 2 + 1] = b;
            }
        }

        void ChangeRing(int card, int ring, int direction)
        {
            if (current == null || card < 0 || card >= roster.Count) return;
            bool product; int a, b; if (!BulkkotRules.TryDecode(roster[card], out product, out a, out b)) return;
            if (current.product)
            {
                if (ring == 0) a = Cycle(current.aItems, a, direction);
                else b = Cycle(current.bItems, b, direction);
                roster[card] = BulkkotRules.CaseId(true, a, b);
            }
            else
            {
                if (ring == 0)
                {
                    a = a == 0 ? 1 : 0;
                    int[] items = a == 0 ? current.aItems : current.bItems;
                    b = items[Mathf.Abs(b) % items.Length];
                }
                else
                {
                    int[] items = a == 0 ? current.aItems : current.bItems;
                    b = Cycle(items, b, direction);
                }
                roster[card] = BulkkotRules.CaseId(false, a, b);
            }
            selectedShell = card; selectedRing = ring;
            st.lastAction = "ring-" + card + "-" + ring + "-" + direction;
            BindCurrent(); RefreshCard(card, true);
            if (phase == Phase.Practice && BulkkotRules.ValidateRoster(current, roster))
            {
                goalTxt.text = "빠진 경우를 채웠습니다 · 도화선을 아래로 당기시오";
                ShowFuseGuide(true);
            }
            MgfSfx.Play("tap", .18f); MgfBridge.NotifyChanged();
        }

        static int Cycle(int[] values, int currentValue, int direction)
        {
            int index = Array.IndexOf(values, currentValue); if (index < 0) index = 0;
            index = (index + (direction >= 0 ? 1 : values.Length - 1)) % values.Length;
            return values[index];
        }

        void SubmitRoster()
        {
            if ((phase != Phase.Practice && phase != Phase.Playing) || current == null) return;
            revealPractice = phase == Phase.Practice;
            revealCorrect = BulkkotRules.ValidateRoster(current, roster);
            bool wasFirst = firstSubmission;
            st.submissionCount++;
            if (!revealPractice && wasFirst)
            {
                st.firstAttempts++;
                if (revealCorrect) st.firstCorrect++;
                else firstFailedRounds++;
                firstSubmission = false;
            }
            if (revealPractice)
            {
                if (revealCorrect) { st.score = 100; st.solved = 1; }
                else st.misconceptionId = BulkkotRules.Misconception(current, roster);
            }
            else if (revealCorrect)
            {
                st.score += wasFirst ? 150 + roundIndex * 20 : 70;
                st.solved++; st.misconceptionId = "";
            }
            else
            {
                st.lives = Mathf.Max(0, st.lives - 1);
                st.misconceptionId = BulkkotRules.Misconception(current, roster);
            }
            phase = Phase.Reveal; revealClock = 0f; hitStop = revealCorrect ? .08f : 0f;
            ringDragging = fuseDragging = false; dragCard = dragRing = -1;
            BindCurrent(); BeginRevealVisual(revealCorrect, wasFirst); ShowPhaseUi(); MgfBridge.NotifyChanged();
        }

        void FinishReveal()
        {
            if (phase != Phase.Reveal) return;
            CompleteRevealVisual(revealCorrect);
            if (revealPractice)
            {
                if (revealCorrect) BeginRunAfterPractice();
                else
                {
                    phase = Phase.Practice; CopyInitialRoster(); BindCurrent(); PrepareRosterVisual(true);
                    ShowPhaseUi(); ReplayGuide(true); MgfBridge.NotifyChanged();
                }
                return;
            }
            if (!revealCorrect)
            {
                if (st.lives <= 0) { EndRun("lights"); return; }
                if (firstFailedRounds >= 2) { EndRun("mastery-impossible"); return; }
                phase = Phase.Playing;
                ShowToast("중복 " + st.duplicateCount + "개 · 누락 " + st.missingCount + "개 · 같은 명단을 다시 고치시오", 3.4f);
                PrepareRosterVisual(false); ShowPhaseUi(); MgfBridge.NotifyChanged(); return;
            }
            roundIndex++; phase = Phase.Playing; LoadRound(); ShowPhaseUi();
        }

        void EndRun(string reason)
        {
            if (phase == Phase.End) return;
            phase = Phase.End; endReason = reason; ringDragging = fuseDragging = false;
            st.lastAction = "end-" + reason; ShowEnd(reason); ShowPhaseUi();
            MgfSfx.Play(reason == "clear" ? "win" : "lose", .52f); MgfBridge.NotifyChanged();
        }

        void HandlePointer()
        {
            if (MgfPointer.Down)
            {
                st.pointerVersion++; st.lastAction = "pointer"; pointerDown = MgfPointer.Position;
                SpawnTapRipple(pointerDown); MgfBridge.NotifyChanged();
                if (phase == Phase.Title) { BeginTitlePress(); MgfSfx.Play("tap", .2f); return; }
                if (phase == Phase.End) { PunchRestart(); return; }
                if (phase == Phase.Reveal) { RefuseInput("발사 결과를 확인한 뒤 다시 점검할 수 있습니다"); return; }
                if (phase != Phase.Practice && phase != Phase.Playing) return;
                idleClock = 0f;
                if (HitFuse(pointerDown))
                {
                    fuseDragging = true; BeginFuseDrag(); return;
                }
                if (phase == Phase.Practice)
                {
                    // 연습 정답 뒤에는 다시 고리를 흐트러뜨리지 않고 도화선 행동만 받는다.
                    if (BulkkotRules.ValidateRoster(current, roster))
                    {
                        // 한 번 정답 고리를 만든 뒤에는 다음 탭을 도화선 손잡이로 스냅한다.
                        // 연습에서 작은 손잡이를 다시 찾아 헤매지 않게 하되 실전 판정은 그대로다.
                        fuseDragging = true; BeginFuseDrag(); ShowFuseGuide(true); return;
                    }
                    // 안내 카드의 위·아래 고리와 몸통을 모두 '별→원' 목표 고리 하나로 연결한다.
                    if (HitPracticeShell(pointerDown))
                    {
                        ringDragging = true; dragCard = 1; dragRing = 1;
                        selectedShell = dragCard; selectedRing = dragRing; BindCurrent();
                        BeginRingDrag(dragCard, dragRing); MgfSfx.Play("tap", .14f); return;
                    }
                    RefuseInput("맥동하는 두 번째 불꽃 몸통을 누르거나 좌우로 미시오"); ReplayGuide(true); return;
                }
                if (HitRing(pointerDown, out int card, out int ring))
                {
                    ringDragging = true; dragCard = card; dragRing = ring;
                    selectedShell = card; selectedRing = ring; BindCurrent(); BeginRingDrag(card, ring); MgfSfx.Play("tap", .14f); return;
                }
                RefuseInput(phase == Phase.Practice ? "맥동하는 불꽃 고리를 좌우로 미시오" : "불꽃 고리를 밀거나 도화선을 아래로 당기시오");
                ReplayGuide(true); return;
            }
            if (MgfPointer.Held)
            {
                if (ringDragging) UpdateRingDrag(dragCard, dragRing, MgfPointer.Position.x - pointerDown.x);
                else if (fuseDragging) UpdateFuseDrag(pointerDown.y - MgfPointer.Position.y);
            }
            if (MgfPointer.Up)
            {
                if (phase == Phase.Title) { EndTitlePress(); StartPractice(); return; }
                if (phase == Phase.End) { StartPractice(); return; }
                if (ringDragging)
                {
                    float dx = MgfPointer.Position.x - pointerDown.x;
                    ringDragging = false; EndRingDrag(dragCard, dragRing);
                    if (Mathf.Abs(dx) >= 14f) ChangeRing(dragCard, dragRing, dx > 0 ? 1 : -1);
                    else if (phase == Phase.Practice) ChangeRing(dragCard, dragRing, 1);
                    else { ShowRingGuide(dragCard, dragRing, true); RefuseInput("고리를 좌우로 14px 이상 미시오"); }
                    dragCard = dragRing = -1; return;
                }
                if (fuseDragging)
                {
                    float dy = pointerDown.y - MgfPointer.Position.y;
                    fuseDragging = false; EndFuseDrag();
                    if (dy >= 48f || (phase == Phase.Practice && BulkkotRules.ValidateRoster(current, roster))) SubmitRoster();
                    else RefuseInput("도화선 손잡이를 아래로 48px 이상 당기시오");
                }
            }
        }

        void HandleKeyboard()
        {
            if (phase != Phase.Practice && phase != Phase.Playing) return;
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                keyboardRing++; if (keyboardRing > 2) { keyboardRing = 0; keyboardCard = (keyboardCard + 1) % Mathf.Max(1, roster.Count); }
                if (keyboardRing < 2) { selectedShell = keyboardCard; selectedRing = keyboardRing; ShowRingGuide(keyboardCard, keyboardRing, true); }
                else PulseFuse();
                st.pointerVersion++; st.lastAction = "keyboard-select"; BindCurrent(); MgfBridge.NotifyChanged();
            }
            int direction = Input.GetKeyDown(KeyCode.RightArrow) ? 1 : Input.GetKeyDown(KeyCode.LeftArrow) ? -1 : 0;
            if (direction != 0 && keyboardRing < 2) ChangeRing(keyboardCard, keyboardRing, direction);
            if ((Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.Return)) && keyboardRing == 2) SubmitRoster();
        }

        void FinishPendingReveal()
        {
            if (phase == Phase.Reveal) { revealClock = 99f; FinishReveal(); }
        }

        public void TestStart() { FinishPendingReveal(); StartRunDirect(); }

        public void TestAnswerCorrect()
        {
            FinishPendingReveal();
            if (phase != Phase.Playing || current == null) StartRunDirect();
            roster.Clear(); roster.AddRange(current.canonicalCaseIds); BindCurrent(); RefreshAllCards(); SubmitRoster();
        }

        public void TestAnswerWrong()
        {
            FinishPendingReveal();
            if (phase != Phase.Playing || current == null) StartRunDirect();
            if (BulkkotRules.ValidateRoster(current, roster))
            {
                roster[0] = roster[1]; BindCurrent(); RefreshCard(0, false);
            }
            SubmitRoster();
        }

        public string StateJson()
        {
            st.phase = phase == Phase.Title ? "title" : phase == Phase.End ? (endReason == "clear" ? "clear" : "gameover") : "playing";
            st.onboarding = phase == Phase.Practice;
            st.frozen = phase == Phase.Practice || phase == Phase.Reveal || phase == Phase.End;
            st.selectedShell = selectedShell; st.selectedRing = selectedRing;
            st.firstFailedRounds = firstFailedRounds;
            BindCurrent(); FillScreenProbe();
            return JsonUtility.ToJson(st);
        }

        public string ProblemBankJson() => BulkkotRules.ExportBankJson(problemModels);
    }
}
