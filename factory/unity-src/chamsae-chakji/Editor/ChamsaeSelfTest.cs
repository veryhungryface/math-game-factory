#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Mgf.ChamsaeChakji.Editor
{
    public static class ChamsaeSelfTest
    {
        [Serializable] sealed class BotResult
        {
            public string bot;
            public int games;
            public int attempts;
            public int correct;
            public float firstAttemptRate;
            public int earlyAttempts;
            public int earlyCorrect;
            public float earlyRate;
            public int lateAttempts;
            public int lateCorrect;
            public float lateRate;
            public float ci95Low;
            public float ci95High;
            public int completed;
            public float completionRate;
            public int progress;
        }

        [Serializable] sealed class Report
        {
            public string generatedAt;
            public int bankCount;
            public int uniquePrompts;
            public int invalidRecords;
            public int accidentalTruths;
            public int band1Count;
            public int band2Count;
            public int band3Count;
            public int distinctBand2Pairs;
            public int distinctBand3Pairs;
            public float chanceEarly = 1f / 9f;
            public float chanceLate = 1f / 32f;
            public BotResult[] bots;
            public bool passed;
        }

        public static void Run()
        {
            var pool = ChamsaeRules.BuildProblems();
            var prompts = new HashSet<string>();
            var pairs2 = new HashSet<string>();
            var pairs3 = new HashSet<string>();
            int invalid = 0, b1 = 0, b2 = 0, b3 = 0;
            for (int i = 0; i < pool.Count; i++)
            {
                var p = pool[i];
                prompts.Add(p.prompt + "|" + p.ToMgf().answer);
                if (!ChamsaeRules.ValidateRecord(p)) invalid++;
                if (p.band == 1) b1++;
                else if (p.band == 2) { b2++; pairs2.Add(p.answerA + "," + p.answerB); }
                else { b3++; pairs3.Add(p.answerA + "," + p.answerB); }
            }

            var bots = new[]
            {
                Simulate("repeat", pool, 200, 0x1101u),
                Simulate("cycle", pool, 200, 0x2202u),
                Simulate("random", pool, 200, 0x3303u),
                Simulate("noinput", pool, 200, 0x4404u)
            };
            bool botsPass = true;
            for (int i = 0; i < bots.Length; i++)
            {
                var b = bots[i];
                if (b.bot == "noinput") botsPass &= b.attempts == 0 && b.progress == 0;
                else botsPass &= b.earlyRate <= 1f / 9f + .00001f && b.lateRate <= 1f / 32f + .00001f;
            }
            var report = new Report
            {
                generatedAt = DateTime.UtcNow.ToString("o"), bankCount = pool.Count, uniquePrompts = prompts.Count,
                invalidRecords = invalid, accidentalTruths = invalid, band1Count = b1, band2Count = b2, band3Count = b3,
                distinctBand2Pairs = pairs2.Count, distinctBand3Pairs = pairs3.Count, bots = bots,
                passed = invalid == 0 && prompts.Count == pool.Count && pool.Count >= 300 && pairs2.Count == 32 && pairs3.Count == 32 && botsPass
            };
            string path = Environment.GetEnvironmentVariable("CHAMSAE_VALIDATION_OUT");
            if (string.IsNullOrEmpty(path)) path = Path.Combine(Directory.GetCurrentDirectory(), "chamsae-validation.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(report, true));
            Debug.Log("[CHAMSAE_SELFTEST] " + (report.passed ? "PASS" : "FAIL") + " bank=" + pool.Count + " invalid=" + invalid + " out=" + path);
            if (!report.passed) throw new Exception("참새 착지 자체 검증 실패: " + path);
        }

        static BotResult Simulate(string kind, List<LandingProblem> pool, int games, uint seed)
        {
            int attempts = 0, correct = 0, earlyAttempts = 0, earlyCorrect = 0, lateAttempts = 0, lateCorrect = 0;
            int completed = 0, progress = 0;
            // 첫 시도율은 게임오버 여부와 분리해 200개 덱의 예정 문항을 모두 잰다.
            // 따라서 초기 400회, 후반 800회가 항상 확보되어 작은 조건부 표본의 우연 급등을 피한다.
            uint metricSeed = seed;
            if (kind != "noinput")
            {
                for (int game = 0; game < games; game++)
                {
                    var deck = ChamsaeRules.BuildDeck(pool, game + 1, true);
                    for (int round = 0; round < deck.Count; round++)
                    {
                        var p = deck[round]; int a, b;
                        Candidate(kind, p, ref metricSeed, game * ChamsaeRules.RoundCount + round, out a, out b);
                        bool ok = ChamsaeRules.IsCorrect(p, a, b);
                        attempts++; if (ok) correct++;
                        if (p.band == 1) { earlyAttempts++; if (ok) earlyCorrect++; }
                        else { lateAttempts++; if (ok) lateCorrect++; }
                    }
                }
            }
            uint playSeed = seed ^ 0xA5A5A5A5u;
            for (int game = 0; game < games; game++)
            {
                var deck = ChamsaeRules.BuildDeck(pool, game + 1, true);
                if (kind == "noinput") continue;
                int lives = ChamsaeRules.StartLives, firstCorrect = 0, dispersionCorrect = 0, attemptedRounds = 0, solved = 0;
                bool ended = false;
                for (int round = 0; round < deck.Count && !ended; round++)
                {
                    var p = deck[round]; bool solvedRound = false; bool first = true;
                    while (!solvedRound && lives > 0 && !ended)
                    {
                        int a, b;
                        Candidate(kind, p, ref playSeed, game * 97 + round * 13 + (ChamsaeRules.StartLives - lives), out a, out b);
                        bool ok = ChamsaeRules.IsCorrect(p, a, b);
                        if (first)
                        {
                            attemptedRounds++;
                            if (ok) { firstCorrect++; if (p.band >= 2) dispersionCorrect++; }
                        }
                        if (ok) { solvedRound = true; solved++; progress++; }
                        else
                        {
                            lives--;
                            if (first)
                            {
                                int remaining = ChamsaeRules.RoundCount - attemptedRounds;
                                int remainingDispersion = Math.Max(0, 4 - Math.Max(0, attemptedRounds - 2));
                                if (firstCorrect + remaining < ChamsaeRules.MasteryNeeded || dispersionCorrect + remainingDispersion < ChamsaeRules.DispersionMasteryNeeded) ended = true;
                            }
                        }
                        first = false;
                    }
                    if (lives <= 0) ended = true;
                }
                if (solved == ChamsaeRules.RoundCount && firstCorrect >= ChamsaeRules.MasteryNeeded && dispersionCorrect >= ChamsaeRules.DispersionMasteryNeeded) completed++;
            }
            float rate = attempts == 0 ? 0 : (float)correct / attempts;
            float low, high; Wilson(correct, attempts, out low, out high);
            return new BotResult
            {
                bot = kind, games = games, attempts = attempts, correct = correct, firstAttemptRate = rate,
                earlyAttempts = earlyAttempts, earlyCorrect = earlyCorrect, earlyRate = earlyAttempts == 0 ? 0 : (float)earlyCorrect / earlyAttempts,
                lateAttempts = lateAttempts, lateCorrect = lateCorrect, lateRate = lateAttempts == 0 ? 0 : (float)lateCorrect / lateAttempts,
                ci95Low = low, ci95High = high, completed = completed, completionRate = (float)completed / games, progress = progress
            };
        }

        static void Candidate(string kind, LandingProblem p, ref uint seed, int action, out int a, out int b)
        {
            int x, y;
            if (kind == "repeat") { x = 0; y = 0; }
            else if (kind == "cycle") { x = action % 9; y = (action + 1) % 9; }
            else
            {
                seed ^= seed << 13; seed ^= seed >> 17; seed ^= seed << 5; x = (int)(seed % 9u);
                seed ^= seed << 13; seed ^= seed >> 17; seed ^= seed << 5; y = (int)(seed % 9u);
            }
            a = p.shift + x; b = p.band == 1 ? -1 : p.shift + y;
        }

        static void Wilson(int success, int count, out float low, out float high)
        {
            if (count == 0) { low = high = 0; return; }
            const double z = 1.959963984540054;
            double n = count, p = (double)success / count, d = 1 + z * z / n;
            double center = (p + z * z / (2 * n)) / d;
            double half = z * Math.Sqrt(p * (1 - p) / n + z * z / (4 * n * n)) / d;
            low = (float)Math.Max(0, center - half); high = (float)Math.Min(1, center + half);
        }
    }
}
#endif
