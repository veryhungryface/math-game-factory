using System;
using System.Collections.Generic;
using Mgf;
using UnityEngine;

namespace Mgf.FoldlineLab
{
    public enum FoldTaskKind { Symmetry, VertexPair, SidePair, Impossible }

    [Serializable]
    public sealed class FoldLine
    {
        public Vector2 point;
        public Vector2 direction;

        public FoldLine(Vector2 p, Vector2 d)
        {
            point = p;
            direction = d.sqrMagnitude < .0001f ? Vector2.up : d.normalized;
        }

        public Vector2 Normal { get { return new Vector2(-direction.y, direction.x); } }
        public float Offset { get { return Vector2.Dot(Normal, point); } }
    }

    [Serializable]
    public sealed class FoldTask
    {
        public string id = "";
        public string prompt = "";
        public string concept = "";
        public string answer = "";
        public FoldTaskKind kind;
        public int level;
        public int group;
        public bool haloAfter;
        public bool circumHalo;
        public Vector2 a, b, c;
        public FoldLine target;
        public FoldLine misconceptionA;
        public FoldLine misconceptionB;

        public MgfProblem ToMgf()
        {
            return new MgfProblem {
                id = id,
                prompt = prompt,
                choices = null,
                answer = answer,
                answerNumeric = double.NaN,
                unitConcept = concept
            };
        }
    }

    public struct FoldJudge
    {
        public bool correct;
        public float angleError;
        public float offsetError;
        public string misconceptionId;
    }

    // curriculum/2022-middle-math.json 표현 계약:
    // - 중학교 지시형 「~하시오」를 쓴다. 「가정」「결론」은 쓰지 않는다.
    // - △, ∠, ⊥, cm, ° 표기를 그대로 쓴다. 중2이므로 √는 생성하지 않는다.
    // - 외심은 변의 수직이등분선, 내심은 내각의 이등분선의 교점으로 구별한다.
    // - 문제 풀은 길이·회전·이동·각의 제약 만족 풀에서 직접 고른다. 난수 후 필터링은 하지 않는다.
    // - 오답 진단은 실제 선택지가 아니라 학생이 그은 선을 두 오개념 선과 비교해 역산한다.
    public static class FoldlineRules
    {
        public const int Goal = 6;
        public const int RequiredFirstCorrect = 5;
        public const int StartLives = 3;
        public const float SessionSeconds = 90f;
        public const float StageLength = 9.5f;
        public const float AngleTolerance = 4f;
        public const float OffsetTolerance = StageLength * .04f;

        public static FoldTask Practice()
        {
            Vector2 a = new Vector2(0, 3.0f), b = new Vector2(-3.1f, -2f), c = new Vector2(3.1f, -2f);
            return new FoldTask {
                id = "fl-practice",
                prompt = "AB=AC인 △ABC의 두 쪽이 겹치게 접으시오.",
                concept = "이등변삼각형의 꼭지각 이등분선",
                answer = "꼭짓점 A와 밑변 BC의 중점을 지나는 직선",
                kind = FoldTaskKind.Symmetry, level = 1, group = 0,
                a = a, b = b, c = c,
                target = Through(a, (b + c) * .5f),
                misconceptionA = Through(new Vector2(0, 3), new Vector2(0, -3)),
                misconceptionB = PerpendicularBisector(b, c)
            };
        }

        public static List<FoldTask> RuntimeDeck(int serial)
        {
            var deck = new List<FoldTask>(Goal);
            int s = Positive(serial);

            // 1. 이등변삼각형: 같은 두 변의 끼인각이 화면 위쪽이 아니도록 회전·이동한다.
            float r1 = 28f + (s * 37 % 116);
            Vector2 axis1 = Rot(Vector2.up, r1);
            Vector2 normal1 = new Vector2(-axis1.y, axis1.x);
            float side1 = ((s & 1) == 0 ? 1f : -1f) * (.95f + (s % 3) * .13f);
            // 회전에 독립적인 화면 중심선 반복을 막는다. 대칭축 offset은 항상 |u|>=0.95이다.
            Vector2 sh1 = normal1 * side1 + axis1 * (-.2f + (s % 3) * .2f);
            Vector2 a1 = Rot(new Vector2(0, 3), r1) + sh1;
            Vector2 b1 = Rot(new Vector2(-3, -2), r1) + sh1;
            Vector2 c1 = Rot(new Vector2(3, -2), r1) + sh1;
            deck.Add(new FoldTask {
                id = "fl-run-" + serial + "-1", kind = FoldTaskKind.Symmetry, level = 1, group = 1,
                prompt = "AB=AC인 △ABC의 두 쪽이 겹치게 접으시오.",
                concept = "이등변삼각형의 성질", answer = "∠A의 이등분선이자 BC의 수직이등분선",
                a = a1, b = b1, c = c1, target = Through(a1, (b1 + c1) * .5f),
                // misconceptionId=visual_top_vertex: 그림에서 가장 위인 점만 보고 그은 수직선
                misconceptionA = VerticalThrough(TopVertex(a1, b1, c1)),
                // misconceptionId=base_bisector_only: 밑변의 수직이등분 방향만 기억하고 위치를 놓침
                misconceptionB = Shift(Through(a1, (b1 + c1) * .5f), 1.25f)
            });

            // 2. SSA 일반화 함정: 이등변이 아닌 삼각형은 대칭 접기가 불가능하다.
            float r2 = -21f + (s * 29 % 72);
            Vector2 sh2 = new Vector2(.4f - (s % 4) * .35f, .15f);
            Vector2 a2 = Rot(new Vector2(-.4f, 3.0f), r2) + sh2;
            Vector2 b2 = Rot(new Vector2(-3.2f, -2.0f), r2) + sh2;
            Vector2 c2 = Rot(new Vector2(2.4f, -1.2f), r2) + sh2;
            deck.Add(new FoldTask {
                id = "fl-run-" + serial + "-2", kind = FoldTaskKind.Impossible, level = 1, group = 2,
                prompt = "같은 길이 표시가 없는 △ABC이다. 대칭 접기가 불가능하면 안전 깃을 돛 위에 꽂으시오.",
                concept = "이등변삼각형의 역과 SSA 오개념", answer = "대칭 접기 불가능",
                a = a2, b = b2, c = c2,
                misconceptionA = Through(a2, (b2 + c2) * .5f),
                misconceptionB = PerpendicularBisector(b2, c2)
            });

            // 3~4. 같은 둔각삼각형의 두 변 수직이등분선. 교점은 돛 밖의 외심이다.
            float r3 = -18f + (s * 13 % 37);
            // 두 수직이등분선 모두 원점을 우연히 지나지 않도록 로컬 좌표에서 먼저 이동한다.
            Vector2 sh3 = Rot(new Vector2(-1.05f - (s % 3) * .12f, .45f + (s % 2) * .12f), r3);
            Vector2 oa = Rot(new Vector2(-3.2f, -1.7f), r3) + sh3;
            Vector2 ob = Rot(new Vector2(3.0f, -1.7f), r3) + sh3;
            Vector2 oc = Rot(new Vector2(1.45f, -.05f), r3) + sh3;
            var o1 = PerpendicularBisector(oa, ob);
            var o2 = PerpendicularBisector(oa, oc);
            deck.Add(new FoldTask {
                id = "fl-run-" + serial + "-3", kind = FoldTaskKind.VertexPair, level = 2, group = 3,
                prompt = "꼭짓점 A가 B에 포개지게 접으시오.",
                concept = "외심과 선분의 수직이등분선", answer = "선분 AB의 수직이등분선",
                a = oa, b = ob, c = oc, target = o1,
                // misconceptionId=circumcenter_incenter_swapped: 꼭짓점을 지나는 각의 이등분선을 그음
                misconceptionA = AngleBisector(oa, ob, oc),
                // misconceptionId=midpoint_without_perpendicular: 중점은 맞지만 선분과 평행
                misconceptionB = new FoldLine((oa + ob) * .5f, ob - oa)
            });
            deck.Add(new FoldTask {
                id = "fl-run-" + serial + "-4", kind = FoldTaskKind.VertexPair, level = 3, group = 3,
                prompt = "꼭짓점 A가 C에 포개지게 접으시오. 두 자국의 교점을 확인하시오.",
                concept = "둔각삼각형의 외심", answer = "선분 AC의 수직이등분선",
                a = oa, b = ob, c = oc, target = o2, haloAfter = true, circumHalo = true,
                misconceptionA = AngleBisector(oa, ob, oc),
                // misconceptionId=outside_center_denial: 외심을 안쪽에 두려고 평행 이동한 선
                misconceptionB = Shift(o2, -1.35f)
            });

            // 5~6. 같은 예각삼각형의 두 내각 이등분선. 교점은 늘 내부의 내심이다.
            float r4 = 14f + (s * 17 % 58);
            // 각의 이등분선도 화면 중앙 고정선으로 맞힐 수 없게 회전 전 좌표에서 이동한다.
            Vector2 sh4 = Rot(new Vector2(-1.0f - (s % 3) * .1f, .55f + (s % 2) * .12f), r4);
            Vector2 ia = Rot(new Vector2(0, 3.0f), r4) + sh4;
            Vector2 ib = Rot(new Vector2(-3.1f, -2.1f), r4) + sh4;
            Vector2 ic = Rot(new Vector2(2.75f, -1.65f), r4) + sh4;
            var i1 = AngleBisector(ia, ib, ic);
            var i2 = AngleBisector(ib, ia, ic);
            deck.Add(new FoldTask {
                id = "fl-run-" + serial + "-5", kind = FoldTaskKind.SidePair, level = 3, group = 4,
                prompt = "변 AB가 AC에 포개지게 접으시오.",
                concept = "내심과 각의 이등분선", answer = "∠A의 이등분선",
                a = ia, b = ib, c = ic, target = i1,
                // misconceptionId=circumcenter_incenter_swapped: AB의 수직이등분선을 그음
                misconceptionA = PerpendicularBisector(ia, ib),
                // misconceptionId=visual_top_vertex: 화면 위쪽을 기준으로 수직선만 그음
                misconceptionB = VerticalThrough(TopVertex(ia, ib, ic))
            });
            deck.Add(new FoldTask {
                id = "fl-run-" + serial + "-6", kind = FoldTaskKind.SidePair, level = 3, group = 4,
                prompt = "변 BA가 BC에 포개지게 접으시오. 두 자국의 교점을 확인하시오.",
                concept = "삼각형의 내심", answer = "∠B의 이등분선",
                a = ia, b = ib, c = ic, target = i2, haloAfter = true, circumHalo = false,
                misconceptionA = PerpendicularBisector(ib, ic),
                misconceptionB = Shift(i2, 1.2f)
            });
            return deck;
        }

        public static FoldJudge Judge(FoldLine submitted, FoldTask task)
        {
            var j = new FoldJudge { misconceptionId = "precision_miss" };
            if (submitted == null || task == null || task.target == null) return j;
            Error(submitted, task.target, out j.angleError, out j.offsetError);
            j.correct = j.angleError <= AngleTolerance && j.offsetError <= OffsetTolerance;
            if (j.correct) { j.misconceptionId = ""; return j; }

            if (task.misconceptionA != null && Near(submitted, task.misconceptionA, 7f, .72f))
            {
                if (task.kind == FoldTaskKind.SidePair || task.kind == FoldTaskKind.VertexPair)
                    j.misconceptionId = "circumcenter_incenter_swapped";
                else j.misconceptionId = "visual_top_vertex";
            }
            else if (task.misconceptionB != null && Near(submitted, task.misconceptionB, 7f, .72f))
            {
                if (task.level >= 3 && task.kind == FoldTaskKind.VertexPair) j.misconceptionId = "outside_center_denial";
                else if (task.kind == FoldTaskKind.VertexPair) j.misconceptionId = "midpoint_without_perpendicular";
                else j.misconceptionId = "offset_only_miss";
            }
            else if (j.angleError <= AngleTolerance) j.misconceptionId = "offset_only_miss";
            else if (j.offsetError <= OffsetTolerance) j.misconceptionId = "direction_only_miss";
            return j;
        }

        public static void Error(FoldLine x, FoldLine target, out float angle, out float offset)
        {
            float dot = Mathf.Clamp(Mathf.Abs(Vector2.Dot(x.direction.normalized, target.direction.normalized)), 0f, 1f);
            angle = Mathf.Acos(dot) * Mathf.Rad2Deg;
            Vector2 xn = x.Normal, tn = target.Normal;
            float xc = x.Offset, tc = target.Offset;
            if (Vector2.Dot(xn, tn) < 0f) xc = -xc;
            offset = Mathf.Abs(xc - tc);
        }

        public static bool Near(FoldLine x, FoldLine y, float angleTol, float offsetTol)
        {
            Error(x, y, out float a, out float o);
            return a <= angleTol && o <= offsetTol;
        }

        public static FoldLine Through(Vector2 a, Vector2 b) { return new FoldLine(a, b - a); }
        public static FoldLine PerpendicularBisector(Vector2 a, Vector2 b)
        {
            Vector2 d = b - a;
            return new FoldLine((a + b) * .5f, new Vector2(-d.y, d.x));
        }
        public static FoldLine AngleBisector(Vector2 vertex, Vector2 p, Vector2 q)
        {
            Vector2 d = (p - vertex).normalized + (q - vertex).normalized;
            return new FoldLine(vertex, d);
        }
        public static FoldLine VerticalThrough(Vector2 p) { return new FoldLine(p, Vector2.up); }
        public static FoldLine Shift(FoldLine line, float signed)
        {
            return new FoldLine(line.point + line.Normal * signed, line.direction);
        }
        public static Vector2 Reflect(Vector2 p, FoldLine line)
        {
            Vector2 n = line.Normal;
            float d = Vector2.Dot(n, p) - line.Offset;
            return p - 2f * d * n;
        }
        public static bool Intersect(FoldLine a, FoldLine b, out Vector2 p)
        {
            float cross = Cross(a.direction, b.direction);
            if (Mathf.Abs(cross) < .0001f) { p = Vector2.zero; return false; }
            float t = Cross(b.point - a.point, b.direction) / cross;
            p = a.point + a.direction * t;
            return true;
        }
        public static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float c1 = Cross(b - a, p - a), c2 = Cross(c - b, p - b), c3 = Cross(a - c, p - c);
            bool neg = c1 < 0 || c2 < 0 || c3 < 0, pos = c1 > 0 || c2 > 0 || c3 > 0;
            return !(neg && pos);
        }
        public static float Cross(Vector2 a, Vector2 b) { return a.x * b.y - a.y * b.x; }
        public static Vector2 Rot(Vector2 p, float deg)
        {
            float r = deg * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(p.x * c - p.y * s, p.x * s + p.y * c);
        }
        public static Vector2 TopVertex(Vector2 a, Vector2 b, Vector2 c)
        {
            if (a.y >= b.y && a.y >= c.y) return a;
            return b.y >= c.y ? b : c;
        }
        static int Positive(int x) { return x == int.MinValue ? 0 : Math.Abs(x); }

        public static List<MgfProblem> BuildBank()
        {
            var bank = new List<MgfProblem>(420);
            // 제약 만족 매개변수 풀. 각 항목은 답 형식까지 같은 모델에서 만든다.
            for (int i = 0; i < 140; i++)
            {
                int baseCm = 4 + (i * 5 % 13);
                int apex = 28 + (i * 7 % 69);
                int rot = (i * 17) % 180;
                bank.Add(new MgfProblem {
                    id = "fl-bank-s-" + (i + 1),
                    prompt = "AB=AC, BC=" + baseCm + " cm, ∠A=" + apex + "°인 △ABC를 " + rot + "° 돌려 놓았다. 두 쪽이 겹치게 접으시오.",
                    choices = null,
                    answer = "∠A의 이등분선이자 BC의 수직이등분선",
                    answerNumeric = double.NaN,
                    unitConcept = "이등변삼각형의 꼭지각 이등분선"
                });
            }
            for (int i = 0; i < 140; i++)
            {
                int len = 3 + (i * 7 % 16);
                int rot = (i * 19 + 11) % 180;
                string pair = i % 3 == 0 ? "A가 B" : (i % 3 == 1 ? "B가 C" : "C가 A");
                string seg = i % 3 == 0 ? "AB" : (i % 3 == 1 ? "BC" : "CA");
                bank.Add(new MgfProblem {
                    id = "fl-bank-o-" + (i + 1),
                    prompt = "선분 " + seg + "=" + len + " cm인 △ABC를 " + rot + "° 돌려 놓았다. 꼭짓점 " + pair + "에 포개지게 접으시오.",
                    choices = null,
                    answer = "선분 " + seg + "의 수직이등분선",
                    answerNumeric = double.NaN,
                    unitConcept = "삼각형의 외심"
                });
            }
            for (int i = 0; i < 140; i++)
            {
                int angle = 31 + (i * 11 % 101);
                int rot = (i * 23 + 7) % 180;
                string vertex = i % 3 == 0 ? "A" : (i % 3 == 1 ? "B" : "C");
                string sides = vertex == "A" ? "AB가 AC" : (vertex == "B" ? "BA가 BC" : "CA가 CB");
                bank.Add(new MgfProblem {
                    id = "fl-bank-i-" + (i + 1),
                    prompt = "∠" + vertex + "=" + angle + "°인 △ABC를 " + rot + "° 돌려 놓았다. 변 " + sides + "에 포개지게 접으시오.",
                    choices = null,
                    answer = "∠" + vertex + "의 이등분선",
                    answerNumeric = double.NaN,
                    unitConcept = "삼각형의 내심"
                });
            }
            return bank;
        }

        public static bool Validate(out string error)
        {
            var bank = BuildBank();
            if (bank.Count < 300) { error = "bank count"; return false; }
            var ids = new HashSet<string>();
            var prompts = new HashSet<string>();
            for (int i = 0; i < bank.Count; i++)
            {
                var p = bank[i];
                if (!ids.Add(p.id) || !prompts.Add(p.prompt)) { error = "duplicate " + p.id; return false; }
                if (p.choices != null && p.choices.Length > 0) { error = "unexpected choices"; return false; }
                if (p.prompt.Contains("√") || p.prompt.Contains("가정") || p.prompt.Contains("결론")) { error = "expression trap"; return false; }
            }
            // 오개념 전수 검증: 실제 덱 2,000개에서 정답선과 두 오개념선이 허용창 안에서 일치하면 안 된다.
            for (int s = 1; s <= 2000; s++)
            {
                var deck = RuntimeDeck(s);
                if (deck.Count != Goal) { error = "deck count"; return false; }
                for (int i = 0; i < deck.Count; i++)
                {
                    var t = deck[i];
                    if (t.kind == FoldTaskKind.Impossible) continue;
                    if (t.target == null) { error = "missing target"; return false; }
                    if (t.misconceptionA != null && Near(t.target, t.misconceptionA, AngleTolerance, OffsetTolerance))
                    { error = "misconception A accidental truth " + t.id; return false; }
                    if (t.misconceptionB != null && Near(t.target, t.misconceptionB, AngleTolerance, OffsetTolerance))
                    { error = "misconception B accidental truth " + t.id; return false; }
                    var exact = Judge(t.target, t);
                    if (!exact.correct) { error = "exact target rejected " + t.id; return false; }
                }
            }
            error = "";
            return true;
        }
    }
}
