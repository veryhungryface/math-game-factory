// 헤드리스 검산 + B3 무뇌 봇(게임과 같은 HanKkeutRules.BuildProblems/BuildDeck/IsCorrect/ClassifyMisconception 을 쓴다).
// 실행: bash factory/unity-src/han-kkeut-golf/ArtSource/validation/run-headless.sh
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Mgf.HanKkeutGolf;

static class HeadlessCheck
{
    static int fails;
    static void Expect(bool ok, string what) { if (!ok) { fails++; Console.WriteLine("FAIL " + what); } }

    static int Main(string[] args)
    {
        var bank = HanKkeutRules.BuildProblems();
        Expect(bank.Count == 540, "bank 540");
        Expect(bank.Select(p => p.prompt).Distinct().Count() == bank.Count, "prompts unique");
        foreach (var p in bank)
        {
            Expect(p.answer == (p.theta + 90) % 180, "k " + p.id);
            Expect(p.prompt.Contains(p.theta + "°") && p.prompt.Contains(p.radius + " cm"), "prompt values " + p.id);
            Expect(!p.prompt.Contains("√") && !p.prompt.Contains("의 자리에서"), "forbidden expr " + p.id);
            if (p.band == 3) Expect(p.prompt.Contains(p.phi + "°"), "phi in prompt " + p.id);
        }
        Expect(HanKkeutRules.Practice().answer == 120 && HanKkeutRules.LineDiff(HanKkeutRules.PracticeStart, 120) > 2, "practice 30->120, start 65");
        var t127 = new GolfProblem { theta = 127, answer = HanKkeutRules.Tangent(127) };
        Expect(t127.answer == 37, "127 -> 37");
        Expect(HanKkeutRules.Tangent(0) == 90 && HanKkeutRules.Tangent(90) == 0 && HanKkeutRules.Tangent(179) == 89, "boundaries 0/90/179");

        // 전수: 모든 θ × (φ 후보) × 제출각 0..179 — 통과 5개, 오개념 대표값 우연 참 0, 기하(첫 진입) 일치
        int passCountBad = 0, coincidence = 0, geomBad = 0, combos = 0, mirrorExcluded = 0;
        for (int theta = 0; theta < 180; theta++)
        {
            var phis = HanKkeutRules.PhiCandidates(theta);
            Expect(phis.Count > 0, "phi candidates " + theta);
            foreach (int phi in phis.Concat(new[] { -1 }))
            {
                var p = new GolfProblem { band = phi < 0 ? 1 : 3, theta = theta, phi = phi, answer = HanKkeutRules.Tangent(theta) };
                combos++;
                int passes = 0;
                for (int a = 0; a < 180; a++)
                {
                    bool ok = HanKkeutRules.IsCorrect(p, a);
                    if (ok) passes++;
                    string mis = HanKkeutRules.ClassifyMisconception(p, a);
                    if (ok && mis != "") coincidence++;
                    if (!ok && mis == "") coincidence++;
                }
                if (passes != 5) passCountBad++;
                // 오개념 대표값이 정답 허용범위 안에 들어가면 안 된다
                if (HanKkeutRules.IsCorrect(p, HanKkeutRules.ParallelValue(p))) coincidence++;
                int m = HanKkeutRules.MirrorValue(p); if (m < 0) mirrorExcluded++; else if (HanKkeutRules.IsCorrect(p, m)) coincidence++;
                int o = HanKkeutRules.OtherRadiusValue(p); if (o >= 0 && HanKkeutRules.IsCorrect(p, o)) coincidence++;
                if (phi >= 0 && HanKkeutRules.RayDiff(phi, theta) < 20) coincidence++;
            }
            // 기하: 레일(A 양쪽 2.25r)이 원 안으로 들어가는지 ↔ 정수 판정
            for (int r = 4; r <= 10; r++)
            {
                double R = 1.55 + .1 * (r - 4), L = 2.25 * R;
                double ax = R * Math.Cos(theta * Math.PI / 180), az = R * Math.Sin(theta * Math.PI / 180);
                for (int a = 0; a < 180; a++)
                {
                    double te = HanKkeutRules.FirstEntry(ax, az, 0, 0, R, a, L);
                    int diff = HanKkeutRules.LineDiff(a, HanKkeutRules.Tangent(theta));
                    double chord = 2 * R * Math.Sin(diff * Math.PI / 180);
                    if (diff >= 3 && te < 0) geomBad++;          // 접선이 아닌데 원을 안 가른다
                    if (diff == 0 && te >= 0 && chord > 1e-6) geomBad++;
                    if (diff >= 3 && chord > 2 * L) geomBad++;   // 레일이 현 전체를 덮지 못한다
                }
            }
        }
        Expect(passCountBad == 0, "5 passing directions per problem: bad=" + passCountBad);
        Expect(coincidence == 0, "misconception coincidence=" + coincidence);
        Expect(geomBad == 0, "geometry mismatch=" + geomBad);

        // 덱: θ 중복 없음, 시작각이 정답과 독립(허용범위 밖), 밴드 순서
        var rng = new Random(5);
        int[] kHist = new int[180]; int startNear = 0; double corrSum = 0; int decks = 4000;
        for (int s = 0; s < decks; s++)
        {
            var deck = HanKkeutRules.BuildDeck(1000 + s, out var starts);
            Expect(deck.Select(p => p.theta).Distinct().Count() == 8, "deck theta unique");
            for (int i = 0; i < 8; i++)
            {
                Expect(deck[i].band == HanKkeutRules.BandOf(i), "band order");
                kHist[deck[i].answer]++;
                if (HanKkeutRules.LineDiff(starts[i], deck[i].answer) <= 2) startNear++;
                corrSum += HanKkeutRules.LineDiff(starts[i], deck[i].answer);
            }
        }
        Expect(Math.Abs(startNear / (decks * 8.0) - 5 / 180.0) < .006, "start uniform/independent: pre-solved rate " + startNear / (decks * 8.0));
        double meanStartDiff = corrSum / (decks * 8.0);

        // ── B3 무뇌 봇: 실전 규칙(공 3, 서로 다른 코스 두 번째 첫 실패 종료, 수리 반복) ──
        var sb = new StringBuilder();
        sb.Append("{\n  \"checks\": {\"bank\": ").Append(bank.Count).Append(", \"combos\": ").Append(combos).Append(", \"coincidence\": ").Append(coincidence)
          .Append(", \"geometryMismatch\": ").Append(geomBad).Append(", \"mirrorExcludedCombos\": ").Append(mirrorExcluded)
          .Append(", \"meanStartAnswerDiff\": ").Append(meanStartDiff.ToString("0.00")).Append(", \"fails\": ").Append(fails).Append("},\n  \"bots\": [\n");
        var bots = new (string name, Func<int, int, int, Random, int> pick)[]
        {
            ("연타(항상 90°)", (course, start, tries, r) => 90),
            ("연타(최소 이동: 시작각 그대로 4px)", (course, start, tries, r) => start),
            ("순환(0→45→90→135°)", (course, start, tries, r) => new[] { 0, 45, 90, 135 }[(course + tries) % 4]),
            ("무작위(0~179°)", (course, start, tries, r) => r.Next(180)),
            ("시작각+90° 고정 회전", (course, start, tries, r) => (start + 90) % 180),
            ("눈금 훑기 후 아무 데서 놓기", (course, start, tries, r) => { int a = start; int steps = r.Next(30, 200); for (int i = 0; i < steps; i++) a = (a + 1) % 180; return a; }),
        };
        int runs = args.Length > 1 ? int.Parse(args[1]) : 200;
        foreach (var bot in bots)
        {
            var res = Sim(bot.pick, runs, 77);
            var big = Sim(bot.pick, 20000, 991);
            sb.Append("    {\"bot\": \"").Append(bot.name).Append("\", \"runs\": ").Append(runs)
              .Append(", \"firstAttempts\": ").Append(res.att).Append(", \"firstCorrect\": ").Append(res.cor)
              .Append(", \"firstRate\": ").Append((res.cor / (double)Math.Max(1, res.att)).ToString("0.0000"))
              .Append(", \"ci95\": ").Append((1.96 * Math.Sqrt(.0278 * .9722 / Math.Max(1, res.att))).ToString("0.0000"))
              .Append(", \"clearRate\": ").Append((res.clear / (double)runs).ToString("0.000"))
              .Append(", \"firstRate20000runs\": ").Append((big.cor / (double)Math.Max(1, big.att)).ToString("0.0000"))
              .Append(", \"chance\": 0.0278},\n");
            Console.WriteLine($"{bot.name}: first {res.cor}/{res.att} = {res.cor / (double)Math.Max(1, res.att):P2} (20000판 {big.cor / (double)big.att:P2}) · clear {res.clear}/{runs}");
        }
        sb.Append("    {\"bot\": \"무입력\", \"runs\": ").Append(runs).Append(", \"firstAttempts\": 0, \"firstCorrect\": 0, \"firstRate\": 0, \"clearRate\": 0, \"note\": \"제출·진도 0, 90초 뒤 time 종료\"},\n");
        var smart = Sim((c, s, t, r) => -1, runs, 3);
        var oneSlip = Sim((c, s, t, r) => c == 4 && t == 0 ? -2 : -1, runs, 3);
        sb.Append("    {\"bot\": \"정답 정책(θ+90°)\", \"clearRate\": ").Append((smart.clear / (double)runs).ToString("0.000")).Append("},\n");
        sb.Append("    {\"bot\": \"5코스 첫 실수 후 수리\", \"clearRate\": ").Append((oneSlip.clear / (double)runs).ToString("0.000")).Append("}\n  ]\n}\n");
        Console.WriteLine($"정답 정책 clear {smart.clear}/{runs} · 한 번 실수 후 수리 clear {oneSlip.clear}/{runs}");
        Console.WriteLine("fails=" + fails + " coincidence=" + coincidence + " geomBad=" + geomBad + " mirrorExcluded=" + mirrorExcluded);
        if (args.Length > 0) File.WriteAllText(args[0], sb.ToString());
        return fails == 0 ? 0 : 1;
    }

    // pick 반환: 0..179 각도, -1 = 정답(θ+90), -2 = 평행 오개념
    static (int att, int cor, int clear) Sim(Func<int, int, int, Random, int> pick, int runs, int seed)
    {
        var r = new Random(seed); int att = 0, cor = 0, clear = 0;
        for (int run = 0; run < runs; run++)
        {
            var deck = HanKkeutRules.BuildDeck(seed * 100003 + run, out var starts);
            int balls = HanKkeutRules.StartBalls, misses = 0, firstOk = 0, done = 0; bool over = false;
            for (int c = 0; c < 8 && !over; c++)
            {
                var p = deck[c];
                int start = starts[c];
                for (int tries = 0; ; tries++)
                {
                    if (tries > 400) { over = true; break; }
                    int a = pick(c, start, tries, r);
                    if (a == -1) a = p.answer; else if (a == -2) a = HanKkeutRules.ParallelValue(p);
                    bool ok = HanKkeutRules.IsCorrect(p, a);
                    if (tries == 0) { att++; if (ok) { cor++; firstOk++; } else misses++; }
                    if (ok) { done++; break; }
                    balls--;
                    if (balls <= 0 || misses > HanKkeutRules.MaxFirstMisses) { over = true; break; }
                    start = a; // 수리는 잘못 놓은 레일에서 다시 시작
                }
            }
            if (!over && done == 8 && firstOk >= HanKkeutRules.MasteryNeeded) clear++;
        }
        return (att, cor, clear);
    }
}
