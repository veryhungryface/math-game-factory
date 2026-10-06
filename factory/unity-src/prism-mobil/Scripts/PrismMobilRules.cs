using System;
using System.Collections.Generic;
using Mgf;

namespace Mgf.PrismMobil
{
    public enum PrismProblemKind { Parallel, Median, Centroid }

    [Serializable]
    public sealed class PrismProblem
    {
        public string id;
        public PrismProblemKind kind;
        public string prompt;
        public string answer;
        public int answerNumeric;
        public int maxNode;
        public int answerNode;
        public int ratioP;
        public int ratioQ;
        public int aeLength;
        public int ecLength;
        public int totalLength;
        public int ax, ay, bx, by, cx, cy, gx, gy;
        public int misconceptionMid;
        public int misconceptionReverse;
        public int misconceptionMixed;
    }

    public static class PrismMobilRules
    {
        public const string MixPartWhole = "mix_part_part_with_part_whole";
        public const string MedianMidpoint = "centroid_as_median_midpoint";
        public const string ReverseTwoToOne = "reverse_two_to_one";
        public const string ShapeScreenCenter = "centroid_as_screen_center";
        public const string Other = "other_point";

        static int Gcd(int a, int b)
        {
            a = Math.Abs(a); b = Math.Abs(b);
            while (b != 0) { int t = a % b; a = b; b = t; }
            return Math.Max(1, a);
        }

        // expression_traps 정본:
        // 1) 무게중심은 중선을 "꼭짓점으로부터" 2:1로 나눈다. AG:GD 순서를 화면에 함께 쓴다.
        // 2) 중2에서는 근호를 쓰지 않는다. 이 게임은 모든 길이와 좌표를 정수로 생성한다.
        // 3) 수와 단위는 띄어 쓴다(6 cm). 도형 기호는 △ABC, DE∥BC로 쓴다.
        public static PrismProblem Practice()
        {
            return new PrismProblem
            {
                id = "practice-ad6", kind = PrismProblemKind.Median,
                prompt = "중선 AD=6 cm이다. 꼭짓점 A로부터 무게중심 G의 위치를 찾으시오.",
                answer = "A에서 4 cm", answerNumeric = 4, answerNode = 4,
                maxNode = 6, totalLength = 6, misconceptionMid = 3,
                misconceptionReverse = 2, misconceptionMixed = -1
            };
        }

        public static List<PrismProblem> BuildBank()
        {
            var all = new List<PrismProblem>(420);
            int id = 1;

            // 정답 눈금 k를 먼저 고르고, k:(24-k)가 되도록 자료를 역으로 만든다.
            // 무작위 생성 뒤 필터하지 않으므로 모든 문항이 구조적으로 조건을 만족한다.
            for (int k = 2; k <= 22; k++)
            {
                int g = Gcd(k, 24 - k);
                int p = k / g, q = (24 - k) / g;
                for (int scale = 1; scale <= 8; scale++)
                {
                    int ae = p * scale, ec = q * scale;
                    int mixedDen = 2 * p + q;
                    int mixed = (24 * p + mixedDen / 2) / mixedDen;
                    if (mixed == k) mixed = Math.Max(1, k - 1);
                    all.Add(new PrismProblem
                    {
                        id = "parallel-" + id++, kind = PrismProblemKind.Parallel,
                        prompt = "△ABC에서 DE∥BC이고 AE=" + ae + " cm, EC=" + ec +
                                 " cm이다. 선분 AB를 24등분한 눈금에서 점 D의 위치를 찾으시오.",
                        answer = "A에서 " + k + "번째 눈금", answerNumeric = k,
                        answerNode = k, maxNode = 24, ratioP = p, ratioQ = q,
                        aeLength = ae, ecLength = ec,
                        misconceptionMixed = mixed, misconceptionMid = 12,
                        misconceptionReverse = 24 - k
                    });
                }
            }

            // AD=3u를 먼저 정해 AG=2u, GD=u가 언제나 자연수가 되게 한다.
            for (int u = 1; u <= 8; u++)
            {
                int total = 3 * u;
                all.Add(new PrismProblem
                {
                    id = "median-" + id++, kind = PrismProblemKind.Median,
                    prompt = "점 G는 △ABC의 무게중심이고 중선 AD=" + total +
                             " cm이다. 꼭짓점 A로부터 AG의 길이를 구하시오.",
                    answer = (2 * u) + " cm", answerNumeric = 2 * u,
                    answerNode = 2 * u, maxNode = total, totalLength = total,
                    misconceptionMid = total / 2, misconceptionReverse = u,
                    misconceptionMixed = -1
                });
            }

            // 각 좌표합을 3의 배수로 직접 구성해 G가 정수 격자점이 되게 한다.
            // 비정삼각형만 사용하고, 바운딩박스 중심과 G가 겹치지 않는 템플릿을 평행이동한다.
            int[,] templates =
            {
                { 1,1, 10,3, 10,8 }, { 1,1, 10,5, 10,9 }, { 1,2, 10,4, 10,9 },
                { 1,2, 10,5, 10,8 }, { 1,3, 10,4, 10,8 }, { 1,3, 10,5, 10,10 },
                { 1,4, 10,5, 10,9 }, { 2,3, 11,1, 2,8 }
            };
            for (int t = 0; t < templates.GetLength(0); t++)
            for (int ox = 0; ox <= 1; ox++)
            for (int oy = 0; oy <= 1; oy++)
            for (int tag = 0; tag < 5; tag++)
            {
                int ax = templates[t,0] + ox, ay = templates[t,1] + oy;
                int bx = templates[t,2] + ox, by = templates[t,3] + oy;
                int cx = templates[t,4] + ox, cy = templates[t,5] + oy;
                if (tag == 1) { int tx = bx, ty = by; bx = cx; by = cy; cx = tx; cy = ty; }
                else if (tag == 2) { ax = 12 - ax; bx = 12 - bx; cx = 12 - cx; }
                else if (tag == 3) { ay = 12 - ay; by = 12 - by; cy = 12 - cy; }
                else if (tag == 4)
                {
                    int ta = ax; ax = ay; ay = ta;
                    int tb = bx; bx = by; by = tb;
                    int tc = cx; cx = cy; cy = tc;
                }
                int gx = (ax + bx + cx) / 3, gy = (ay + by + cy) / 3;
                all.Add(new PrismProblem
                {
                    id = "centroid-" + id++, kind = PrismProblemKind.Centroid,
                    prompt = "△ABC에서 A의 좌표는 x=" + ax + ", y=" + ay + ", B는 x=" + bx + ", y=" + by +
                             ", C는 x=" + cx + ", y=" + cy + "이다. 두 중선을 그어 무게중심을 찾으시오.",
                    answer = "(" + gx + "," + gy + ")", answerNumeric = gx * 100 + gy,
                    answerNode = gx * 13 + gy, maxNode = 169,
                    ax = ax, ay = ay, bx = bx, by = by, cx = cx, cy = cy, gx = gx, gy = gy,
                    misconceptionMid = ((ax + bx) / 2) * 13 + ((ay + by) / 2),
                    misconceptionReverse = ((ax + cx) / 2) * 13 + ((ay + cy) / 2),
                    misconceptionMixed = ((Math.Min(ax, Math.Min(bx, cx)) + Math.Max(ax, Math.Max(bx, cx))) / 2) * 13 +
                                         ((Math.Min(ay, Math.Min(by, cy)) + Math.Max(ay, Math.Max(by, cy))) / 2)
                });
            }
            return all;
        }

        public static MgfProblem ToMgf(PrismProblem p)
        {
            string[] choices;
            if (p.kind == PrismProblemKind.Centroid)
            {
                choices = new string[169];
                int n = 0;
                for (int y = 0; y <= 12; y++)
                for (int x = 0; x <= 12; x++) choices[n++] = "(" + x + "," + y + ")";
            }
            else
            {
                choices = new string[p.maxNode + 1];
                for (int i = 0; i <= p.maxNode; i++)
                    choices[i] = p.kind == PrismProblemKind.Parallel ? "A에서 " + i + "번째 눈금" : i + " cm";
            }
            return new MgfProblem
            {
                id = p.id, prompt = p.prompt, choices = choices, answer = p.answer,
                answerNumeric = p.answerNumeric,
                unitConcept = p.kind == PrismProblemKind.Parallel ? "삼각형에서 평행선과 선분의 길이의 비" :
                              p.kind == PrismProblemKind.Median ? "무게중심은 중선을 꼭짓점으로부터 2:1로 나눈다" :
                              "두 중선의 교점인 무게중심"
            };
        }

        public static bool IsCorrect(PrismProblem p, int selectedNode) => p.answerNode == selectedNode;

        public static string Diagnose(PrismProblem p, int selectedNode)
        {
            if (p.kind == PrismProblemKind.Median)
            {
                if (selectedNode == p.misconceptionMid) return MedianMidpoint;
                if (selectedNode == p.misconceptionReverse) return ReverseTwoToOne;
            }
            else if (p.kind == PrismProblemKind.Parallel)
            {
                if (selectedNode == p.misconceptionMixed) return MixPartWhole;
                if (selectedNode == p.misconceptionReverse) return ReverseTwoToOne;
            }
            else if (selectedNode == p.misconceptionMixed) return ShapeScreenCenter;
            return Other;
        }
    }
}
