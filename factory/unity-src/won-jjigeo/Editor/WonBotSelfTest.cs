// 원 찍어 — B3 무뇌 봇 자가 테스트(에디터 배치 전용, 빌드에 들어가지 않는다).
// 게임과 똑같은 Gen(풀)·Layout.Place(배치)·Geo.Hit(정수 판정)을 써서 봇 4종 × 200판을 돌린다.
//   Unity -batchmode -projectPath <ws> -executeMethod Mgf.WonJjigeo.WonBotSelfTest.Run -wonOut <json> -quit
// 판 진행 모델: 문항당 생각 시간 봇 1.2초(무입력은 0장), 오답 3회면 끝, 90초면 끝. 밴드는 경과 시간으로(25초·55초).
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;

namespace Mgf.WonJjigeo
{
    public static class WonBotSelfTest
    {
        delegate bool Bot(Placed pl, int step, Random r, out V2 p);

        static bool Mash(Placed pl, int step, Random r, out V2 p) { p = new V2(0.3, 0.2); return true; }          // 항상 같은 점
        static bool MashCenter(Placed pl, int step, Random r, out V2 p) { p = new V2(0, 0); return true; }       // 베드 한가운데
        static readonly V2[] cyc = { new V2(-2, 1.5), new V2(2, 1.5), new V2(2, -1.5), new V2(-2, -1.5) };
        static bool Cycle(Placed pl, int step, Random r, out V2 p) { p = cyc[step % 4]; return true; }
        static bool Rand(Placed pl, int step, Random r, out V2 p)
        {
            p = new V2(-Layout.BedHX + r.NextDouble() * (Layout.UsableMaxX(0) + Layout.BedHX), -Layout.BedHZ + r.NextDouble() * 2 * Layout.BedHZ);
            return true;
        }
        static bool Idle(Placed pl, int step, Random r, out V2 p) { p = default; return false; }
        // 참고용 "얕은 전략" 봇 — 화면 정보만으로 수학을 우회하려는 시도
        static bool Centroid(Placed pl, int step, Random r, out V2 p) { p = pl.G; return true; }                 // 삼각형 한가운데
        static bool BoxMid(Placed pl, int step, Random r, out V2 p) { p = Geo.BoxCenter(pl.P); return true; }   // 눈대중 한가운데
        static bool InsideRand(Placed pl, int step, Random r, out V2 p)                                          // 삼각형 안 아무 데나
        {
            double a = r.NextDouble(), b = r.NextDouble(); if (a + b > 1) { a = 1 - a; b = 1 - b; }
            p = pl.P[0] + (pl.P[1] - pl.P[0]) * a + (pl.P[2] - pl.P[0]) * b; return true;
        }
        static bool Swap(Placed pl, int step, Random r, out V2 p) { p = pl.other; return true; }                 // 외심↔내심 혼동
        static bool Perfect(Placed pl, int step, Random r, out V2 p) { p = pl.ans; return true; }               // 정상 경로 확인용
        static bool OneSlip(Placed pl, int step, Random r, out V2 p) { p = step == 0 ? pl.G : pl.ans; return true; } // 첫 실수 뒤 회복 경로

        class Res { public string name; public int games, plants, firstOk, finished, plates; public double chanceSum; public int chanceN; }

        public static void Run()
        {
            string outPath = "won-bots.json";
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == "-wonOut") outPath = a[i + 1];
            var bots = new List<(string, Bot)>
            {
                ("연타(고정점)", Mash), ("연타(베드 한가운데)", MashCenter), ("순환(4점)", Cycle), ("무작위", Rand), ("무입력", Idle),
                ("삼각형 무게중심", Centroid), ("상자 한가운데", BoxMid), ("삼각형 안 무작위", InsideRand), ("외심↔내심 혼동", Swap),
                ("정답(상한 확인)", Perfect), ("첫 실수 뒤 정답", OneSlip)
            };
            var sb = new StringBuilder("{\"bots\":[");
            var gen = new Gen(99);
            // 풀 크기와 계약 단언: 모든 항목이 함정 거리 조건을 만족
            int bad = 0;
            foreach (var pool in new[] { gen.isoO, gen.scO, gen.scI, gen.rightO, gen.obtO, gen.wideI })
                foreach (var e in pool)
                    foreach (var rot in e.rots)
                        if (!Geo.PassesTraps(new PlateSpec { A = e.A, B = e.B, C = e.C, kind = e.kind, incenter = e.incenter, rot = rot })) bad++;
            bool first = true;
            foreach (var (name, bot) in bots)
            {
                var res = new Res { name = name };
                var rng = new Random(name.GetHashCode());
                var g = new Gen(1234);
                for (int game = 0; game < 200; game++)
                {
                    res.games++;
                    double t = 0; int lives = 3, scraps = 0, idx = 0, inBand = 0, lastBand = 1;
                    while (t < 90 && lives > 0)
                    {
                        int band = t < 25 ? 1 : t < 55 ? 2 : 3;
                        if (band != lastBand) { lastBand = band; inBand = 0; }
                        var spec = idx == 0 ? Gen.First() : g.Make(band, inBand);
                        inBand++;
                        var pl = Layout.Place(spec, scraps, rng);
                        // 우연 수준: 허용원 넓이 ÷ 핀을 놓을 수 있는 베드 넓이
                        double area = (Layout.UsableMaxX(scraps) + Layout.BedHX) * 2 * Layout.BedHZ;
                        res.chanceSum += Math.PI * pl.tol * pl.tol / area; res.chanceN++;
                        if (!bot(pl, idx, rng, out var p)) { t = 90; break; }   // 무입력: 온보딩 대기에서 시간이 멈춰 진도 0
                        idx++;
                        res.plants++;
                        if (Geo.Hit(p, pl.ans, pl.tol)) { res.firstOk++; res.plates++; }
                        else { lives--; scraps = Math.Min(2, scraps + 1); }
                        t += 1.2;
                    }
                    if (lives > 0 && t >= 90 && res.plants > 0) res.finished++;
                }
                // 판 수명과 무관한 대표본: 밴드 1~3 · 스크랩 0~2 명판 6000장에 한 번씩 박기
                int bigN = 0, bigOk = 0;
                var g2 = new Gen(555);
                for (int k = 0; k < 6000; k++)
                {
                    var spec = g2.Make(1 + k % 3, k);
                    var pl = Layout.Place(spec, k % 3, rng);
                    if (!bot(pl, k, rng, out var p)) continue;
                    bigN++;
                    if (Geo.Hit(p, pl.ans, pl.tol)) bigOk++;
                }
                double rate = res.plants > 0 ? (double)res.firstOk / res.plants : 0;
                double chance = res.chanceN > 0 ? res.chanceSum / res.chanceN : 0;
                if (!first) sb.Append(','); first = false;
                sb.Append("{\"bot\":\"").Append(res.name).Append("\",\"games\":").Append(res.games)
                  .Append(",\"plants\":").Append(res.plants).Append(",\"firstTryRate\":").Append(rate.ToString("0.0000"))
                  .Append(",\"chance\":").Append(chance.ToString("0.0000")).Append(",\"completion\":").Append(((double)res.finished / res.games).ToString("0.000"))
                  .Append(",\"platesPerGame\":").Append(((double)res.plates / res.games).ToString("0.00"))
                  .Append(",\"perPlate6000\":").Append(bigN > 0 ? ((double)bigOk / bigN).ToString("0.0000") : "0").Append('}');
            }
            sb.Append("],\"pool\":{\"isoO\":").Append(gen.isoO.Count).Append(",\"scO\":").Append(gen.scO.Count).Append(",\"scI\":").Append(gen.scI.Count)
              .Append(",\"rightO\":").Append(gen.rightO.Count).Append(",\"obtO\":").Append(gen.obtO.Count).Append(",\"wideI\":").Append(gen.wideI.Count)
              .Append(",\"trapViolations\":").Append(bad).Append("}}");
            File.WriteAllText(outPath, sb.ToString());
            UnityEngine.Debug.Log("WON_BOTS " + sb);
            EditorApplication.Exit(0);
        }
    }
}
