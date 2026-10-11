// 연 띄워 — 타이틀→실전 연습→3×3 생존 보드→결과 상태 머신과 실제 원호·삽입 입력.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.YeonTtwiwo
{
    public partial class YeonTtwiwoGame : MonoBehaviour, IMgfGame
    {
        [Serializable]
        sealed class State : MgfState
        {
            public int firstAttempts;
            public int firstCorrect;
            public int launched;
            public int placements;
            public int correctPlacements;
            public bool masteryPossible;
            public bool onboarding;
            public bool frozen;
            public int combo;
            public int pointerVersion;
            public int currentBand;
            public int legalCount;
            public int acceptedCount;
            public int wrongExplanationChecks;
            public double chanceBaseline;
            public string prompt = "";
            public string misconceptionId = "";
            public string lastExplanation = "";
            public string lastAction = "boot";
            public int[] cells = new int[YeonRules.BoardCount];
            // 실제 포인터 검증용 화면 좌표(왼쪽 위 원점): dock 9쌍, cartridge 3쌍, title/restart 각 1쌍.
            public int[] dockPx = new int[YeonRules.BoardCount * 2];
            public int[] cartridgePx = new int[YeonRules.CartCount * 2];
            public int[] chromePx = new int[4];
            // goal/prompt/hud/evidence의 실제 화면 픽셀 사각형(x,y,w,h, 왼쪽 위 원점).
            public int[] panelPx = new int[16];
            // 화면에 적힌 현재 정수 측정값의 기계 판독 사본. 실제 포인터 플레이스루 검산에 쓴다.
            public int[] dockGeometry = new int[YeonRules.BoardCount * 3]; // goal, ux, uy
            public int[] cartridgeGeometry = new int[YeonRules.CartCount * 4]; // vx, vy, p, q
            public double[] botFirstRates = new double[4];
            public double[] botChanceBaselines = new double[4];
            public int[] botCompletions = new int[4];
            public int screenW;
            public int screenH;
        }

        enum GamePhase { Title, Practice, Playing, Reveal, End }
        enum DragMode { None, TitleCord, Cartridge, EndCord }

        readonly State st = new State();
        readonly int[] cells = new int[YeonRules.BoardCount]; // 0=빈 연틀, 1=바람줄, 2=먹물 매듭
        readonly int[] cartridgeRotation = new int[YeonRules.CartCount];
        readonly List<YeonBankItem> bank = new List<YeonBankItem>(360);

        GamePhase gamePhase = GamePhase.Title;
        DragMode dragMode;
        BundleSpec current;
        System.Random rng;
        int runSerial, bundleSerial, selectedCartridge = -1, keyboardDock, keyboardCart;
        int revealClearMask, revealKnotMask, revealLineCount;
        bool revealCorrect, revealPractice, revealWillEnd;
        float runLeft, revealClock, hitStopLeft, idleClock;
        Vector2 pointerDown, previousPointer, cartScreenCenter;
        float arcAccumulator, lastPointerAngle, dragDistance;
        bool rotatedThisGrab, inserting;
        string endReason = "";

        void Awake()
        {
            MgfLook.Quality(35f);
            // CreatePrimitive 내부 콜라이더 IL2CPP 스트리핑 방지. 게임 오브젝트는 대부분 Block을 쓴다.
            var colliderTypes = new GameObject("PrimitiveColliderTypes");
            colliderTypes.AddComponent<BoxCollider>();
            colliderTypes.AddComponent<SphereCollider>();
            colliderTypes.AddComponent<CapsuleCollider>();
            Destroy(colliderTypes);

            st.wrongExplanationChecks = YeonRules.ValidateAll();
            BotAudit[] audits = YeonRules.RunBotAudit(200);
            for (int i = 0; i < audits.Length; i++)
            {
                st.botFirstRates[i] = audits[i].firstRate;
                st.botChanceBaselines[i] = audits[i].chanceBaseline;
                st.botCompletions[i] = audits[i].completions;
                // 200판의 표본 흔들림은 기록하되, 우연 기준을 2%p 이상 넘는 구조적 누설만 부팅을 막는다.
                if (audits[i].firstRate > audits[i].chanceBaseline + .02)
                    throw new InvalidOperationException("무뇌 봇 우연 수준 유의 초과: " + audits[i].policy);
            }

            bank.AddRange(YeonRules.BuildBank(360));
            BuildWorld();
            BuildUi();
            Prewarm();
            ShowTitle();
            MgfBridge.Register(this);
        }

        void Prewarm()
        {
            var sb = new StringBuilder("연띄워살대를엮어하늘로띄워라중학교2학년사각형의성질먹구름흑칠관측옥상발사줄아래로당겨시작최고기록");
            sb.Append("살대를돌려꽂고바람줄세칸을이으시오평행사변형직사각형마름모정사각형대각선서로다른것을이등분수직길이같음연틀걸이먹물매듭남은시간목숨점수연습실전발사완료숙련미달다시하기");
            sb.Append("가운데연틀에꽂으시오슬롯밖입니다먼저손잡이를원호로돌리시오비단이펴질때까지기다리시오");
            for (int i = 0; i < Math.Min(40, bank.Count); i++) sb.Append(bank[i].prompt);
            MgfText.Prewarm(sb.ToString());
        }

        void Update()
        {
            float dt = Mathf.Min(.05f, Time.deltaTime);
            HandleInput();
            HandleKeyboard();

            if (hitStopLeft > 0f) hitStopLeft = Mathf.Max(0f, hitStopLeft - dt);
            else if (gamePhase == GamePhase.Practice)
            {
                idleClock += dt;
                if (idleClock >= 7f) { idleClock = 0f; ReplayGuide(true); }
            }
            else if (gamePhase == GamePhase.Playing)
            {
                runLeft = Mathf.Max(0f, runLeft - dt);
                idleClock += dt;
                if (idleClock >= 8f) { idleClock = 0f; ReplayGuide(true); }
                if (runLeft <= 0f) EndRun("timeout");
            }
            else if (gamePhase == GamePhase.Reveal)
            {
                revealClock += dt;
                float duration = revealPractice ? 1.55f : revealCorrect ? (revealLineCount > 0 ? 2.0f : 1.0f) : 1.35f;
                if (revealClock >= duration) FinishReveal();
            }

            AnimateWorld(dt);
            UpdateUi(dt);
            ApplyResponsiveLayout();
        }

        void ShowTitle()
        {
            gamePhase = GamePhase.Title;
            dragMode = DragMode.None;
            current = null;
            endReason = "";
            selectedCartridge = -1;
            st.score = 0;
            st.lives = YeonRules.StartLives;
            st.level = 1;
            st.phase = "title";
            st.solved = 0;
            st.firstAttempts = 0;
            st.firstCorrect = 0;
            st.launched = 0;
            st.placements = 0;
            st.correctPlacements = 0;
            st.masteryPossible = true;
            st.onboarding = false;
            st.frozen = true;
            st.combo = 0;
            st.currentBand = 0;
            st.prompt = "";
            st.misconceptionId = "";
            st.lastExplanation = "";
            st.lastAction = "title";
            Array.Clear(cells, 0, cells.Length);
            SyncCells();
            ResetAllVisuals();
            SetPhaseUi();
            MgfBridge.NotifyChanged();
        }

        void StartPractice()
        {
            runSerial++;
            rng = new System.Random(unchecked(Environment.TickCount ^ runSerial * 104729));
            gamePhase = GamePhase.Practice;
            dragMode = DragMode.None;
            current = YeonRules.BuildPractice();
            selectedCartridge = -1;
            endReason = "";
            runLeft = 90f;
            revealClock = 0f;
            idleClock = 0f;
            Array.Clear(cells, 0, cells.Length);
            Array.Clear(cartridgeRotation, 0, cartridgeRotation.Length);
            st.score = 0;
            st.lives = YeonRules.StartLives;
            st.level = 1;
            st.phase = "playing";
            st.solved = 0;
            st.firstAttempts = 0;
            st.firstCorrect = 0;
            st.launched = 0;
            st.placements = 0;
            st.correctPlacements = 0;
            st.masteryPossible = true;
            st.onboarding = true;
            st.frozen = true;
            st.combo = 0;
            st.currentBand = 0;
            st.misconceptionId = "";
            st.lastExplanation = "";
            st.lastAction = "practice-start";
            BindBundle();
            ResetBoardVisuals();
            SetPhaseUi();
            ReplayGuide(false);
            MgfSfx.Play("whoosh", .32f);
            MgfBridge.NotifyChanged();
        }

        void StartRunDirect()
        {
            runSerial++;
            bundleSerial = 0;
            rng = new System.Random(unchecked(Environment.TickCount ^ runSerial * 130363));
            gamePhase = GamePhase.Playing;
            dragMode = DragMode.None;
            selectedCartridge = -1;
            endReason = "";
            runLeft = 90f;
            revealClock = 0f;
            idleClock = 0f;
            Array.Clear(cells, 0, cells.Length);
            Array.Clear(cartridgeRotation, 0, cartridgeRotation.Length);
            st.score = 0;
            st.lives = YeonRules.StartLives;
            st.level = 1;
            st.phase = "playing";
            st.solved = 0;
            st.firstAttempts = 0;
            st.firstCorrect = 0;
            st.launched = 0;
            st.placements = 0;
            st.correctPlacements = 0;
            st.masteryPossible = true;
            st.onboarding = false;
            st.frozen = false;
            st.combo = 0;
            st.misconceptionId = "";
            st.lastExplanation = "";
            st.lastAction = "run-start";
            ResetBoardVisuals();
            LoadBundle();
            SetPhaseUi();
            MgfBridge.NotifyChanged();
        }

        void BeginRunAfterPractice()
        {
            StartRunDirect();
            ShowToast("실전 시작 · 맞힌 칸도 바람줄로 남아 다음 수를 바꾼다", 3.2f);
        }

        void LoadBundle()
        {
            if (gamePhase != GamePhase.Playing) return;
            int emptyMask = 0;
            for (int i = 0; i < cells.Length; i++) if (cells[i] == 0) emptyMask |= 1 << i;
            if (YeonRules.CountBits(emptyMask) < 3) { EndRun("no_moves"); return; }
            int preferred = ChoosePreferredDock(emptyMask);
            int band = st.correctPlacements < 4 ? 1 : st.correctPlacements < 10 ? 2 : 3;
            st.level = band;
            st.currentBand = band;
            int seed = unchecked(runSerial * 99991 + (++bundleSerial) * 7919 + st.placements * 3571);
            current = YeonRules.BuildBundle(seed, band, emptyMask, preferred);
            if (current == null) { EndRun("no_moves"); return; }
            Array.Clear(cartridgeRotation, 0, cartridgeRotation.Length);
            selectedCartridge = -1;
            st.prompt = current.prompt;
            st.legalCount = current.legalCount;
            st.acceptedCount = current.acceptedCount;
            st.chanceBaseline = current.chanceBaseline;
            st.misconceptionId = "";
            st.lastExplanation = "";
            st.lastAction = "bundle-" + bundleSerial;
            idleClock = 0f;
            BindBundle();
            MgfBridge.NotifyChanged();
        }

        int ChoosePreferredDock(int emptyMask)
        {
            int best = -1, bestScore = -999;
            for (int i = 0; i < cells.Length; i++) if ((emptyMask & (1 << i)) != 0)
            {
                int row = i / 3, col = i % 3;
                int rowTokens = 0, colTokens = 0;
                bool rowKnot = false, colKnot = false;
                for (int k = 0; k < 3; k++)
                {
                    int rs = cells[row * 3 + k], cs = cells[k * 3 + col];
                    if (rs == 1) rowTokens++; else if (rs == 2) rowKnot = true;
                    if (cs == 1) colTokens++; else if (cs == 2) colKnot = true;
                }
                int score = (!rowKnot ? rowTokens * 8 + 3 : 0) + (!colKnot ? colTokens * 7 + 2 : 0);
                score += rng == null ? 0 : rng.Next(0, 3);
                if (score > bestScore) { bestScore = score; best = i; }
            }
            return best >= 0 ? best : YeonRules.FirstEmpty(emptyMask);
        }

        void HandleInput()
        {
            if (MgfPointer.Down)
            {
                st.pointerVersion++;
                pointerDown = previousPointer = MgfPointer.Position;
                idleClock = 0f;

                if (gamePhase == GamePhase.Title)
                {
                    // 첫 탭의 Down 프레임에서 바로 연습으로 전환한다. 모바일 WebGL에서는
                    // 짧은 탭의 Up이 캔버스 밖으로 귀속될 수 있어 타이틀이 오래 정체되던
                    // 경로가 있었다. 드래그와 탭 모두 같은 단일 CTA다.
                    dragMode = DragMode.None;
                    MgfSfx.Play("tap", .18f);
                    StartPractice();
                    return;
                }
                if (gamePhase == GamePhase.End)
                {
                    dragMode = DragMode.EndCord;
                    SetEndCordPull(0f);
                    MgfSfx.Play("tap", .18f);
                    MgfBridge.NotifyChanged();
                    return;
                }
                if (gamePhase == GamePhase.Reveal)
                {
                    RefuseAt(MgfPointer.Position, "비단이 펴질 때까지 잠시 기다리시오");
                    MgfBridge.NotifyChanged();
                    return;
                }

                int cart = CartridgeAtScreen(MgfPointer.Position);
                // 고정 연습에서는 하단 절반 전체를 연습 살대의 넓은 시작 영역으로 쓴다.
                // 짧은 원호를 그리는 학생이 작은 3D 손잡이를 정확히 찍지 못해도 같은
                // 실제 drag 경로로 회전·삽입을 배울 수 있고, 탭만으로는 제출되지 않는다.
                if (cart < 0 && gamePhase == GamePhase.Practice)
                    cart = 0;
                if (cart >= 0 && (gamePhase != GamePhase.Practice || cart == 0))
                {
                    BeginCartridgeGrab(cart);
                    MgfBridge.NotifyChanged();
                    return;
                }
                    RefuseAt(MgfPointer.Position, "손잡이에서 시작해 원호로 돌린 뒤 가운데 연틀까지 이으시오");
                    ReplayGuide(false);
                MgfBridge.NotifyChanged();
            }

            if (MgfPointer.Held)
            {
                Vector2 now = MgfPointer.Position;
                if (dragMode == DragMode.TitleCord)
                {
                    float pull = Mathf.Max(0f, pointerDown.y - now.y);
                    SetTitleCordPull(pull);
                }
                else if (dragMode == DragMode.EndCord)
                {
                    float pull = Mathf.Max(0f, pointerDown.y - now.y);
                    SetEndCordPull(pull);
                }
                else if (dragMode == DragMode.Cartridge) UpdateCartridgeDrag(now);
                previousPointer = now;
            }

            if (MgfPointer.Up)
            {
                Vector2 now = MgfPointer.Position;
                if (dragMode == DragMode.TitleCord)
                {
                    SetTitleCordPull(0f);
                    dragMode = DragMode.None;
                    // 발사줄을 당기면 장력 연출이 따라오고, 짧게 눌러도 같은 단일 CTA로 시작한다.
                    StartPractice();
                }
                else if (dragMode == DragMode.EndCord)
                {
                    SetEndCordPull(0f);
                    dragMode = DragMode.None;
                    // 결과 화면에서는 손잡이 전체가 1탭 재시작 CTA다. 당기기도 같은 경로로 동작한다.
                    StartPractice();
                }
                else if (dragMode == DragMode.Cartridge) FinishCartridgeDrag(now);
            }
        }

        void BeginCartridgeGrab(int cart)
        {
            selectedCartridge = cart;
            dragMode = DragMode.Cartridge;
            rotatedThisGrab = false;
            inserting = false;
            dragDistance = 0f;
            arcAccumulator = 0f;
            cartScreenCenter = CartridgeCenterScreen(cart);
            Vector2 v = pointerDown - cartScreenCenter;
            if (v.sqrMagnitude < 144f) v = Vector2.right * 24f;
            lastPointerAngle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
            SelectCartridgeVisual(cart, true);
            MgfSfx.Play("tap", .22f);
        }

        void UpdateCartridgeDrag(Vector2 now)
        {
            if (selectedCartridge < 0) return;
            dragDistance += Vector2.Distance(previousPointer, now);
            if (!inserting)
            {
                Vector2 v = now - cartScreenCenter;
                // 연습에서는 한 grab이 한 번의 90° 회전만 만든다. 이전 구현은 18°를
                // 넘을 때마다 계속 회전시켜 자연스러운 긴 원호가 180°/270°로 과진행했다.
                // 실전은 여러 사분면을 의도적으로 돌릴 수 있어 기존 누적 회전을 유지한다.
                bool mayRotate = gamePhase != GamePhase.Practice || !rotatedThisGrab;
                if (mayRotate && v.sqrMagnitude >= 100f)
                {
                    float angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
                    arcAccumulator += Mathf.DeltaAngle(lastPointerAngle, angle);
                    lastPointerAngle = angle;
                    if (Mathf.Abs(arcAccumulator) >= 18f)
                    {
                        int step = arcAccumulator > 0 ? 1 : -1;
                        cartridgeRotation[selectedCartridge] = gamePhase == GamePhase.Practice
                            ? 1
                            : (cartridgeRotation[selectedCartridge] + step + 4) % 4;
                        arcAccumulator = 0f;
                        rotatedThisGrab = true;
                        RotateCartridgeVisual(selectedCartridge, cartridgeRotation[selectedCartridge], true);
                        MgfSfx.Play("pop", .20f);
                    }
                }
                // 첫 연습은 어느 방향으로든 충분히 끌면 안내 궤적을 따라 90° 홈에 잠긴다.
                // 실전에서는 아래 보정이 없고 실제 원호 각도만 판정한다.
                if (gamePhase == GamePhase.Practice && !rotatedThisGrab && dragDistance >= 28f)
                {
                    cartridgeRotation[selectedCartridge] = 1;
                    rotatedThisGrab = true;
                    RotateCartridgeVisual(selectedCartridge, 1, true);
                    MgfSfx.Play("pop", .20f);
                }
                bool hasTurn = rotatedThisGrab || cartridgeRotation[selectedCartridge] != 0;
                float upward = now.y - pointerDown.y;
                if (hasTurn && (upward > 30f * Mathf.Max(1f, MgfText.Canvas.scaleFactor) || Vector2.Distance(now, cartScreenCenter) > 76f))
                    inserting = true;
            }
            if (inserting)
            {
                MoveCartridgeToScreen(selectedCartridge, now);
                UpdateTrail(cartScreenCenter, now, true);
                int dock = DockAtScreen(now, false);
                PreviewDock(dock);
            }
        }

        void FinishCartridgeDrag(Vector2 now)
        {
            int cart = selectedCartridge;
            dragMode = DragMode.None;
            selectedCartridge = -1;
            UpdateTrail(Vector2.zero, Vector2.zero, false);
            PreviewDock(-1);
            if (cart < 0) return;
            SelectCartridgeVisual(cart, false);

            if (!inserting)
            {
                ReturnCartridgeVisual(cart, cartridgeRotation[cart]);
                if (!rotatedThisGrab && cartridgeRotation[cart] == 0)
                {
                    RefuseAt(now, "먼저 손잡이를 원호로 돌려 90°씩 맞추시오");
                    ReplayGuide(false);
                }
                return;
            }

            int dock = DockAtScreen(now, true);
            ReturnCartridgeVisual(cart, cartridgeRotation[cart]);
            if (dock < 0)
            {
                RefuseAt(now, "연틀 입구 안에서 손을 놓으시오");
                ShakeNearestDock(now);
                return;
            }
            TryPlacement(cart, dock, cartridgeRotation[cart], false);
        }

        void HandleKeyboard()
        {
            if (gamePhase != GamePhase.Practice && gamePhase != GamePhase.Playing) return;
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                keyboardCart = (keyboardCart + 1) % YeonRules.CartCount;
                selectedCartridge = keyboardCart;
                SelectCartridgeVisual(keyboardCart, true);
                st.pointerVersion++; st.lastAction = "keyboard-cart"; MgfBridge.NotifyChanged();
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                int step = Input.GetKeyDown(KeyCode.RightArrow) ? 1 : -1;
                cartridgeRotation[keyboardCart] = (cartridgeRotation[keyboardCart] + step + 4) % 4;
                RotateCartridgeVisual(keyboardCart, cartridgeRotation[keyboardCart], true);
                st.pointerVersion++; st.lastAction = "keyboard-rotate"; MgfBridge.NotifyChanged();
            }
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                int step = Input.GetKeyDown(KeyCode.DownArrow) ? 1 : -1;
                for (int k = 0; k < 9; k++)
                {
                    keyboardDock = (keyboardDock + step + 9) % 9;
                    if (cells[keyboardDock] == 0) break;
                }
                PreviewDock(keyboardDock);
                st.pointerVersion++; st.lastAction = "keyboard-dock"; MgfBridge.NotifyChanged();
            }
            if (Input.GetKeyDown(KeyCode.Space))
            {
                st.pointerVersion++;
                if (gamePhase == GamePhase.Practice && keyboardCart != 0)
                    RefuseAt(Vector2.one * .5f, "연습 살대를 먼저 사용하시오");
                else TryPlacement(keyboardCart, keyboardDock, cartridgeRotation[keyboardCart], false);
                MgfBridge.NotifyChanged();
            }
        }

        void TryPlacement(int cartIndex, int dockIndex, int rotation, bool forced)
        {
            if (gamePhase != GamePhase.Practice && gamePhase != GamePhase.Playing) return;
            if (current == null || cartIndex < 0 || cartIndex >= current.cartridges.Length || dockIndex < 0 || dockIndex >= 9) return;
            if (gamePhase == GamePhase.Practice)
            {
                bool correctPractice = cartIndex == 0 && dockIndex == 4 && (rotation & 1) == 1
                    && YeonRules.IsAccepted(current.docks[4], current.cartridges[0], rotation);
                if (!correctPractice)
                {
                    st.lastAction = "practice-retry";
                    st.misconceptionId = "practice_needs_turn_and_center";
                    ShowWrongEvidence(current.docks[4], current.cartridges[cartIndex], rotation, true);
                    ReplayGuide(true);
                    MgfSfx.Play("wrong", .20f);
                    MgfBridge.NotifyChanged();
                    return;
                }
                revealCorrect = true;
                revealPractice = true;
                revealLineCount = 0;
                revealClearMask = 0;
                revealKnotMask = 0;
                revealClock = 0f;
                gamePhase = GamePhase.Reveal;
                st.lastAction = "practice-correct";
                st.misconceptionId = "";
                PlacePracticeSilk(dockIndex, cartIndex, rotation);
                ShowPracticeReveal();
                hitStopLeft = .07f;
                MgfSfx.Play("correct", .35f);
                MgfBridge.NotifyChanged();
                return;
            }

            if (cells[dockIndex] != 0)
            {
                RefuseAt(DockCenterScreen(dockIndex), "이미 바람줄이나 먹물 매듭이 놓인 연틀이다");
                ShakeDock(dockIndex);
                return;
            }

            DockSpec dock = current.docks[dockIndex];
            CartridgeSpec cart = current.cartridges[cartIndex];
            bool correct = YeonRules.IsAccepted(dock, cart, rotation);
            st.firstAttempts++;
            st.placements++;
            revealClock = 0f;
            revealPractice = false;
            revealCorrect = correct;
            revealWillEnd = false;
            selectedCartridge = -1;

            if (correct)
            {
                cells[dockIndex] = 1;
                st.firstCorrect++;
                st.correctPlacements++;
                st.score += 100;
                st.combo++;
                hitStopLeft = .075f;
                st.misconceptionId = "";
                st.lastAction = "correct-place";
                PlaceWindToken(dockIndex, cartIndex, rotation);
                FindCompletedLines(out revealClearMask, out revealLineCount);
                revealKnotMask = FindAdjacentKnots(revealClearMask);
                if (revealLineCount > 0)
                {
                    int reward = revealLineCount >= 2 ? 1000 : 400;
                    st.score += reward;
                    int gained = revealLineCount * 3;
                    st.launched += gained;
                    st.solved = st.launched;
                    st.combo += revealLineCount;
                    StartLineLaunch(revealClearMask, revealKnotMask, revealLineCount);
                    MgfSfx.Play("whoosh", .48f);
                }
                else
                {
                    ShowCorrectEvidence(dock, cart, rotation);
                    MgfSfx.Play("correct", .34f);
                }
            }
            else
            {
                cells[dockIndex] = 2;
                st.lives = Math.Max(0, st.lives - 1);
                st.combo = 0;
                st.masteryPossible = st.firstCorrect + Math.Max(0, 12 - st.firstAttempts) >= YeonRules.RequiredFirstCorrect;
                st.misconceptionId = YeonRules.ClassifyWrong(dock, cart, rotation);
                st.lastExplanation = YeonRules.WrongExplanation(dock, cart, rotation);
                st.lastAction = "wrong-knot";
                PlaceInkKnot(dockIndex, cartIndex, rotation);
                ShowWrongEvidence(dock, cart, rotation, false);
                ShakeWrong(dockIndex, st.lives <= 1);
                MgfSfx.Play("wrong", .30f);
                revealClearMask = 0;
                revealKnotMask = 0;
                revealLineCount = 0;
                revealWillEnd = st.lives <= 0;
            }

            SyncCells();
            gamePhase = GamePhase.Reveal;
            st.frozen = true;
            MgfBridge.NotifyChanged();
        }

        void FindCompletedLines(out int mask, out int count)
        {
            mask = 0; count = 0;
            for (int r = 0; r < 3; r++)
                if (cells[r * 3] == 1 && cells[r * 3 + 1] == 1 && cells[r * 3 + 2] == 1)
                { mask |= 7 << (r * 3); count++; }
            for (int c = 0; c < 3; c++)
                if (cells[c] == 1 && cells[3 + c] == 1 && cells[6 + c] == 1)
                { mask |= (1 << c) | (1 << (3 + c)) | (1 << (6 + c)); count++; }
        }

        int FindAdjacentKnots(int clearMask)
        {
            int knots = 0;
            for (int i = 0; i < 9; i++) if ((clearMask & (1 << i)) != 0)
            {
                int r = i / 3, c = i % 3;
                int[] dr = { -1, 1, 0, 0 }, dc = { 0, 0, -1, 1 };
                for (int k = 0; k < 4; k++)
                {
                    int rr = r + dr[k], cc = c + dc[k];
                    if (rr >= 0 && rr < 3 && cc >= 0 && cc < 3 && cells[rr * 3 + cc] == 2)
                        knots |= 1 << (rr * 3 + cc);
                }
            }
            return knots;
        }

        void FinishReveal()
        {
            if (gamePhase != GamePhase.Reveal) return;
            if (revealPractice)
            {
                ClearPracticeSilk();
                BeginRunAfterPractice();
                return;
            }

            if (revealCorrect && revealLineCount > 0)
            {
                for (int i = 0; i < 9; i++)
                {
                    if ((revealClearMask & (1 << i)) != 0) cells[i] = 0;
                    if ((revealKnotMask & (1 << i)) != 0) cells[i] = 0;
                }
                ClearLineVisuals(revealClearMask, revealKnotMask);
                SyncCells();
            }
            st.frozen = false;
            gamePhase = GamePhase.Playing;
            st.phase = "playing";
            if (revealWillEnd || st.lives <= 0) { EndRun("knots"); return; }
            if (st.launched >= YeonRules.TargetKites)
            {
                EndRun(st.firstCorrect >= YeonRules.RequiredFirstCorrect ? "clear" : "mastery_failed");
                return;
            }
            LoadBundle();
            SetPhaseUi();
            MgfBridge.NotifyChanged();
        }

        void EndRun(string reason)
        {
            if (gamePhase == GamePhase.End) return;
            gamePhase = GamePhase.End;
            dragMode = DragMode.None;
            endReason = reason;
            st.frozen = true;
            st.onboarding = false;
            st.phase = reason == "clear" ? "clear" : reason;
            if (reason == "clear") MgfSfx.Play("win", .50f); else MgfSfx.Play("lose", .38f);
            ShowEnd(reason);
            SetPhaseUi();
            MgfBridge.NotifyChanged();
        }

        void SyncCells()
        {
            for (int i = 0; i < cells.Length; i++) st.cells[i] = cells[i];
        }

        // QA 훅은 실제 TryPlacement 판정 경로를 사용한다.
        public void TestStart() => StartRunDirect();

        public void TestAnswerCorrect()
        {
            if (gamePhase == GamePhase.End) return;
            if (gamePhase == GamePhase.Title || gamePhase == GamePhase.Practice) StartRunDirect();
            if (gamePhase == GamePhase.Reveal) FinishReveal();
            if (gamePhase != GamePhase.Playing || current == null || current.accepted.Length == 0) return;
            PlacementSpec p = current.accepted[0];
            TryPlacement(p.cartridge, p.dock, p.rotation, true);
        }

        public void TestAnswerWrong()
        {
            if (gamePhase == GamePhase.End) return;
            if (gamePhase == GamePhase.Title || gamePhase == GamePhase.Practice) StartRunDirect();
            if (gamePhase == GamePhase.Reveal) FinishReveal();
            if (gamePhase != GamePhase.Playing || current == null) return;
            for (int d = 0; d < 9; d++) if (cells[d] == 0)
            for (int c = 0; c < 3; c++)
            for (int r = 0; r < 4; r++)
                if (!YeonRules.IsAccepted(current.docks[d], current.cartridges[c], r))
                { TryPlacement(c, d, r, true); return; }
        }

        public string StateJson()
        {
            UpdateControlCoordinates();
            return JsonUtility.ToJson(st);
        }

        public string ProblemBankJson()
        {
            return JsonUtility.ToJson(new YeonBankWrap { items = bank.ToArray() });
        }
    }
}
