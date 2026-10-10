// 딱 맞는 방울 — 무뇌 봇 자가 테스트(B3). 게임과 같은 BangulRules(생성기·정수 판정)를 그대로 쓴다.
// 빌드된 게임을 ?selftest=1 로 열면 부팅 직후 실행되어 결과 JSON 을 콘솔에 "BANGUL_SELFTEST " 접두사로 찍는다
// (ArtSource/validation/bot-selftest.mjs 가 받아 저장한다). 일반 플레이에는 실행되지 않는다.
//
// 봇은 「판 위의 한 점에 씨앗을 놓는다」만 한다(접기 띠·거리 막대를 쓰지 않는다).
// 시간 모델: 한 번 놓기 = 1.6초(끌기+부풀기), 거절(안쪽 라운드에서 삼각형 밖) = 0.8초, 정답·오답 연출 중에는 시계 정지.
// 우연 수준 = 라운드마다 「정답 영역 넓이 ÷ 조작 가능 영역 넓이」(안쪽 = 삼각형 내부, 바깥 = 판 전체)의 평균.
using System;
using System.Collections.Generic;
using System.Text;

namespace Mgf.TtakMatneunBangul
{
    public static class BangulBots
    {
        delegate bool Policy(Board b, int attempt, Random rng, out IP p);

        sealed class Stat
        {
            public string name;
            public int games, firstTotal, firstOk, attempts, completions, masteries, rescues, timeouts, heartOuts;
        }

        static readonly IP[] CycleSpots = { new IP(-84, 56), new IP(84, 56), new IP(84, -56), new IP(-84, -56) };

        public static string Run(int games, int seed)
        {
            var policies = new List<KeyValuePair<string, Policy>>
            {
                // 1) 연타: 항상 같은 자리(판 정중앙 = 화면 중앙 탭)
                new KeyValuePair<string, Policy>("mash_center", (Board b, int a, Random r, out IP p) => { p = new IP(0, 0); return true; }),
                // 1b) 연타: 항상 같은 자리(판 왼쪽 위 1/4 지점)
                new KeyValuePair<string, Policy>("mash_corner", (Board b, int a, Random r, out IP p) => { p = new IP(-84, 70); return true; }),
                // 2) 순환: 네 자리를 1→2→3→4 로 돌아가며
                new KeyValuePair<string, Policy>("cycle4", (Board b, int a, Random r, out IP p) => { p = CycleSpots[a % 4]; return true; }),
                // 3) 무작위: 판 위 아무 데나
                new KeyValuePair<string, Policy>("random", (Board b, int a, Random r, out IP p) => { p = new IP(r.Next(-BangulRules.HX, BangulRules.HX + 1), r.Next(-BangulRules.HY, BangulRules.HY + 1)); return true; }),
                // 4) 무입력
                new KeyValuePair<string, Policy>("idle", (Board b, int a, Random r, out IP p) => { p = default(IP); return false; }),
                // 오개념 봇(참고): 무게중심에 놓기 · 반대 중심(안쪽이면 O, 바깥이면 I)에 놓기
                new KeyValuePair<string, Policy>("misc_centroid", (Board b, int a, Random r, out IP p) => { p = new IP((int)Math.Round(b.Gx), (int)Math.Round(b.Gy)); return true; }),
                new KeyValuePair<string, Policy>("misc_swap_IO", (Board b, int a, Random r, out IP p) =>
                {
                    p = b.kind == Kind.In ? new IP((int)Math.Round(b.Ox), (int)Math.Round(b.Oy)) : new IP((int)Math.Round(b.Ix), (int)Math.Round(b.Iy));
                    if (!BangulRules.OnBoard(p)) p = new IP((int)Math.Round(b.Gx), (int)Math.Round(b.Gy));
                    return true;
                }),
                // 대조군: 정답 중심을 아는 학생(게임이 풀 수 있는지)
                new KeyValuePair<string, Policy>("oracle", (Board b, int a, Random r, out IP p) => { p = b.hit; return true; }),
            };

            var sb = new StringBuilder();
            sb.Append("{\"games\":").Append(games).Append(",\"tolIn\":").Append(BangulRules.TolIn).Append(",\"tolOut\":").Append(BangulRules.TolOut);

            // 우연 수준: 실제 런 덱(봇과 같은 생성기)에서 라운드별 넓이 비
            var crng = new Random(seed ^ 0x5eed);
            int chanceDecks = Math.Max(10, games / 4);
            var perRound = new double[BangulRules.Rounds]; var perRoundMax = new double[BangulRules.Rounds];
            double sumIn = 0, sumOut = 0; int nIn = 0, nOut = 0; double maxAll = 0;
            double sumRegionIn = 0, sumRegionOut = 0;
            for (int d = 0; d < chanceDecks; d++)
            {
                var deck = BangulRules.RunDeck(crng);
                for (int i = 0; i < deck.Count; i++)
                {
                    var b = deck[i];
                    double f = BangulRules.ChanceFraction(b, 4);
                    perRound[i] += f; perRoundMax[i] = Math.Max(perRoundMax[i], f); maxAll = Math.Max(maxAll, f);
                    // 정답 영역의 등가 반지름(월드 단위) — 사람이 노릴 수 있는 크기
                    double opArea = b.kind == Kind.In ? Math.Abs(BangulRules.Cross(b.V[0], b.V[1], b.V[2])) / 2.0 : (2.0 * BangulRules.HX) * (2.0 * BangulRules.HY);
                    double region = Math.Sqrt(f * opArea / Math.PI) / BangulRules.Sub;
                    if (b.kind == Kind.In) { sumIn += f; nIn++; sumRegionIn += region; } else { sumOut += f; nOut++; sumRegionOut += region; }
                }
            }
            double chanceMean = (sumIn + sumOut) / Math.Max(1, nIn + nOut);
            sb.Append(",\"chance\":{\"mean\":").Append(F(chanceMean)).Append(",\"in\":").Append(F(sumIn / Math.Max(1, nIn)))
              .Append(",\"out\":").Append(F(sumOut / Math.Max(1, nOut))).Append(",\"max\":").Append(F(maxAll))
              .Append(",\"regionRadiusWorldIn\":").Append(F(sumRegionIn / Math.Max(1, nIn))).Append(",\"regionRadiusWorldOut\":").Append(F(sumRegionOut / Math.Max(1, nOut)))
              .Append(",\"perRound\":[");
            for (int i = 0; i < perRound.Length; i++) { if (i > 0) sb.Append(','); sb.Append(F(perRound[i] / chanceDecks)); }
            sb.Append("],\"perRoundMax\":[");
            for (int i = 0; i < perRound.Length; i++) { if (i > 0) sb.Append(','); sb.Append(F(perRoundMax[i])); }
            sb.Append("],\"decks\":").Append(chanceDecks).Append('}');

            // 연습 판도 순진한 점이 떨어지는지
            var pr = BangulRules.Practice();
            sb.Append(",\"practiceValid\":").Append(BangulRules.Validate(pr) ? "true" : "false");
            sb.Append(",\"practiceChance\":").Append(F(BangulRules.ChanceFraction(pr, 2)));

            sb.Append(",\"bots\":[");
            for (int k = 0; k < policies.Count; k++)
            {
                var st = Simulate(policies[k].Key, policies[k].Value, games, seed + k * 7919);
                if (k > 0) sb.Append(',');
                double rate = st.firstTotal == 0 ? 0 : (double)st.firstOk / st.firstTotal;
                sb.Append("{\"bot\":\"").Append(st.name).Append("\",\"games\":").Append(st.games)
                  .Append(",\"firstTryRate\":").Append(F(rate)).Append(",\"firstOk\":").Append(st.firstOk).Append(",\"firstTotal\":").Append(st.firstTotal)
                  .Append(",\"attempts\":").Append(st.attempts).Append(",\"rescues\":").Append(st.rescues)
                  .Append(",\"completionRate\":").Append(F((double)st.completions / games))
                  .Append(",\"masteryRate\":").Append(F((double)st.masteries / games))
                  .Append(",\"timeouts\":").Append(st.timeouts).Append(",\"heartOuts\":").Append(st.heartOuts).Append('}');
            }
            sb.Append("]");
            var bank = BangulRules.BuildBank();
            sb.Append(",\"bank\":").Append(BangulRules.SelfCheckBank(bank));
            sb.Append('}');
            return sb.ToString();
        }

        static string F(double v) => v.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture);

        static Stat Simulate(string name, Policy policy, int games, int seed)
        {
            var st = new Stat { name = name, games = games };
            var rng = new Random(seed);
            for (int g = 0; g < games; g++)
            {
                var deck = BangulRules.RunDeck(rng);
                float time = BangulRules.RunSeconds;
                int hearts = BangulRules.StartHearts, firstOkGame = 0, done = 0;
                bool lost = false;
                int attemptIdx = 0;
                for (int ri = 0; ri < deck.Count && !lost; ri++)
                {
                    var b = deck[ri];
                    bool first = true;
                    while (true)
                    {
                        if (time <= 0) { lost = true; st.timeouts++; break; }
                        IP p;
                        if (!policy(b, attemptIdx++, rng, out p)) { time = 0; continue; }
                        var v = BangulRules.Judge(b, p);
                        if (v.refused) { time -= 0.8f; continue; }
                        time -= 1.6f;
                        st.attempts++;
                        if (first) { st.firstTotal++; if (v.ok) { st.firstOk++; firstOkGame++; } }
                        if (v.ok) { st.rescues++; done++; break; }
                        hearts--;
                        if (hearts <= 0) { lost = true; st.heartOuts++; break; }
                        if (!first) { done++; break; }  // 수리 실패 → 구조 못 하고 다음 라운드
                        first = false;
                    }
                }
                if (!lost && done == deck.Count) st.completions++;
                if (!lost && done == deck.Count && firstOkGame >= BangulRules.MasteryFirst) st.masteries++;
            }
            return st;
        }
    }
}
