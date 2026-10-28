// 양말 대탈출 — 중3 원의 접선과 삼각형의 내접원 정수 문제 은행.
// curriculum/2022-middle-math.json style_guide / expression_traps:
// - 값 발문은 중학교 지시형 「~의 길이를 구하시오」, 오류 수리는 「바르게 고치시오」로 쓴다.
// - 수와 단위는 띄어 쓴다(6 cm). 선분은 「선분 PA」처럼 명시한다.
// - 원과 비례는 다루지 않고, 접점의 직각과 같은 점에서 그은 두 접선의 길이만 쓴다.
// - 증명에서 금지된 「가정」「결론」을 쓰지 않는다.
// 오개념 역산(misconceptionId): center_distance, whole_side, different_vertex, stopped_midway.
using System;
using System.Collections.Generic;
using Mgf;

namespace Mgf.YangmalDaetalchul
{
    [Serializable]
    public sealed class SockProblem
    {
        public string id;
        public int band;
        public int answerCm;
        public int x, y, z, po;
        public int variant;
        public string prompt;
        public string evidence;
        public string unitConcept;
        public int wrongCenter, wrongWhole, wrongVertex, wrongMid;
    }

    public static class SockRules
    {
        public const int MinLength = 1;
        public const int MaxLength = 24;
        public const int Rounds = 9;
        public const int Lives = 3;
        public const int Mastery = 7;
        public const float RunSeconds = 90f;
        static List<SockProblem> cache;

        public static SockProblem Practice() => Make(1, 6, 2, "practice-pa6");

        // 답 q를 먼저 고정하고 나머지 치수를 구조적으로 붙인다. 사후 정답 필터링은 없다.
        public static SockProblem Make(int band, int q, int variant, string id)
        {
            if (q < 1 || q > 24) throw new ArgumentOutOfRangeException(nameof(q));
            if (variant < 1 || variant > 5) throw new ArgumentOutOfRangeException(nameof(variant));
            int y = 2 + ((q + variant * 3) % 11);                 // 2..12
            int z = 1 + ((q * 7 + variant * 5) % 24);            // 1..24
            if (z == q) z = z == 24 ? 23 : z + 1;                // 3단계 오개념은 항상 거짓
            var p = new SockProblem { id = id, band = band, answerCm = q, x = q, y = y, z = z, variant = variant };
            if (band == 1)
            {
                p.po = q + 2 + variant;
                p.prompt = "점 P에서 원 O에 그은 두 접선의 접점을 A, B라 하자. 선분 PA의 길이가 " + q + " cm, 선분 PO의 길이가 " + p.po + " cm일 때, 선분 PB의 길이를 구하시오.";
                p.evidence = "같은 점 P에서 그은 두 접선의 길이는 같다 · PA=PB=" + q + " cm";
                p.unitConcept = "원 밖의 한 점에서 그은 두 접선";
                p.wrongCenter = p.po;
            }
            else if (band == 2)
            {
                int ab = q + y;
                p.prompt = "△ABC의 내접원이 선분 AB, BC, CA와 각각 D, E, F에서 접한다. AB=" + ab + " cm, BD=" + y + " cm일 때, 선분 AF의 길이를 구하시오.";
                p.evidence = "AD=AB−BD=" + ab + "−" + y + "=" + q + ", 같은 점 A에서 AD=AF · AF=" + q + " cm";
                p.unitConcept = "내접원에서 전체 길이와 접선 구간";
                p.wrongWhole = ab <= 24 ? ab : 0;
            }
            else if (band == 3)
            {
                int ab = q + y, bc = y + z;
                p.prompt = "△ABC의 내접원이 선분 AB, BC, CA와 각각 D, E, F에서 접한다. AB=" + ab + " cm, BC=" + bc + " cm, CF=" + z + " cm이다. AF=" + z + " cm라는 기록을 바르게 고치시오.";
                p.evidence = "CE=CF=" + z + ", BD=BE=" + bc + "−" + z + "=" + y + ", AF=AD=" + ab + "−" + y + "=" + q + " cm";
                p.unitConcept = "서로 다른 꼭짓점의 접선 구간 연결";
                p.wrongVertex = z;
                p.wrongMid = y == q ? 0 : y;
                p.wrongWhole = ab <= 24 ? ab : 0;
            }
            else throw new ArgumentOutOfRangeException(nameof(band));
            return p;
        }

        public static List<SockProblem> BuildBank()
        {
            if (cache != null) return cache;
            cache = new List<SockProblem>(360);
            for (int band = 1; band <= 3; band++)
            for (int q = 1; q <= 24; q++)
            for (int v = 1; v <= 5; v++)
                cache.Add(Make(band, q, v, "sock-b" + band + "-q" + q.ToString("00") + "-v" + v));
            Audit(cache);
            return cache;
        }

        public static List<SockProblem> BuildDeck(Random rng, int serial)
        {
            var deck = new List<SockProblem>(9);
            var answers = new List<int>(24);
            for (int i = 1; i <= 24; i++) answers.Add(i);
            for (int i = answers.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); int t = answers[i]; answers[i] = answers[j]; answers[j] = t; }
            for (int r = 0; r < 9; r++)
            {
                int band = r / 3 + 1, q = answers[r], v = 1 + rng.Next(5);
                deck.Add(Make(band, q, v, "run" + serial + "-r" + (r + 1) + "-b" + band + "-q" + q.ToString("00") + "-v" + v));
            }
            return deck;
        }

        public static string Diagnose(SockProblem p, int value)
        {
            if (value == p.answerCm) return "";
            if (p.band == 1 && value == p.wrongCenter) return "center_distance";
            if (p.wrongWhole > 0 && value == p.wrongWhole) return "whole_side";
            if (p.band == 3 && value == p.wrongVertex) return "different_vertex";
            if (p.band == 3 && p.wrongMid > 0 && value == p.wrongMid) return "stopped_midway";
            return value < p.answerCm ? "too_short" : "too_long";
        }

        public static List<MgfProblem> ToMgf(List<SockProblem> bank)
        {
            var list = new List<MgfProblem>(bank.Count);
            foreach (var p in bank) list.Add(new MgfProblem {
                id = p.id, prompt = p.prompt, choices = null, answer = p.answerCm + " cm",
                answerNumeric = p.answerCm, unitConcept = p.unitConcept
            });
            return list;
        }

        static void Audit(List<SockProblem> bank)
        {
            if (bank.Count != 360) throw new Exception("문제 은행 360개 불일치");
            var ids = new HashSet<string>(); var prompts = new HashSet<string>(); var counts = new int[4,25];
            foreach (var p in bank)
            {
                if (!ids.Add(p.id) || !prompts.Add(p.prompt)) throw new Exception("문항 중복: " + p.id);
                if (p.answerCm != p.x || p.answerCm < 1 || p.answerCm > 24) throw new Exception("정답 오류: " + p.id);
                if (p.band == 2 && (p.x + p.y) - p.y != p.x) throw new Exception("2단계 정수식 오류: " + p.id);
                if (p.band == 3 && ((p.y + p.z) - p.z != p.y || (p.x + p.y) - p.y != p.x || p.z == p.x)) throw new Exception("3단계 정수식 오류: " + p.id);
                if (p.wrongCenter == p.answerCm || p.wrongWhole == p.answerCm || p.wrongVertex == p.answerCm || p.wrongMid == p.answerCm) throw new Exception("오개념 우연 정답: " + p.id);
                counts[p.band,p.answerCm]++;
            }
            for (int b=1;b<=3;b++) for(int q=1;q<=24;q++) if(counts[b,q]!=5) throw new Exception("답 분포 오류");
        }
    }
}
