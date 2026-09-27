// Headless editor test of the production movement/dwell/answer model.
// No blind bot calls Select(), AnswerPad(), or reads answer labels.
// Run after the source has been synced by the normal build script:
// Unity -batchmode -nographics -projectPath <workspace>
//   -executeMethod Mgf.HyeopgokSasu.HyeopgokBotSelfTest.Run
//   -hyeopgokRepo /Users/sitpo/math-game-factory -quit
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Mgf.HyeopgokSasu
{
    public static class HyeopgokBotSelfTest
    {
        const int Seed = 20260926, Games = 200;
        const float Dt = 1f / 60f;
        enum Bot { Mash, Cycle, RandomCoordinates, Idle, Nearest, RandomPad, Fixed1, Fixed2, Fixed3, Perfect, FirstWrong }
        [Serializable] public sealed class Result
        {
            public string pack, bot, strategy;
            public int games, answered, correct, timeouts, victories, completed, maximumSolved;
            public int symmetricAnswered, symmetricCorrect, symmetricErrors;
            public double firstAttemptRate, ci95Low, ci95High, victoryRate, meanSeconds;
            public bool blind, nominalChanceGate;
        }
        [Serializable] public sealed class Report
        {
            public int seed = Seed, gamesPerBotPerPack = Games;
            public string seedDerivation = "Unchanged master seed 20260926; production uses fixed published SplitMix64 stream domains 0 and 1. No favorable seed search.";
            public string model = "production HyeopgokRules.Move + Tick, dt=1/60, hold=0.8, cooldown=1.55";
            public string limitation = "Combat DrainGateDamage is excluded; browser integration validates real combat.";
            public bool nominalChanceGate, symmetricChanceGate, controlsPassed;
            public Result[] results;
            public ExactExpectation exactStoppedExpectation;
            public string[] failures;
        }
        [Serializable] public sealed class ExactExpectation
        {
            public string method = "All 1024 geometry-only timeout schedules × complete 4^10 answer-position paths, collapsed by exact integer branch weights";
            public int timeoutSchedules = 1024, waves = 10, startingHp = 100, wrongDamage = 18;
            public long labelPathsPerSchedule = 1048576;
            public long terminalPrefixes, zeroLiftFailures, pathMassFailures;
            public long noTimeoutAnsweredMass, noTimeoutCorrectMass, noTimeoutVictoryMass;
            public long idleAnsweredMass, idleCorrectMass, idleVictoryMass;
            public string ratio = "For every nonempty schedule: 4 × correct mass = answered mass exactly. Idle: zero answered, zero correct.";
            public string limitation = "Expected-value proof under independently uniform answer positions; does not overwrite or pass finite raw observations.";
        }
        sealed class Mass
        {
            public long paths, answered, correct, victories, terminals;
        }
        static long Pow4(int power) => 1L << (2 * power);
        static void EnumeratePrefixes(int timeoutMask, int depth, int hp, int correct, int answered, long weight, Mass mass)
        {
            // Each valid pad contributes one correct label position and three
            // incorrect positions. A geometric timeout ignores all four labels.
            // On early death, all unobserved suffix label sequences still exist.
            if (hp <= 0 || depth == 10)
            {
                long fullPathWeight = weight * Pow4(10 - depth);
                mass.paths += fullPathWeight;
                mass.answered += answered * fullPathWeight;
                mass.correct += correct * fullPathWeight;
                if (hp > 0 && depth == 10 && correct >= 7) mass.victories += fullPathWeight;
                mass.terminals++;
                return;
            }
            if ((timeoutMask & (1 << depth)) != 0)
            {
                EnumeratePrefixes(timeoutMask, depth + 1, Math.Max(0, hp - 18), correct, answered, weight * 4, mass);
            }
            else
            {
                EnumeratePrefixes(timeoutMask, depth + 1, hp, correct + 1, answered + 1, weight, mass);
                EnumeratePrefixes(timeoutMask, depth + 1, Math.Max(0, hp - 18), correct, answered + 1, weight * 3, mass);
            }
        }
        static ExactExpectation ProveStoppedExpectation()
        {
            var proof = new ExactExpectation();
            // This covers any seed's deterministic geometry-only timeout
            // schedule, including random-coordinate inputs. The king resets at
            // every wave and these bots do not adapt input to correctness.
            for (int timeoutMask = 0; timeoutMask < 1024; timeoutMask++)
            {
                var mass = new Mass();
                EnumeratePrefixes(timeoutMask, 0, 100, 0, 0, 1, mass);
                proof.terminalPrefixes += mass.terminals;
                if (mass.paths != Pow4(10)) proof.pathMassFailures++;
                if (mass.correct * 4 != mass.answered) proof.zeroLiftFailures++;
                if (timeoutMask == 0)
                {
                    proof.noTimeoutAnsweredMass = mass.answered; proof.noTimeoutCorrectMass = mass.correct; proof.noTimeoutVictoryMass = mass.victories;
                }
                if (timeoutMask == 1023)
                {
                    proof.idleAnsweredMass = mass.answered; proof.idleCorrectMass = mass.correct; proof.idleVictoryMass = mass.victories;
                }
            }
            if (proof.zeroLiftFailures != 0 || proof.pathMassFailures != 0 || proof.idleCorrectMass != 0) Failures.Add("Whole-run exact expectation failed");
            return proof;
        }
        [Serializable] public sealed class SeedIntervalDiagnostic
        {
            public string pack;
            public int firstSeed, lastSeed, games = 200;
            public int[] pairedSlots, crossPairedSlots, pairedWaveSlots;
            public double[] pairedRates, crossPairedRates;
            public string scope = "Predetermined interval sensitivity only; never replaces the fixed primary bot result.";
        }
        [Serializable] public sealed class SeedDiagnosticReport
        {
            public string version = "POST-FIX: fixed published SplitMix64 master seed expansion into stream domains 0 and 1";
            public string design = "Four intervals chosen before execution: 0..199, 100000..100199, 20260926..20261125, Int32.MaxValue-199..Int32.MaxValue. All intervals are retained.";
            public SeedIntervalDiagnostic[] intervals;
        }
        static void RunSeedDiagnostics(string repo, string gameRoot)
        {
            var rows = new List<SeedIntervalDiagnostic>();
            foreach (string id in new[] { "m2s2-u7", "m2s2-u6" })
            {
                var pack = JsonUtility.FromJson<QuestionPack>(File.ReadAllText(Path.Combine(repo, "public/g/hyeopgok-sasu/packs/" + id + ".json")));
                foreach (int first in new[] { 0, 100000, 20260926, int.MaxValue - 199 })
                {
                    var row = new SeedIntervalDiagnostic { pack = id, firstSeed = first, lastSeed = first + 199, pairedSlots = new int[4], crossPairedSlots = new int[4], pairedWaveSlots = new int[40], pairedRates = new double[4], crossPairedRates = new double[4] };
                    var native = new int[2000]; var shuffle = new int[8000];
                    for (int game = 0; game < 200; game++)
                    {
                        int seed = first + game; var rules = new HyeopgokRules(); rules.Start(pack, seed);
                        for (int wave = 0; wave < 10; wave++)
                        {
                            int offset = game * 10 + wave;
                            native[offset] = Array.IndexOf(rules.Current.choices, rules.Current.answer);
                            for (int pad = 0; pad < 4; pad++) shuffle[offset * 4 + pad] = Array.IndexOf(rules.Current.choices, rules.Choices[pad]);
                            int slot = rules.AnswerPad(); row.pairedSlots[slot]++; row.pairedWaveSlots[wave * 4 + slot]++;
                            // Diagnostic captures all ten question/placement draws;
                            // this explicit perfect control prevents death from
                            // censoring the marginal distribution. It is not a bot
                            // pass measurement and never replaces the primary run.
                            PlayQuestion(rules, Bot.Perfect, seed, wave + 1); rules.Next();
                        }
                    }
                    // All order/placement pairings within the same preregistered
                    // seed interval, never a selected favorable offset.
                    for (int orderGame = 0; orderGame < 200; orderGame++)
                        for (int placeGame = 0; placeGame < 200; placeGame++)
                            for (int wave = 0; wave < 10; wave++)
                                for (int pad = 0; pad < 4; pad++)
                                    if (shuffle[(placeGame * 10 + wave) * 4 + pad] == native[orderGame * 10 + wave]) row.crossPairedSlots[pad]++;
                    for (int pad = 0; pad < 4; pad++) { row.pairedRates[pad] = row.pairedSlots[pad] / 2000.0; row.crossPairedRates[pad] = row.crossPairedSlots[pad] / 400000.0; }
                    rows.Add(row);
                }
            }
            var report = new SeedDiagnosticReport { intervals = rows.ToArray() };
            File.WriteAllText(Path.Combine(gameRoot, "ArtSource/validation/bot-seed-diagnostic.json"), JsonUtility.ToJson(report, true) + "\n");
            var md = new StringBuilder("# 도메인 분리 후 사전 고정 시드 구간 진단\n\n");
            md.AppendLine("기준 봇 시드와 표본 수는 바꾸지 않았다. 별도 민감도 진단으로 실행 전에 0~199, 100000~100199, 20260926~20261125, 2147483448~2147483647의 네 구간을 정했다. 구간·상수를 결과에 맞춰 선택하지 않았으며 모든 결과를 남긴다. 프로덕션의 실제 HyeopgokRules가 문항과 보기를 만들고, 대조군 입력으로 10문항까지 진행해 종료에 가려지는 배치도 기록했다. 이 분포 진단을 무뇌 봇 합격 결과로 사용하지 않는다.\n");
            md.AppendLine("| 팩 | 시드 구간 | 원래 연결의 정답 위치 1·2·3·4 | 동일 구간 전체 교차의 위치 1·2·3·4 |\n|---|---|---|---|");
            foreach (var row in rows) md.AppendLine("| " + row.pack + " | " + row.firstSeed + "~" + row.lastSeed + " | " + string.Join(" / ", Array.ConvertAll(row.pairedRates, Percent)) + " | " + string.Join(" / ", Array.ConvertAll(row.crossPairedRates, Percent)) + " |");
            md.AppendLine("\n각 원래 구간은 200판 × 10문항 = 2,000개 배치다. 출제 열·배치 열을 같은 200시드 집합 안에서 전부 교차한 40,000쌍은 400,000개 배치이며, 원래 두 열의 연결에 대한 민감도를 보여 준다. 이것은 무한 시드 독립성의 증명이 아니며, 특정 위치의 유한 표본 초과를 숨기거나 기존 원시 ≤25% 기준을 대체하지 않는다.");
            File.WriteAllText(Path.Combine(gameRoot, "ArtSource/validation/bot-seed-diagnostic.md"), md.ToString());
        }
        static readonly List<string> Failures = new List<string>();
        static readonly List<Result> Results = new List<Result>();
        static bool IsBlind(Bot bot) => bot != Bot.Perfect && bot != Bot.FirstWrong;
        static string Label(Bot bot)
        {
            switch (bot)
            {
                case Bot.Mash: return "연타";
                case Bot.Cycle: return "순환";
                case Bot.RandomCoordinates: return "무작위 좌표";
                case Bot.Idle: return "무입력";
                case Bot.Nearest: return "가장 가까운 패드";
                case Bot.RandomPad: return "무작위 패드";
                case Bot.Fixed1: return "고정 패드 2";
                case Bot.Fixed2: return "고정 패드 3";
                case Bot.Fixed3: return "고정 패드 4";
                case Bot.Perfect: return "정답 대조군";
                default: return "첫 오답 뒤 회복 대조군";
            }
        }
        static string Strategy(Bot bot)
        {
            switch (bot)
            {
                case Bot.Mash: return "항상 패드 1 같은 좌표를 0.2초마다 반복";
                case Bot.Cycle: return "패드 1→2→3→4 순서, 문항 내에도 2.4초마다 순환";
                case Bot.RandomCoordinates: return "1.5초마다 이동 가능 영역의 무작위 좌표, 빈 땅 포함";
                case Bot.Idle: return "이동 명령 없음, 난도별 실제 24/32/40초 제한까지 방치";
                case Bot.Nearest: return "정답을 읽지 않고 왕과의 거리 제곱이 최소인 패드";
                case Bot.RandomPad: return "네 패드 중 균등 무작위 하나를 골라 정지";
                case Bot.Fixed1: return "항상 패드 2";
                case Bot.Fixed2: return "항상 패드 3";
                case Bot.Fixed3: return "항상 패드 4";
                case Bot.Perfect: return "정답 위치까지 실제 이동·0.8초 정지";
                default: return "첫 문항만 오답 위치로 이동, 나머지는 정답 위치로 이동";
            }
        }
        static int Nearest(HyeopgokRules rules)
        {
            int nearest = 0; float distance = float.MaxValue;
            for (int i = 0; i < HyeopgokRules.Pads.Length; i++)
            {
                float candidate = (HyeopgokRules.Pads[i] - rules.King).sqrMagnitude;
                if (candidate < distance) { distance = candidate; nearest = i; }
            }
            return nearest;
        }
        static int EpisodeSeed(int gameSeed, int wave, Bot bot)
        {
            // Fixed before any results are observed. Separate deterministic input
            // RNG keeps blind decisions independent of production choice shuffling.
            unchecked { return gameSeed * 397 ^ wave * 8191 ^ (int)bot * 104729; }
        }
        static int PlayQuestion(HyeopgokRules rules, Bot bot, int gameSeed, int wave)
        {
            var inputs = new System.Random(EpisodeSeed(gameSeed, wave, bot));
            int randomPad = inputs.Next(4);
            int controlPad = -1;
            if (!IsBlind(bot))
            {
                controlPad = rules.AnswerPad();
                if (bot == Bot.FirstWrong && wave == 1) controlPad = (controlPad + 1) % 4;
            }
            int tickLimit = (int)Math.Ceiling(rules.TimeLimit / Dt) + 60;
            for (int tick = 0; tick < tickLimit; tick++)
            {
                switch (bot)
                {
                    case Bot.Mash:
                        if (tick % 12 == 0) rules.Move(HyeopgokRules.Pads[0]); break;
                    case Bot.Cycle:
                        if (tick % 144 == 0) rules.Move(HyeopgokRules.Pads[(wave - 1 + tick / 144) % 4]); break;
                    case Bot.RandomCoordinates:
                        if (tick % 90 == 0) rules.Move(new Vector3(-2.75f + (float)inputs.NextDouble() * 4.23f, 1.24f, -4.2f + (float)inputs.NextDouble() * 7.75f)); break;
                    case Bot.Idle: break;
                    case Bot.Nearest:
                        if (tick == 0) rules.Move(HyeopgokRules.Pads[Nearest(rules)]); break;
                    case Bot.RandomPad:
                        if (tick == 0) rules.Move(HyeopgokRules.Pads[randomPad]); break;
                    case Bot.Fixed1: if (tick == 0) rules.Move(HyeopgokRules.Pads[1]); break;
                    case Bot.Fixed2: if (tick == 0) rules.Move(HyeopgokRules.Pads[2]); break;
                    case Bot.Fixed3: if (tick == 0) rules.Move(HyeopgokRules.Pads[3]); break;
                    default: if (tick == 0) rules.Move(HyeopgokRules.Pads[controlPad]); break;
                }
                rules.Tick(Dt);
                if (rules.Pending || rules.Ended) return tick + 1;
            }
            Failures.Add("Question did not resolve within its difficulty deadline: " + Label(bot));
            return tickLimit;
        }
        static void SymmetricReplay(Result result, PackItem item, string[] displayed, Bot bot, int gameSeed, int wave)
        {
            // Counterfactual four rotations of EXACTLY the same item, geometry,
            // movement commands and input seed. Only labels change places.
            // This is additional evidence, never a replacement for the raw run.
            int correct = 0, answered = 0; int firstPad = int.MinValue;
            var one = new QuestionPack { pack_id = "counterfactual", items = new[] { item } };
            for (int rotation = 0; rotation < 4; rotation++)
            {
                var rules = new HyeopgokRules(); rules.Start(one, gameSeed);
                for (int i = 0; i < 4; i++) rules.Choices[i] = displayed[(i + rotation) % 4];
                PlayQuestion(rules, bot, gameSeed, wave);
                if (firstPad == int.MinValue) firstPad = rules.LastPad;
                else if (firstPad != rules.LastPad) result.symmetricErrors++;
                if (rules.LastPad >= 0) { answered++; if (rules.LastCorrect) correct++; }
            }
            result.symmetricAnswered += answered; result.symmetricCorrect += correct;
            if (!(answered == 0 && correct == 0 || answered == 4 && correct == 1)) result.symmetricErrors++;
        }
        static void Wilson(Result result)
        {
            if (result.answered == 0) { result.firstAttemptRate = result.ci95Low = result.ci95High = 0; return; }
            double n = result.answered, p = result.correct / n, z = 1.95996398454005;
            double scale = 1 + z * z / n, center = (p + z * z / (2 * n)) / scale;
            double half = z * Math.Sqrt(p * (1 - p) / n + z * z / (4 * n * n)) / scale;
            result.firstAttemptRate = p; result.ci95Low = center - half; result.ci95High = center + half;
        }
        static Result RunBot(QuestionPack pack, Bot bot)
        {
            var result = new Result { pack = pack.pack_id, bot = Label(bot), strategy = Strategy(bot), games = Games, blind = IsBlind(bot) };
            long totalTicks = 0;
            for (int game = 0; game < Games; game++)
            {
                int seed = Seed + game;
                var rules = new HyeopgokRules(); rules.Start(pack, seed);
                int safety = 0;
                while (!rules.Ended && safety++ < 12)
                {
                    int wave = rules.Wave; var current = rules.Current;
                    var displayed = (string[])rules.Choices.Clone();
                    totalTicks += PlayQuestion(rules, bot, seed, wave);
                    if (rules.LastPad >= 0) { result.answered++; if (rules.LastCorrect) result.correct++; }
                    else result.timeouts++;
                    if (result.blind) SymmetricReplay(result, current, displayed, bot, seed, wave);
                    // The production game reveals feedback before Next; while
                    // Pending, Tick rejects all further answer attempts.
                    int before = rules.Attempts;
                    for (int cooldown = 0; cooldown < 93; cooldown++) rules.Tick(Dt);
                    if (rules.Attempts != before) Failures.Add("Pending allowed duplicate attempt");
                    totalTicks += 93;
                    rules.Next();
                }
                if (!rules.Ended) Failures.Add(pack.pack_id + ": unfinished " + Label(bot));
                if (rules.Wave == 10 && rules.Attempts == 10) result.completed++;
                if (rules.Won) result.victories++;
                result.maximumSolved = Math.Max(result.maximumSolved, rules.Correct);
                if (bot == Bot.Idle && (rules.Correct != 0 || rules.Score != 0)) Failures.Add("Idle made learning progress");
                if (bot == Bot.Perfect && (!rules.Won || rules.Correct != 10 || rules.Hp != 100)) Failures.Add("Perfect control failed");
                if (bot == Bot.FirstWrong && (!rules.Won || rules.Correct != 9 || rules.Hp != 82)) Failures.Add("First-error recovery failed");
            }
            Wilson(result);
            result.nominalChanceGate = !result.blind || result.answered == 0 || result.correct * 4 <= result.answered;
            result.victoryRate = result.victories / (double)Games;
            result.meanSeconds = totalTicks * Dt / Games;
            return result;
        }
        static string Percent(double n) => (100 * n).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "%";
        public static void Run()
        {
            Failures.Clear(); Results.Clear();
            string repo = Environment.GetEnvironmentVariable("MGF_HYEOPGOK_REPO");
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-hyeopgokRepo") repo = args[i + 1];
            if (string.IsNullOrEmpty(repo)) repo = "/Users/sitpo/math-game-factory";
            string gameRoot = Path.Combine(repo, "factory/unity-src/hyeopgok-sasu");
            foreach (string id in new[] { "m2s2-u7", "m2s2-u6" })
            {
                var pack = JsonUtility.FromJson<QuestionPack>(File.ReadAllText(Path.Combine(repo, "public/g/hyeopgok-sasu/packs/" + id + ".json")));
                foreach (var item in pack.items)
                {
                    var timed = new HyeopgokRules();
                    timed.Start(new QuestionPack { items = new[] { item } }, Seed);
                    float expected = item.difficulty <= 1 ? 24 : item.difficulty == 2 ? 32 : 40;
                    if (timed.TimeLimit != expected) Failures.Add(item.id + ": wrong deadline");
                    timed.Tick(expected - .02f);
                    if (timed.Pending || timed.Attempts != 0) Failures.Add(item.id + ": early timeout");
                    timed.Tick(.03f);
                    if (!timed.Pending || timed.LastCorrect || timed.LastPad != -1 || timed.Attempts != 1 || timed.Hp != 82) Failures.Add(item.id + ": timeout outcome changed");
                }
                foreach (Bot bot in Enum.GetValues(typeof(Bot))) Results.Add(RunBot(pack, bot));
            }
            bool nominal = true, symmetric = true, controls = true;
            foreach (var r in Results)
            {
                if (r.blind && !r.nominalChanceGate) nominal = false;
                if (r.blind && (r.symmetricErrors != 0 || r.symmetricCorrect * 4 != r.symmetricAnswered)) symmetric = false;
                if (!r.blind && r.victories != Games) controls = false;
            }
            var exact = ProveStoppedExpectation();
            var report = new Report { exactStoppedExpectation = exact, nominalChanceGate = nominal, symmetricChanceGate = symmetric, controlsPassed = controls, results = Results.ToArray(), failures = Failures.ToArray() };
            Directory.CreateDirectory(Path.Combine(gameRoot, "ArtSource/validation"));
            File.WriteAllText(Path.Combine(gameRoot, "ArtSource/validation/bot-results.json"), JsonUtility.ToJson(report, true) + "\n");
            var md = new StringBuilder();
            md.AppendLine("# 협곡 사수 — 무뇌 봇 및 회복 경로 검증\n");
            md.AppendLine("- 고정 기준 시드: **20260926**. 각 팩·각 봇 **200판**, 판별 시드는 기준 시드 + 판 번호. 결과를 보고 시드나 입력 주기를 고르지 않았다.");
            md.AppendLine("- 기준 시드·200판·입력 주기는 이전과 동일하다. 수정 전 관련 시드 스트림 진단을 근거로 프로덕션은 고정된 표준 SplitMix64로 도메인 0(출제)·1(보기)을 분리했다. 결과에 맞춘 시드·상수 탐색은 하지 않았다. 수정 전 관측값은 `validation/bot-results-before-seed-fix.md/json`에 보존했다.");
            md.AppendLine("- 게임에서 쓰는 **HyeopgokRules**를 직접 실행. Move → Tick(1/60초) → 왕 이동 → 패드에서 0.8초 정지 → Select. 무뇌 봇은 Select·AnswerPad를 호출하지 않고 정답 문구도 읽지 않는다.");
            md.AppendLine("- 문제별 첫 패드 확정만 분모에 포함. 난도별 24/32/40초 시간초과는 별도 집계하므로 무작위 좌표의 빈 땅 입력이 정답률을 인위적으로 낮추지 않는다. 오답·시간초과 후 1.55초 피드백 동안 중복 확정 금지도 검사했다. 전 팩 문항에서 제한 0.02초 전 미확정, 제한 0.01초 후 오답·성문 -18 경계도 검사했다.");
            md.AppendLine("- HP 100, 오답 18, 10문항, 정답 7개 이상이면 승리. **전투의 DrainGateDamage는 이 모델 검증에서 제외**한다. 실제 전투 승패·회복 가능성은 브라우저 통합 검증에서 따로 확인해야 한다.");
            md.AppendLine("- 무입력의 학습 진도는 정답 수·점수 모두 0이다. 시간초과는 실패 처리되어 다음 웨이브로 가지만 정답·학습 보상을 주지 않는다.\n");
            md.AppendLine("| 팩 | 봇 | 첫 시도 정답/확정 | 정답률 | Wilson 95% 구간 | 시간초과 | 10문항 완료 | 승리 | 원시 ≤25% |\n|---|---|---:|---:|---|---:|---:|---:|---|");
            foreach (var r in Results)
                md.AppendLine("| " + r.pack + " | " + r.bot + " | " + r.correct + "/" + r.answered + " | " + Percent(r.firstAttemptRate) + " | " + Percent(r.ci95Low) + "–" + Percent(r.ci95High) + " | " + r.timeouts + " | " + r.completed + "/200 | " + r.victories + "/200 | " + (r.blind ? (r.nominalChanceGate ? "통과" : "초과 — 아래 해석") : "대조군") + " |");
            md.AppendLine("\n## 입력 전략\n");
            foreach (Bot bot in Enum.GetValues(typeof(Bot))) md.AppendLine("- " + Label(bot) + ": " + Strategy(bot) + ".");
            md.AppendLine("\n## 대칭 반사실 검증\n");
            md.AppendLine("원시 실행에서 만난 각 문항을 **같은 문제·같은 왕 시작점·같은 이동 입력 시드**로 네 번 다시 실행하되, 보기 위치만 0·1·2·3칸 순환했다. 왕의 이동과 확정 패드가 네 실행에서 같고, 패드를 확정한 경우 네 번 중 정확히 한 번만 정답이어야 한다. 시간초과면 네 번 모두 오답이다. 정답 배치와 기하 선택의 독립성을 검사하는 추가 증거이며, 위 원시 관측값을 대체하거나 숨기지 않는다.\n");
            md.AppendLine("| 팩 | 봇 | 대칭 정답/확정 | 대칭 정답률 | 입력·대칭 불일치 |\n|---|---|---:|---:|---:|");
            foreach (var r in Results) if (r.blind) md.AppendLine("| " + r.pack + " | " + r.bot + " | " + r.symmetricCorrect + "/" + r.symmetricAnswered + " | " + Percent(r.symmetricAnswered == 0 ? 0 : r.symmetricCorrect / (double)r.symmetricAnswered) + " | " + r.symmetricErrors + " |");
            md.AppendLine("\n## 조기 종료까지 포함한 완전판 정확 기대값\n");
            md.AppendLine("문항별 정답 위치가 독립·균등이라는 우연 모형에서 가능한 정답 위치 경로는 4¹⁰ = 1,048,576개다. 실제 움직임 대칭 검증에서 무뇌 봇의 확정 위치가 답 위치와 무관함을 확인했으므로, 확정된 패드 한 곳은 정답 갈래 가중치 1, 오답 갈래 가중치 3으로 묶을 수 있다. 빈 땅에서 시간초과한 문항은 네 답 배치 모두 실패하므로 가중치 4다.");
            md.AppendLine("HP 100·오답/시간초과 피해 18·6회 실패 시 종료·최대 10문항·7정답 승리 조건을 그대로 적용했다. 깊이 d에서 조기 종료한 접두 경로는 관측되지 않은 뒤쪽 배열 4^(10−d)개를 곱해 **완전한 판의 경로 질량을 보존**했다. 종료한 판을 버리거나 정답률 분모를 고정하지 않았다.");
            md.AppendLine("특정 무작위 좌표 결과에 맞추지 않도록 10개 웨이브의 가능한 **시간초과 여부 2¹⁰ = 1,024개 패턴 전부**에 위 계산을 수행했다. 이동·정지 경로가 정답 문구와 무관한 연타·순환·무작위 좌표·가까운 패드·무작위 패드·고정 패드 모두 이 패턴 중 하나에 속한다. 무입력은 전부 시간초과하는 패턴이다.\n");
            md.AppendLine("- 열거한 종료 접두 경로: " + exact.terminalPrefixes + "개. 각 시간초과 패턴의 완전 경로 질량은 정확히 1,048,576; 질량 불일치 " + exact.pathMassFailures + "건.");
            md.AppendLine("- 모든 1,024개 패턴에서 **4 × 정답 질량 = 확정 질량**; 불일치 " + exact.zeroLiftFailures + "건. 따라서 정답 기대 횟수 / 확정 기대 횟수는 정확히 **25%**(확정이 있는 경우), 무입력은 둘 다 **0**.");
            md.AppendLine("- 시간초과가 없는 완전판 모형: 정답 질량 " + exact.noTimeoutCorrectMass + " / 확정 질량 " + exact.noTimeoutAnsweredMass + " = 정확히 25%. 승리 경로 질량 " + exact.noTimeoutVictoryMass + " / 1,048,576.");
            md.AppendLine("- 무입력: 확정 질량 " + exact.idleAnsweredMass + ", 정답 질량 " + exact.idleCorrectMass + ", 승리 질량 " + exact.idleVictoryMass + ".");
            md.AppendLine("- 이것은 **독립·균등 배치에서의 전체 판 기대값 증명**이다. 실제 고정 시드의 유한 원시 표본이나 PRNG의 모든 시드를 전수 검사한 결과가 아니며, 원시 ≤25% 기준을 대신 통과시키지 않는다. E[정답/확정]과 E[정답]/E[확정]도 구별해야 한다. 여기서 증명한 것은 전체 집계 첫 시도 비율에 대응하는 후자다.");
            md.AppendLine("- 같은 순열 24개를 팩 전체 문항에 일괄 적용하는 방식은 문항 간 정오 상관을 남기므로 이 독립 배치 전수 증명과 다르다.");
            md.AppendLine("- 공정한 독립 4보기에서도 유한 N번을 모두 우연히 맞힐 확률 4^(−N)은 0보다 크다. 따라서 모든 유한 표본에서 관측 정답률 ≤25%를 보장하는 조건은 공정한 우연 모형 자체와 양립하지 않는다. 시드를 바꾸거나 결과를 보고 표본 수를 늘려 기준 안으로 들어올 때만 보고하지 않았다.");
            md.AppendLine("\n## 판정과 한계\n");
            md.AppendLine("- 원시 관측값이 모든 무뇌 봇에서 25% 이하: **" + (nominal ? "통과" : "미충족") + "**. 25%를 넘는 행도 원본 그대로 기록했다.");
            md.AppendLine("- 네 방향 대칭의 정확한 우연 수준: **" + (symmetric ? "통과" : "실패") + "**. 원시값의 25% 초과가 학습 없이 생기는 구조적 이득인지, 유한 표본 변동인지 이 결과와 신뢰구간을 함께 보아야 한다. 사용자의 문자 그대로인 원시 ≤25% 조건을 이 통계 설명으로 통과 처리하지 않는다.");
            md.AppendLine("- 정답 대조군 200판 전승 및 첫 오답 후 200판 회복: **" + (controls ? "통과" : "실패") + "**(팩별). 모델 단위 검증 오류 " + Failures.Count + "건.");
            md.AppendLine("- 게임 전투 피해·브라우저 입력 전달·프레임률·가독성은 이 검증 범위 밖이다. `ArtSource/validation/bot-results.json`에 원시 집계가 있다.");
            if (Failures.Count > 0) { md.AppendLine("\n오류:"); foreach (var failure in Failures) md.AppendLine("- " + failure); }
            File.WriteAllText(Path.Combine(gameRoot, "ArtSource/bot-results.md"), md.ToString());
            RunSeedDiagnostics(repo, gameRoot);
            Debug.Log("HYEOPGOK_BOTS " + JsonUtility.ToJson(report));
            EditorApplication.Exit(Failures.Count > 0 || !symmetric || !controls ? 1 : nominal ? 0 : 2);
        }
    }
}
#endif
