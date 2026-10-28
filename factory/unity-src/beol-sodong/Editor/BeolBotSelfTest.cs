using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Mgf.BeolSodong
{
    public static class BeolBotSelfTest
    {
        enum Bot { Spam, Cycle, Random, Idle, SameValue }

        [Serializable]
        sealed class BotResult
        {
            public string policy;
            public int games;
            public int firstAttempts;
            public int firstCorrect;
            public double firstAttemptRate;
            public double theoreticalChance;
            public int completions;
            public double completionRate;
            public int progress;
        }

        [Serializable]
        sealed class Report
        {
            public bool passed;
            public int gamesPerBot = 200;
            public int bankCount;
            public int bankAuditErrors;
            public int subsetCollisions;
            public BotResult[] results;
        }

#if UNITY_EDITOR
        public static void Run()
        {
            string output = "beol-bot-results.json";
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == "-beolOut") output = args[i + 1];
            Report report = RunAll();
            System.IO.File.WriteAllText(output, JsonUtility.ToJson(report, true) + "\n");
            Debug.Log("BEOL_BOT_RESULTS " + JsonUtility.ToJson(report));
            EditorApplication.Exit(report.passed ? 0 : 1);
        }
#endif

        static Report RunAll()
        {
            List<SwarmProblem> bank = BeolRules.BuildBank();
            var auditLog = new System.Text.StringBuilder();
            int audit = BeolRules.AuditBank(bank, auditLog);
            int collisions = 0;
            for (int i = 0; i < bank.Count; i++)
            {
                int accepted = 0;
                for (int mask = 0; mask < 512; mask++) if (BeolRules.IsExact(bank[i], mask)) accepted++;
                if (accepted != 1) collisions++;
            }
            BotResult[] results = {
                RunBot(Bot.Spam, bank, 200, 2026102801),
                RunBot(Bot.Cycle, bank, 200, 2026102802),
                RunBot(Bot.Random, bank, 200, 2026102803),
                RunBot(Bot.Idle, bank, 200, 2026102804),
                RunBot(Bot.SameValue, bank, 200, 2026102805)
            };
            bool pass = audit == 0 && collisions == 0;
            for (int i = 0; i < 4; i++)
            {
                if (results[i].policy == "no-input") pass &= results[i].firstAttempts == 0 && results[i].progress == 0 && results[i].completions == 0;
                else pass &= results[i].firstAttemptRate <= results[i].theoreticalChance;
            }
            return new Report { passed = pass, bankCount = bank.Count, bankAuditErrors = audit, subsetCollisions = collisions, results = results };
        }

        static BotResult RunBot(Bot bot, List<SwarmProblem> bank, int games, int seed)
        {
            var rnd = new System.Random(seed);
            int firstAttempts = 0, firstCorrect = 0, completed = 0, progress = 0, serial = 0;
            double chanceSum = 0;
            for (int game = 0; game < games; game++)
            {
                if (bot == Bot.Idle) continue;
                List<SwarmProblem> deck = BeolRules.RunDeck(bank, seed + game * 104729);
                int lives = BeolRules.StartLives, solved = 0, firstGood = 0;
                for (int q = 0; q < deck.Count && lives > 0; q++)
                {
                    SwarmProblem p = deck[q]; bool first = true;
                    while (lives > 0)
                    {
                        int guess = Guess(bot, p, rnd, serial++);
                        bool ok = BeolRules.IsExact(p, guess);
                        if (first)
                        {
                            firstAttempts++; chanceSum += Chance(bot, p, guess);
                            if (ok) { firstCorrect++; firstGood++; }
                            first = false;
                        }
                        if (ok) { solved++; progress++; break; }
                        lives--;
                    }
                }
                if (solved == BeolRules.Goal && firstGood >= BeolRules.MasteryFirst) completed++;
            }
            return new BotResult
            {
                policy = bot == Bot.Spam ? "spam-fixed" : bot == Bot.Cycle ? "cycle-1-2-3-4" : bot == Bot.Random ? "random-mask" : bot == Bot.Idle ? "no-input" : "same-value-follower",
                games = games,
                firstAttempts = firstAttempts,
                firstCorrect = firstCorrect,
                firstAttemptRate = firstAttempts == 0 ? 0 : (double)firstCorrect / firstAttempts,
                theoreticalChance = firstAttempts == 0 ? 0 : chanceSum / firstAttempts,
                completions = completed,
                completionRate = (double)completed / games,
                progress = progress
            };
        }

        static int Guess(Bot bot, SwarmProblem p, System.Random rnd, int serial)
        {
            if (bot == Bot.Spam) return 201; // 같은 네 자리만 반복: 1·4·7·8
            if (bot == Bot.Cycle)
            {
                int[] masks = { 15, 73, 438, 511 }; // 4개→3개→6개→전체 쓸기
                return masks[serial & 3];
            }
            if (bot == Bot.Random) return rnd.Next(512);
            if (bot == Bot.SameValue)
            {
                var distinct = new List<int>(5);
                for (int i = 0; i < p.values.Length; i++) if (!distinct.Contains(p.values[i])) distinct.Add(p.values[i]);
                return BeolRules.MaskForValue(p, distinct[rnd.Next(distinct.Count)]);
            }
            return 0;
        }

        static double Chance(Bot bot, SwarmProblem p, int guess)
        {
            if (bot == Bot.Random) return 1.0 / 512.0;
            if (bot == Bot.SameValue) return p.band == 3 ? 0 : 1.0 / 5.0;
            int k = BeolRules.BitCount(guess), target = BeolRules.BitCount(p.expectedMask);
            return k == target ? 1.0 / Choose(9, target) : 0;
        }

        static int Choose(int n, int k)
        {
            if (k < 0 || k > n) return 0; k = Math.Min(k, n - k); int r = 1;
            for (int i = 1; i <= k; i++) r = r * (n - k + i) / i;
            return r;
        }
    }
}
