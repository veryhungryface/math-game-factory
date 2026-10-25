// 빙막 봉합 — SSS 합동의 대응 순서, 정수 변 은행, 실측 판정 정본.
// 중학교 표현 계약:
// - 합동 기호의 꼭짓점 순서와 대응변은 correspondence 한 배열에서 함께 만든다.
// - 길이는 "4 cm", 지시는 "옮기시오"로 쓴다.
// - SSA·AAA·근호·삼각비를 만들지 않고, 삼각형 부등식과 부등변 조건을 정수로 감사한다.
using System;
using System.Collections.Generic;
using Mgf;
using UnityEngine;

namespace Mgf.BingmakBonghap
{
    [Serializable]
    public sealed class IceProblem
    {
        public string id;
        public string geometryKey;
        public int band;
        public int[] sourceEdges = new int[3]; // AB, BC, CA
        public int[] correspondence = new int[3]; // A/B/C -> D/E/F
        public int[] targetEdges = new int[3]; // DE, EF, FD
        public int baseLength;
        public int targetDF;
        public int targetEF;
        public float targetX;
        public float targetY;
        public int displayRotation;
        public bool displayMirror;
        public string congruence;
        public string prompt;
        public string answer;
        public string unitConcept;
        public string initialMisconceptionId;
    }

    public static class BingmakRules
    {
        public const int StartLives = 3;
        public const int RoundCount = 8;
        public const int MasteryNeeded = 6;
        public const float RunSeconds = 150f;
        public const float Tolerance = .25f;
        public const float DomainMinX = -1f;
        public const float DomainMaxX = 11f;
        public const float DomainMinY = 0f;
        public const float DomainMaxY = 10f;

        static readonly int[][] Permutations = {
            new[] { 0, 1, 2 }, new[] { 0, 2, 1 }, new[] { 1, 0, 2 },
            new[] { 1, 2, 0 }, new[] { 2, 0, 1 }, new[] { 2, 1, 0 }
        };
        static readonly string[] T = { "D", "E", "F" };

        public static IceProblem Practice()
        {
            // AB=4, BC=5, CA=3이고 △ABC≡△FDE이면 DE=3, DF=4, EF=5인 고정 3·4·5 연습이다.
            var p = Make("practice-345", "practice-345", 0, new[] { 4, 3, 5 }, new[] { 2, 0, 1 }, 0, false);
            p.prompt = "△ABC≡△FDE가 되도록 꼭짓점 F를 옮기시오.";
            p.unitConcept = "고정 연습: 세 변이 각각 같은 SSS 합동";
            return p;
        }

        public static List<IceProblem> BuildBank()
        {
            // 무작위 생성 후 거르는 대신, 아래 정수 제약을 만족하는 풀을 먼저 완성한다.
            // ① scalene, ② 삼각형 부등식, ③ 가장 큰 각≤90°, ④ 가장 작은 각≥30°.
            var unordered = new List<int[]>();
            for (int a = 3; a <= 12; a++)
            for (int b = a + 1; b <= 12; b++)
            for (int c = b + 1; c <= 12; c++)
            {
                if (a + b <= c) continue;
                if (c * c > a * a + b * b) continue; // 가장 큰 각 90° 이하(기획 상한 110°보다 엄격)
                int cosNumerator = b * b + c * c - a * a;
                if (cosNumerator * cosNumerator > 3 * b * b * c * c) continue; // 가장 작은 각 30° 이상
                unordered.Add(new[] { a, b, c });
            }
            if (unordered.Count < 10) throw new InvalidOperationException("SSS constraint pool too small");

            // 열 개의 서로 다른 변 삼중항 × 여섯 라벨 배치 = 60개 구조.
            // 각 구조를 여섯 대응 순열로 전개해 정확히 360개를 만든다.
            var labelled = new List<int[]>(60);
            var labelledKeys = new List<string>(60);
            for (int i = 0; i < 10; i++)
            for (int s = 0; s < 6; s++)
            {
                int[] q = Permutations[s];
                labelled.Add(new[] { unordered[i][q[0]], unordered[i][q[1]], unordered[i][q[2]] });
                labelledKeys.Add(unordered[i][0] + "-" + unordered[i][1] + "-" + unordered[i][2]);
            }

            var bank = new List<IceProblem>(360);
            int serial = 1;
            for (int g = 0; g < labelled.Count; g++)
            for (int c = 0; c < 6; c++)
            {
                int band = g < 20 ? 1 : g < 40 ? 2 : 3;
                int rot = band == 1 ? 0 : new[] { -18, -10, 10, 18 }[(g + c) % 4];
                bool mirror = band >= 2 && ((g + c) & 1) == 1;
                var perm = ClonePerm((c + g * 5) % 6);
                bank.Add(Make("bb-" + serial.ToString("000"), labelledKeys[g] + "-" + g, band,
                    new[] { labelled[g][0], labelled[g][1], labelled[g][2] }, perm, rot, mirror));
                serial++;
            }
            Audit(bank);
            return bank;
        }

        static int[] ClonePerm(int i) => new[] { Permutations[i][0], Permutations[i][1], Permutations[i][2] };

        static IceProblem Make(string id, string geometryKey, int band, int[] source, int[] perm, int rotation, bool mirror)
        {
            var p = new IceProblem {
                id = id, geometryKey = geometryKey, band = band, sourceEdges = source,
                correspondence = perm, displayRotation = rotation, displayMirror = mirror
            };
            TransferEdges(p);
            p.baseLength = p.targetEdges[0];
            p.targetEF = p.targetEdges[1];
            p.targetDF = p.targetEdges[2];
            int numerator = p.targetDF * p.targetDF + p.baseLength * p.baseLength - p.targetEF * p.targetEF;
            p.targetX = numerator / (2f * p.baseLength);
            float y2 = p.targetDF * p.targetDF - p.targetX * p.targetX;
            if (y2 <= 0) throw new InvalidOperationException(id + " degenerate target");
            p.targetY = Mathf.Sqrt(y2);
            string order = T[perm[0]] + T[perm[1]] + T[perm[2]];
            p.congruence = "△ABC≡△" + order;
            p.prompt = p.congruence + "가 되도록 표시된 반평면에서 꼭짓점 F를 옮기시오.";
            p.answer = "DE=" + p.baseLength + ";DF=" + p.targetDF + ";EF=" + p.targetEF +
                ";target=(" + p.targetX.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + "," +
                p.targetY.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + ");tol=0.25;halfplane=marked";
            p.unitConcept = band == 1 ? "SSS 합동과 대응변" :
                band == 2 ? "회전·반사된 합동 도형의 대응" : "대응변 오류 찾기와 수정";
            p.initialMisconceptionId = band == 3 || (band == 2 && (id[id.Length - 1] & 1) == 0)
                ? "screen_position_correspondence_swap" : "single_side_only";
            return p;
        }

        static void TransferEdges(IceProblem p)
        {
            // sourceEdges와 합동 기호를 같은 correspondence에서 만든다.
            int[,] ends = { { 0, 1 }, { 1, 2 }, { 2, 0 } };
            p.targetEdges = new int[3];
            for (int e = 0; e < 3; e++)
            {
                int a = p.correspondence[ends[e, 0]], b = p.correspondence[ends[e, 1]];
                p.targetEdges[EdgeIndex(a, b)] = p.sourceEdges[e];
            }
        }

        static int EdgeIndex(int a, int b)
        {
            if ((a == 0 && b == 1) || (a == 1 && b == 0)) return 0; // DE
            if ((a == 1 && b == 2) || (a == 2 && b == 1)) return 1; // EF
            return 2; // FD
        }

        public static bool IsCorrect(IceProblem p, Vector2 f)
        {
            float dx = f.x - p.targetX, dy = f.y - p.targetY;
            return dx * dx + dy * dy <= Tolerance * Tolerance;
        }

        public static Vector2 InitialPoint(IceProblem p)
        {
            if (p.initialMisconceptionId == "screen_position_correspondence_swap")
            {
                // 오개념 1: 그림의 왼쪽 변끼리 대응시켜 DF·EF 목표를 맞바꾼 위치.
                float x = p.baseLength - p.targetX;
                return new Vector2(x, p.targetY);
            }
            // 오개념 2: DF 하나만 맞춘 채 EF를 확인하지 않은 위치.
            float targetAngle = Mathf.Atan2(p.targetY, p.targetX) * Mathf.Rad2Deg;
            // 변 길이가 큰 문제도 12 cm × 10 cm 제작면 안에 들어오도록 후보 풀에서 고른다.
            // 모든 후보는 D 중심, 반지름 DF인 원 위이므로 DF만 정확하다는 오개념은 보존된다.
            float[] offsets = { 20f, -20f, 35f, -35f, 50f, -50f, 70f, -70f, 90f, -90f };
            Vector2 target = new Vector2(p.targetX, p.targetY);
            for (int i = 0; i < offsets.Length; i++)
            {
                float r = (targetAngle + offsets[i]) * Mathf.Deg2Rad;
                var candidate = new Vector2(Mathf.Cos(r) * p.targetDF, Mathf.Sin(r) * p.targetDF);
                if (candidate.y > 0f && candidate.x >= DomainMinX && candidate.x <= DomainMaxX &&
                    candidate.y <= DomainMaxY && Vector2.Distance(candidate, target) >= 1f)
                    return candidate;
            }
            throw new InvalidOperationException(p.id + " has no in-domain single-side misconception");
        }

        public static string Diagnose(IceProblem p, Vector2 f)
        {
            float df = f.magnitude;
            float ef = Vector2.Distance(f, new Vector2(p.baseLength, 0));
            float swapped = Vector2.Distance(f, new Vector2(p.baseLength - p.targetX, p.targetY));
            if (swapped <= .35f) return "screen_position_correspondence_swap";
            bool dfOk = Mathf.Abs(df - p.targetDF) <= .25f;
            bool efOk = Mathf.Abs(ef - p.targetEF) <= .25f;
            if (dfOk != efOk) return "single_side_only";
            return "outline_similarity_without_lengths";
        }

        public static MgfProblem ToMgf(IceProblem p)
        {
            return new MgfProblem {
                id = p.id,
                prompt = "AB=" + p.sourceEdges[0] + " cm, BC=" + p.sourceEdges[1] + " cm, CA=" + p.sourceEdges[2] +
                    " cm이고 " + p.prompt,
                choices = null,
                answer = p.answer,
                answerNumeric = p.baseLength * 10000 + p.targetDF * 100 + p.targetEF,
                unitConcept = p.unitConcept
            };
        }

        static void Audit(List<IceProblem> bank)
        {
            if (bank.Count != 360) throw new InvalidOperationException("problem bank must contain 360, got " + bank.Count);
            var ids = new HashSet<string>();
            var prompts = new HashSet<string>();
            for (int i = 0; i < bank.Count; i++)
            {
                var p = bank[i];
                if (!ids.Add(p.id)) throw new InvalidOperationException("duplicate id " + p.id);
                string prompt = ToMgf(p).prompt;
                if (!prompts.Add(prompt)) throw new InvalidOperationException("duplicate prompt " + p.id);
                int a = p.sourceEdges[0], b = p.sourceEdges[1], c = p.sourceEdges[2];
                if (a == b || b == c || c == a) throw new InvalidOperationException(p.id + " must be scalene");
                int max = Math.Max(a, Math.Max(b, c));
                if (a + b + c - max <= max) throw new InvalidOperationException(p.id + " triangle inequality");
                if (p.targetEdges[0] == 0 || p.targetEdges[1] == 0 || p.targetEdges[2] == 0)
                    throw new InvalidOperationException(p.id + " target edge transfer");
                float d2 = p.targetX * p.targetX + p.targetY * p.targetY;
                float e2 = (p.targetX - p.baseLength) * (p.targetX - p.baseLength) + p.targetY * p.targetY;
                if (Mathf.Abs(d2 - p.targetDF * p.targetDF) > .01f || Mathf.Abs(e2 - p.targetEF * p.targetEF) > .01f)
                    throw new InvalidOperationException(p.id + " target coordinate mismatch");
                Vector2 initial = InitialPoint(p);
                if (IsCorrect(p, initial) || Vector2.Distance(initial, new Vector2(p.targetX, p.targetY)) < 1f)
                    throw new InvalidOperationException(p.id + " misconception accidentally correct");
                if (initial.y <= 0 || initial.x < DomainMinX || initial.x > DomainMaxX || initial.y > DomainMaxY)
                    throw new InvalidOperationException(p.id + " initial out of domain " + initial);
            }
        }
    }
}
