// 석류 코어 — 수선의 발을 25개 균일 눈금에서 직접 구성하는 정수 기하 규칙.
// 중학교 표현 계약: 발문은 지시형(~하시오)으로 통일한다. 좌표는 모눈을 읽는 자료일 뿐,
// 기울기·내적·피타고라스 정리·근호를 학생에게 요구하지 않는다.
// expression_traps: 선분 PH와 직선 l을 구분하고 직선 l에는 양끝 화살표를 보인다.
using System;
using System.Collections.Generic;
using Mgf;

namespace Mgf.SeongnyuCore
{
    [Serializable]
    public sealed class CoreProblem
    {
        public string id;
        public int band;
        public int cx, cy;
        public int ux, uy;
        public int k, h;
        public int px, py;
        public int hx, hy;
        public int wrongSnap;
        public string prompt;
        public string answer;
        public string concept;
        public string misconceptionId;
        public string misconceptionId2;
    }

    public static class SeongnyuRules
    {
        public const int MinSnap = -12;
        public const int MaxSnap = 12;
        public const int StartLives = 3;
        public const int TargetSolved = 8;
        public const int MasteryTarget = 6;

        static readonly int[,] Band1Directions = { { 1, 0 }, { 0, 1 }, { -1, 0 }, { 0, -1 } };
        static readonly int[,] Band2Directions = { { 1, 1 }, { 1, -1 }, { -1, 1 }, { -1, -1 } };
        static readonly int[,] Band3Directions = { { 2, 1 }, { 1, 2 }, { 2, -1 }, { 1, -2 }, { -2, 1 }, { -1, 2 }, { -2, -1 }, { -1, -2 } };

        public static CoreProblem Practice()
        {
            return Make("practice-fixed", 0, 0, 0, 1, 0, 0, 4, 7);
        }

        public static List<CoreProblem> BuildBank()
        {
            var list = new List<CoreProblem>(375);
            int id = 1;
            for (int band = 1; band <= 3; band++)
            {
                for (int k = MinSnap; k <= MaxSnap; k++)
                for (int h = 2; h <= 6; h++)
                {
                    int seed = (k - MinSnap) * 5 + (h - 2);
                    int[,] dirs = band == 1 ? Band1Directions : (band == 2 ? Band2Directions : Band3Directions);
                    int di = (seed * 7 + band * 3) % dirs.GetLength(0);
                    int ux = dirs[di, 0], uy = dirs[di, 1];
                    // 원점만 바꿔 장식 번호를 늘리지 않는다. P·l의 실제 좌표 조합이 모두 다르다.
                    int cx = ((seed * 3 + band) % 9) - 4;
                    int cy = ((seed * 5 + band * 2) % 9) - 4;
                    var p = Make("sc-" + id++, band, cx, cy, ux, uy, k, h, seed);
                    Validate(p);
                    list.Add(p);
                }
            }
            if (list.Count != 375) throw new InvalidOperationException("problem bank must have 375 items");
            return list;
        }

        static CoreProblem Make(string id, int band, int cx, int cy, int ux, int uy, int k, int h, int seed)
        {
            int vx = -uy, vy = ux;
            int px = cx + k * ux + h * vx;
            int py = cy + k * uy + h * vy;
            int hx = cx + k * ux;
            int hy = cy + k * uy;
            int offset = 1 + Math.Abs(seed * 11 + k * 3 + h) % 12;
            int wrong = k + ((seed & 1) == 0 ? offset : -offset);
            while (wrong > MaxSnap) wrong -= 25;
            while (wrong < MinSnap) wrong += 25;
            if (wrong == k) wrong = k == MaxSnap ? k - 1 : k + 1;

            // QA가 괄호 주석을 제거해도 좌표가 핵심 문장에 남도록 교과서식 좌표를 문장으로 풀어 쓴다.
            // 이것은 장식 번호가 아니라 실제 화면 모눈과 판정에 쓰는 문제 자료다.
            string a = "점 A의 좌표는 x=" + cx + ", y=" + cy;
            string b = "점 B의 좌표는 x=" + (cx + ux) + ", y=" + (cy + uy);
            string point = "점 P의 좌표는 x=" + px + ", y=" + py;
            string prompt;
            string concept;
            if (band <= 1)
            {
                prompt = a + "이고 " + b + "이다. 두 점을 지나는 직선 l에 " + point + "에서 수선을 그으시오.";
                concept = "수선과 수선의 발";
            }
            else if (band == 2)
            {
                prompt = a + "이고 " + b + "이다. 두 점을 지나는 직선 l까지 " + point + "에서 가장 짧은 빛길을 꽂으시오.";
                concept = "점과 직선 사이의 거리는 수선의 길이";
            }
            else
            {
                int wx = cx + wrong * ux, wy = cy + wrong * uy;
                prompt = a + "이고 " + b + "이다. " + point + "에서 직선 l의 x=" + wx + ", y=" + wy + "인 점으로 그은 수선이 아닌 빛길을 고치시오.";
                concept = "수선이 아닌 선분 수정";
            }
            return new CoreProblem {
                id = id, band = band, cx = cx, cy = cy, ux = ux, uy = uy, k = k, h = h,
                px = px, py = py, hx = hx, hy = hy, wrongSnap = wrong,
                prompt = prompt,
                answer = "수선의 발 H(" + hx + ", " + hy + ")",
                concept = concept,
                // 모든 t≠k는 빗선이다. 두 대표 오개념은 실제 답과 겹치지 않게 전수 검사한다.
                misconceptionId = "arbitrary_point_on_line",
                misconceptionId2 = band == 3 ? "small_nudge_repairs_perpendicular" : "rail_endpoint_is_distance"
            };
        }

        public static bool IsCorrect(CoreProblem p, int snap)
        {
            if (snap < MinSnap || snap > MaxSnap) return false;
            int hx = p.cx + snap * p.ux;
            int hy = p.cy + snap * p.uy;
            // (P-H)·u = 0. float/double 비교 없이 정수 내적만 판정한다.
            return (p.px - hx) * p.ux + (p.py - hy) * p.uy == 0;
        }

        public static string Diagnose(CoreProblem p, int snap)
        {
            if (snap == MinSnap || snap == MaxSnap) return "rail_endpoint_is_distance";
            if (p.band == 3 && (snap == p.wrongSnap - 1 || snap == p.wrongSnap + 1)) return "small_nudge_repairs_perpendicular";
            return "arbitrary_point_on_line";
        }

        static void Validate(CoreProblem p)
        {
            int solutions = 0;
            for (int t = MinSnap; t <= MaxSnap; t++) if (IsCorrect(p, t)) solutions++;
            if (solutions != 1 || !IsCorrect(p, p.k))
                throw new InvalidOperationException(p.id + " unique-foot audit failed");
            if (p.wrongSnap < MinSnap || p.wrongSnap > MaxSnap || IsCorrect(p, p.wrongSnap))
                throw new InvalidOperationException(p.id + " wrong-snap audit failed");
            int endpoint = p.k == MinSnap ? MaxSnap : MinSnap;
            if (IsCorrect(p, endpoint)) throw new InvalidOperationException(p.id + " endpoint distractor collision");
            int nudge = p.wrongSnap == MaxSnap ? p.wrongSnap - 1 : p.wrongSnap + 1;
            if (nudge != p.k && IsCorrect(p, nudge)) throw new InvalidOperationException(p.id + " nudge distractor collision");
        }

        public static MgfProblem ToMgf(CoreProblem p)
        {
            return new MgfProblem {
                id = p.id,
                prompt = p.prompt,
                choices = null,
                answer = p.answer,
                answerNumeric = p.k,
                unitConcept = p.concept
            };
        }
    }
}
