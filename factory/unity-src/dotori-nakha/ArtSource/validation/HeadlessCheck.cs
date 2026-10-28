// 헤드리스 검산 + B3 무뇌 봇(게임과 같은 DotoriRules.BuildProblems/BuildDeck/IsCorrect 를 그대로 쓴다).
// 실행: bash factory/unity-src/dotori-nakha/ArtSource/validation/run-headless.sh
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Mgf.DotoriNakha;

static class HeadlessCheck
{
    static int Main(string[] args)
    {
        var sb = new StringBuilder();
        var pool = DotoriRules.BuildProblems(); // Validate() 가 내부에서 전수 검사(중앙값 재계산·유일해·버킷 균등·발문)를 한다
        int halfAnswers = pool.Count(p => p.answer2 % 2 == 1);
        int dupData = pool.Count(p => p.raw.Distinct().Count() < p.raw.Length);
        int meanDiffers = pool.Count(p => p.answer2 * p.raw.Length != 2 * p.raw.Sum());
        int rawMidDiffers = pool.Count(p => DotoriRules.ClassifyMisconception(p, RawMiddle2(p)) != "" );
        int midsSame = pool.Count(p => p.raw.Length % 2 == 0 && Sorted(p)[p.raw.Length / 2 - 1] == Sorted(p)[p.raw.Length / 2]);
        int endpoints = pool.Count(p => p.raw.Contains(1) || p.raw.Contains(12));
        // 오답 진단 가설이 정답과 우연히 같으면 그 가설로 진단하지 않는다(Classify 는 정답을 먼저 거른다) — 전수 확인
        int coincidence = 0;
        foreach (var p in pool)
            for (int v = 2; v <= 24; v++)
                if (v == p.answer2 && DotoriRules.ClassifyMisconception(p, v) != "") coincidence++;
        // 연습 고정값 검산
        bool practiceOk = DotoriRules.RecalculateAnswer2(DotoriRules.PracticeOdd().raw) == 10 && DotoriRules.RecalculateAnswer2(DotoriRules.PracticeEven().raw) == 11;

        // 덱 분포: 실전 정답은 단계별로 균등해야 한다
        var rng = new Random(20261028);
        var oddHist = new int[25]; var evenHist = new int[25];
        int decks = 4000;
        for (int d = 0; d < decks; d++)
        {
            var deck = DotoriRules.BuildDeck(pool, rng.Next(1, int.MaxValue / 8000));
            foreach (var p in deck) (p.raw.Length == 5 ? oddHist : evenHist)[p.answer2]++;
        }
        double oddChi = Chi(oddHist, Enumerable.Range(1, 12).Select(x => x * 2).ToArray());
        double evenChi = Chi(evenHist, Enumerable.Range(2, 23).ToArray());

        sb.Append("{\n");
        sb.Append($"  \"bank\": {{\"count\": {pool.Count}, \"halfGramAnswers\": {halfAnswers}, \"withDuplicateValues\": {dupData}, \"meanDiffersFromMedian\": {meanDiffers}, \"evenMiddleTwoEqual\": {midsSame}, \"containsEndpoint1or12\": {endpoints}, \"rawMiddleIsWrong\": {rawMidDiffers}, \"diagnosisOnCorrectAnswer\": {coincidence}, \"practiceFixedOk\": {(practiceOk ? "true" : "false")}}},\n");
        sb.Append($"  \"deckUniformity\": {{\"decks\": {decks}, \"oddChiSq_df11\": {oddChi:F2}, \"evenChiSq_df22\": {evenChi:F2}, \"note\": \"df11 99%=24.7, df22 99%=40.3\"}},\n");

        // B3 무뇌 봇: 실전 규칙(걸쇠 3, 첫 제출 실패 3이면 종료, 오답이면 같은 무리 재조작) 그대로
        const double chance = (2.0 / 12 + 6.0 / 23) / 8;
        sb.Append($"  \"chanceUpperBound\": {chance:F4},\n  \"bots\": {{\n");
        var bots = new (string name, Func<Random, int, int, int> pick)[]
        {
            ("mash_fixed_all_positions", null),
            ("cycle_1_to_12", (r, k, prev) => 2 + (k % 23)),
            ("random_drag", (r, k, prev) => r.Next(2, 25)),
            ("random_integer_only", (r, k, prev) => 2 * r.Next(1, 13)),
            ("repeat_previous_answer", (r, k, prev) => prev > 0 ? prev : 12),
        };
        var lines = new List<string>();
        foreach (var bot in bots)
        {
            if (bot.pick == null)
            {
                // 23개 고정 눈금을 각각 200판씩(총 4600판) 돌려 합산한다. 최댓값 하나만 고르면 23중 선택 편향이 생긴다.
                var pooled = new Stat(); double bestRate = 0; int bestV = 0;
                for (int v = 2; v <= 24; v++)
                {
                    int vv = v;
                    var s1 = RunBot(pool, 200, 777 + v, (r, k, prev) => vv);
                    pooled.Add(s1);
                    if (s1.Rate > bestRate) { bestRate = s1.Rate; bestV = v; }
                }
                lines.Add($"    \"{bot.name}\": {pooled.Json($"23개 고정 눈금 x 200판 합산; 가장 높았던 눈금 {bestV / 2.0} g = {bestRate:F4}(23중 최댓값이라 선택 편향 포함)")}");
                continue;
            }
            var st = RunBot(pool, 200, 1000 + bot.name.Length * 31 + bot.name[0], bot.pick);
            lines.Add($"    \"{bot.name}\": {st.Json("")}");
        }
        lines.Add("    \"no_input\": {\"runs\": 200, \"submissions\": 0, \"firstAttempts\": 0, \"firstCorrect\": 0, \"rate\": 0, \"cleared\": 0, \"note\": \"제출 0 · 진도 0 · 120초 뒤 시간 종료 실패(코드 경로: Update 의 timeLeft<=0 → EndRun(time))\"}");
        sb.Append(string.Join(",\n", lines)).Append("\n  }\n}\n");
        File.WriteAllText(args.Length > 0 ? args[0] : "headless-results.json", sb.ToString());
        Console.Write(sb.ToString());
        return 0;
    }

    sealed class Stat
    {
        public int runs, submissions, firstAttempts, firstCorrect, cleared;
        public int[] bandAttempts = new int[4], bandCorrect = new int[4];
        public double Rate => firstAttempts == 0 ? 0 : firstCorrect / (double)firstAttempts;
        // 이 봇이 실제로 시도한 단계 구성에 맞춘 우연 상한: 1단계(홀수 5개) 1/12, 2·3단계 1/23
        public double Chance => firstAttempts == 0 ? 0 : (bandAttempts[1] / 12.0 + (bandAttempts[2] + bandAttempts[3]) / 23.0) / firstAttempts;
        public void Add(Stat o)
        {
            runs += o.runs; submissions += o.submissions; firstAttempts += o.firstAttempts; firstCorrect += o.firstCorrect; cleared += o.cleared;
            for (int i = 0; i < 4; i++) { bandAttempts[i] += o.bandAttempts[i]; bandCorrect[i] += o.bandCorrect[i]; }
        }
        public string Json(string note)
        {
            double p = Rate, n = Math.Max(1, firstAttempts), z = 1.96;
            double centre = (p + z * z / (2 * n)) / (1 + z * z / n), half = z * Math.Sqrt(p * (1 - p) / n + z * z / (4 * n * n)) / (1 + z * z / n);
            string bands = string.Join(", ", Enumerable.Range(1, 3).Select(b => $"\"b{b}\": \"{bandCorrect[b]}/{bandAttempts[b]}\""));
            return $"{{\"runs\": {runs}, \"submissions\": {submissions}, \"firstAttempts\": {firstAttempts}, \"firstCorrect\": {firstCorrect}, \"rate\": {p:F4}, \"chanceForThisMix\": {Chance:F4}, \"wilson95\": [{centre - half:F4}, {centre + half:F4}], \"cleared\": {cleared}, {bands}{(note.Length > 0 ? ", \"note\": \"" + note + "\"" : "")}}}";
        }
    }

    static Stat RunBot(List<DotoriProblem> pool, int runs, int seed, Func<Random, int, int, int> pick)
    {
        var r = new Random(seed); var s = new Stat();
        int k = 0;
        for (int run = 0; run < runs; run++)
        {
            s.runs++;
            var deck = DotoriRules.BuildDeck(pool, r.Next(1, int.MaxValue / 8000));
            int lives = DotoriRules.StartLives, solved = 0, firstFails = 0, prevAnswer = 0;
            double time = DotoriRules.SessionSeconds;
            bool over = false;
            for (int round = 0; round < 8 && !over; round++)
            {
                var p = deck[round]; bool first = true;
                while (true)
                {
                    int v = pick(r, k++, prevAnswer); s.submissions++; time -= 1.6;
                    bool ok = DotoriRules.IsCorrect(p, v);
                    if (first)
                    {
                        s.firstAttempts++; s.bandAttempts[p.band]++;
                        if (ok) { s.firstCorrect++; s.bandCorrect[p.band]++; } else firstFails++;
                    }
                    if (ok) { solved++; prevAnswer = p.answer2; break; }
                    lives--; time -= 2; first = false;
                    if (lives <= 0 || firstFails >= 3 || time <= 0) { over = true; break; }
                }
            }
            if (!over && solved == 8 && firstFails <= 2 && lives > 0 && time > 0) s.cleared++;
        }
        return s;
    }

    static int[] Sorted(DotoriProblem p) { var a = (int[])p.raw.Clone(); Array.Sort(a); return a; }
    static int RawMiddle2(DotoriProblem p) { int n = p.raw.Length; return n % 2 == 1 ? 2 * p.raw[n / 2] : p.raw[n / 2 - 1] + p.raw[n / 2]; }

    static double Chi(int[] hist, int[] cells)
    {
        double total = cells.Sum(c => hist[c]), e = total / cells.Length, chi = 0;
        foreach (int c in cells) chi += (hist[c] - e) * (hist[c] - e) / e;
        return chi;
    }
}
