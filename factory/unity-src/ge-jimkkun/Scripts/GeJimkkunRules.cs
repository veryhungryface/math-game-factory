// 게 짐꾼 — 삼각형 합동 조건의 대응 꼭짓점 생성기와 정수 판정 정본.
// 중학교 표현 계약: 지시형 "~옮기시오", 길이는 "4 cm", 각은 "60°",
// 합동 기호는 대응 배열과 같은 순서로 만든다. 중1 범위 밖의 근호·삼각비·SSA·AAA는 만들지 않는다.
using System;
using System.Collections.Generic;
using Mgf;

namespace Mgf.GeJimkkun
{
    [Serializable]
    public sealed class CargoProblem
    {
        public string id;
        public int band;
        public string kind;
        public int[] sourceEdges = new int[3]; // AB, BC, CA. 0은 제시하지 않음.
        public int[] sourceAngles = new int[3]; // A, B, C. 0은 제시하지 않음.
        public int[] targetEdges = new int[3]; // DE, EF, FD.
        public int[] targetAngles = new int[3]; // D, E, F.
        public int[] correspondence = new int[3]; // A/B/C -> D/E/F index.
        public string prompt;
        public string sourceSummary;
        public string targetSummary;
        public string answer;
        public string concept;
    }

    public static class GeJimkkunRules
    {
        public const int StartLives = 3;
        public const int RoundCount = 8;
        public const int MasteryNeeded = 6;
        public const float RunSeconds = 90f;

        static readonly int[][] Permutations = {
            new[] { 0, 1, 2 }, new[] { 0, 2, 1 }, new[] { 1, 0, 2 },
            new[] { 1, 2, 0 }, new[] { 2, 0, 1 }, new[] { 2, 1, 0 }
        };
        static readonly string[] S = { "A", "B", "C" };
        static readonly string[] T = { "D", "E", "F" };

        public static CargoProblem Practice()
        {
            return MakeSss("practice-fixed", 4, 6, 5, new[] { 2, 1, 0 });
        }

        public static List<CargoProblem> BuildBank()
        {
            var list = new List<CargoProblem>(360);
            var triples = new List<int[]>();
            for (int a = 3; a <= 10 && triples.Count < 20; a++)
            for (int b = a + 1; b <= 11 && triples.Count < 20; b++)
            for (int c = b + 1; c <= 12 && triples.Count < 20; c++)
                if (a + b > c) triples.Add(new[] { a, b, c });
            int serial = 1;
            for (int i = 0; i < triples.Count; i++)
            for (int p = 0; p < 6; p++)
                list.Add(MakeSss("gj-sss-" + serial++, triples[i][0], triples[i][2], triples[i][1], ClonePerm(p)));

            var sas = new List<int[]>();
            for (int a = 3; a <= 10 && sas.Count < 20; a++)
            for (int b = 3; b <= 10 && sas.Count < 20; b++)
            for (int angle = 40; angle <= 110 && sas.Count < 20; angle += 10)
                if (a != b) sas.Add(new[] { a, b, angle });
            serial = 1;
            for (int i = 0; i < sas.Count; i++)
            for (int p = 0; p < 6; p++)
                list.Add(MakeSas("gj-sas-" + serial++, sas[i][0], sas[i][1], sas[i][2], ClonePerm((p + i * 5) % 6)));

            var asa = new List<int[]>();
            for (int side = 3; side <= 10 && asa.Count < 20; side++)
            for (int a = 30; a <= 80 && asa.Count < 20; a += 10)
            for (int b = 30; b <= 80 && asa.Count < 20; b += 10)
                if (a != b && a + b < 180 && 180 - a - b != a && 180 - a - b != b)
                    asa.Add(new[] { side, a, b });
            serial = 1;
            for (int i = 0; i < asa.Count; i++)
            for (int p = 0; p < 6; p++)
                list.Add(MakeAsa("gj-asa-" + serial++, asa[i][0], asa[i][1], asa[i][2], ClonePerm((p + i * 3) % 6)));

            if (list.Count != 360) throw new InvalidOperationException("problem bank must contain 360 problems, got " + list.Count);
            var ids = new HashSet<string>();
            for (int i = 0; i < list.Count; i++)
            {
                if (!ids.Add(list[i].id)) throw new InvalidOperationException("duplicate id " + list[i].id);
                int solutions = CountSolutions(list[i]);
                if (solutions != 1) throw new InvalidOperationException(list[i].id + " unique-solution audit: " + solutions);
                if (!IsCorrect(list[i], list[i].correspondence)) throw new InvalidOperationException(list[i].id + " answer mismatch");
            }
            return list;
        }

        static int[] ClonePerm(int i) => new[] { Permutations[i][0], Permutations[i][1], Permutations[i][2] };

        static CargoProblem MakeSss(string id, int ab, int bc, int ca, int[] perm)
        {
            var p = Base(id, 1, "SSS", perm);
            p.sourceEdges = new[] { ab, bc, ca };
            Transfer(p);
            Finish(p);
            return p;
        }

        static CargoProblem MakeSas(string id, int ab, int ac, int angleA, int[] perm)
        {
            var p = Base(id, 2, "SAS", perm);
            p.sourceEdges = new[] { ab, 0, ac };
            p.sourceAngles = new[] { angleA, 0, 0 };
            Transfer(p);
            Finish(p);
            return p;
        }

        static CargoProblem MakeAsa(string id, int ab, int angleA, int angleB, int[] perm)
        {
            var p = Base(id, 3, "ASA", perm);
            p.sourceEdges = new[] { ab, 0, 0 };
            p.sourceAngles = new[] { angleA, angleB, 0 };
            Transfer(p);
            Finish(p);
            return p;
        }

        static CargoProblem Base(string id, int band, string kind, int[] perm)
        {
            return new CargoProblem {
                id = id, band = band, kind = kind, correspondence = perm,
                sourceEdges = new int[3], sourceAngles = new int[3],
                targetEdges = new int[3], targetAngles = new int[3]
            };
        }

        static void Transfer(CargoProblem p)
        {
            int[,] ends = { { 0, 1 }, { 1, 2 }, { 2, 0 } };
            for (int e = 0; e < 3; e++)
            {
                int v = p.sourceEdges[e]; if (v == 0) continue;
                int ta = p.correspondence[ends[e, 0]], tb = p.correspondence[ends[e, 1]];
                p.targetEdges[EdgeIndex(ta, tb)] = v;
            }
            for (int i = 0; i < 3; i++) if (p.sourceAngles[i] != 0)
                p.targetAngles[p.correspondence[i]] = p.sourceAngles[i];
        }

        static void Finish(CargoProblem p)
        {
            p.sourceSummary = Summary(p.sourceEdges, p.sourceAngles, S);
            p.targetSummary = Summary(p.targetEdges, p.targetAngles, T);
            p.prompt = p.sourceSummary + " / " + p.targetSummary + ". 주어진 조건을 이용하여 대응하는 꼭짓점으로 게를 옮기시오.";
            string order = T[p.correspondence[0]] + T[p.correspondence[1]] + T[p.correspondence[2]];
            p.answer = "A→" + T[p.correspondence[0]] + ", B→" + T[p.correspondence[1]] + ", C→" + T[p.correspondence[2]] +
                " · △ABC≡△" + order + " (" + p.kind + " 합동)";
            p.concept = p.kind == "SSS" ? "세 변의 대응과 SSS 합동" :
                p.kind == "SAS" ? "두 변과 끼인각의 대응 및 SAS 합동" : "한 변과 양 끝 각의 대응 및 ASA 합동";
        }

        static string Summary(int[] edges, int[] angles, string[] names)
        {
            var parts = new List<string>(3);
            string[] edgeNames = { names[0] + names[1], names[1] + names[2], names[2] + names[0] };
            for (int i = 0; i < 3; i++) if (edges[i] != 0) parts.Add(edgeNames[i] + "=" + edges[i] + " cm");
            for (int i = 0; i < 3; i++) if (angles[i] != 0) parts.Add("∠" + names[i] + "=" + angles[i] + "°");
            return string.Join(", ", parts.ToArray());
        }

        static int EdgeIndex(int a, int b)
        {
            if ((a == 0 && b == 1) || (a == 1 && b == 0)) return 0;
            if ((a == 1 && b == 2) || (a == 2 && b == 1)) return 1;
            return 2;
        }

        public static bool IsCorrect(CargoProblem p, int[] assignment)
        {
            if (assignment == null || assignment.Length != 3) return false;
            return assignment[0] == p.correspondence[0] && assignment[1] == p.correspondence[1] && assignment[2] == p.correspondence[2];
        }

        public static int CountSolutions(CargoProblem p)
        {
            int count = 0;
            for (int i = 0; i < 6; i++) if (PreservesConditions(p, Permutations[i])) count++;
            return count;
        }

        static bool PreservesConditions(CargoProblem p, int[] candidate)
        {
            int[,] ends = { { 0, 1 }, { 1, 2 }, { 2, 0 } };
            for (int e = 0; e < 3; e++)
            {
                int v = p.sourceEdges[e]; if (v == 0) continue;
                int te = EdgeIndex(candidate[ends[e, 0]], candidate[ends[e, 1]]);
                if (p.targetEdges[te] != v) return false;
            }
            for (int i = 0; i < 3; i++)
                if (p.sourceAngles[i] != 0 && p.targetAngles[candidate[i]] != p.sourceAngles[i]) return false;
            return true;
        }

        public static string Diagnose(CargoProblem p, int[] a)
        {
            if (a[0] == 0 && a[1] == 1 && a[2] == 2) return "alphabet_order";
            if (p.band == 1)
            {
                int kept = 0;
                int[,] ends = { { 0, 1 }, { 1, 2 }, { 2, 0 } };
                for (int e = 0; e < 3; e++) if (p.targetEdges[EdgeIndex(a[ends[e, 0]], a[ends[e, 1]])] == p.sourceEdges[e]) kept++;
                if (kept == 1) return "single_side_endpoint_swap";
            }
            if (p.band == 2 && a[0] != p.correspondence[0]) return "sas_included_vertex_split";
            if (p.band == 3 && a[0] == p.correspondence[1] && a[1] == p.correspondence[0]) return "asa_endpoint_angles_swapped";
            return "screen_position_mapping";
        }

        public static MgfProblem ToMgf(CargoProblem p)
        {
            return new MgfProblem {
                id = p.id, prompt = p.prompt, choices = null, answer = p.answer,
                answerNumeric = p.correspondence[0] * 100 + p.correspondence[1] * 10 + p.correspondence[2],
                unitConcept = p.concept
            };
        }
    }
}
