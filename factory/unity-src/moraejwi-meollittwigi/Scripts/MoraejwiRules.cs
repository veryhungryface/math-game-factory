// 모래쥐 멀리뛰기 — 중2 피타고라스 정리, 정수 좌표 구성형 문항과 판정.
//
// curriculum/2022-middle-math.json style_guide / expression_traps:
// - 발문은 지시형 「~하시오」로 통일한다.
// - 길이 m와 넓이 m²를 구분하고 수와 단위 사이를 띄어 쓴다.
// - 중2에는 제곱근이 없으므로 √와 근호 길이를 묻거나 표시하지 않는다.
// - 제곱수가 아닌 S는 「빗변 위 정사각형의 넓이」로만 묻는다.
// - 풀이가 길이를 복원할 때는 b²=144, b>0이므로 b=12처럼 자연수로 끝낸다.
//
// 오개념 역산(misconceptionId):
// - addLengths: a+b를 빗변 길이로 보고 (a+b)²와 S를 혼동한다.
// - areaAsLeg: 목표 넓이 S를 한 직각변의 제곱 자리에 그대로 놓는다.
// - addKnownSquare: 수리에서 S-a² 대신 S+a²를 사용한다.
// - wrongHypotenuse: 화면 방향으로 빗변을 정하고 직각의 맞은편을 확인하지 않는다.
//
// 모든 판정은 int 하나(a*a+b*b==S)다. 애니메이션만 float를 쓴다.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using UnityEngine;

namespace Mgf.MoraejwiMeollittwigi
{
    [Serializable]
    public sealed class LandingProblem
    {
        public string id;
        public int band;
        public int gridN;
        public int targetA;
        public int targetB;
        public int targetArea;
        public int startB;
        public int orientation;
        public int deltaAB;
        public string prompt;
        public string unitConcept;
        public string misconceptionId;

        public bool LockedAxis => band == 3;
        public bool HasDifferenceConstraint => band == 2;
        public int AnswerNumeric => LockedAxis ? targetB : targetArea;
    }

    public static class MoraejwiRules
    {
        public const int StartLives = 3;
        public const int RoundCount = 8;
        public const int RequiredFirstCorrect = 6;
        public const float RunSeconds = 120f;

        static List<LandingProblem> bankCache;

        public static LandingProblem Practice()
        {
            return Make("mj-practice", 0, 3, 4, 0, 0);
        }

        public static LandingProblem Make(string id, int band, int a, int b, int orientation, int startB, int deltaAB = 0)
        {
            int gridN = band <= 1 ? 6 : band == 2 ? 10 : 15;
            if (a < 1 || b < 1 || a > gridN || b > gridN)
                throw new ArgumentOutOfRangeException("양의 정수 격자 밖 문항: " + a + "," + b + " / " + gridN);
            int s = a * a + b * b;
            string prompt;
            string concept;
            if (band == 3)
            {
                prompt = "AC=" + a + " m일 때, 빗변 위 정사각형의 넓이가 " + s + " m²가 되도록 CB의 길이를 정하시오.";
                concept = "피타고라스 정리로 다른 한 변의 길이 구하기";
            }
            else if (band == 2)
            {
                string relation = deltaAB == 0
                    ? "AC와 CB의 길이가 같고"
                    : deltaAB > 0
                        ? "AC가 CB보다 " + deltaAB + " m 길고"
                        : "CB가 AC보다 " + (-deltaAB) + " m 길고";
                prompt = relation + ", 빗변 위 정사각형의 넓이가 " + s + " m²가 되는 착지점을 정하시오.";
                concept = "두 직각변의 길이 조건과 정사각형의 넓이 관계";
            }
            else
            {
                prompt = "빗변 위 정사각형의 넓이가 " + s + " m²가 되는 착지점을 정하시오.";
                concept = "두 직각변 위 정사각형의 넓이 관계";
            }
            return new LandingProblem {
                id = id,
                band = band,
                gridN = gridN,
                targetA = a,
                targetB = b,
                targetArea = s,
                startB = band == 3 ? ValidWrongStart(a, b, startB) : 0,
                orientation = orientation & 7,
                deltaAB = band == 2 ? deltaAB : 0,
                prompt = prompt,
                unitConcept = concept,
                misconceptionId = band == 3 ? "areaAsLeg" : "addLengths"
            };
        }

        static int ValidWrongStart(int a, int answerB, int candidate)
        {
            if (candidate >= 1 && candidate <= 15 && candidate != answerB) return candidate;
            // 넓이를 한 변으로 읽는 오개념을 실제 자연수로 만들 수 있으면 우선 쓴다.
            int s = a * a + answerB * answerB;
            for (int q = 1; q <= 15; q++) if (q != answerB && q * q == s) return q;
            int fallback = answerB == 15 ? 14 : answerB + 1;
            return fallback == answerB ? (answerB == 1 ? 2 : answerB - 1) : fallback;
        }

        public static bool IsCorrect(LandingProblem problem, int a, int b)
        {
            if (problem == null || a < 1 || b < 1 || a > problem.gridN || b > problem.gridN) return false;
            if (problem.LockedAxis && a != problem.targetA) return false;
            if (problem.HasDifferenceConstraint && a - b != problem.deltaAB) return false;
            return a * a + b * b == problem.targetArea;
        }

        public static int CountSolutions(LandingProblem problem)
        {
            int count = 0;
            for (int a = 1; a <= problem.gridN; a++)
            for (int b = 1; b <= problem.gridN; b++)
                if (IsCorrect(problem, a, b)) count++;
            return count;
        }

        public static string AnswerText(LandingProblem problem)
        {
            if (problem.LockedAxis) return "CB=" + problem.targetB + " m";
            var sb = new StringBuilder();
            for (int a = 1; a <= problem.gridN; a++)
            for (int b = 1; b <= problem.gridN; b++)
                if (IsCorrect(problem, a, b))
                {
                    if (sb.Length > 0) sb.Append(" · ");
                    sb.Append('(').Append(a).Append(',').Append(b).Append(')');
                }
            return sb.ToString();
        }

        public static string ClassifyMisconception(LandingProblem problem, int a, int b)
        {
            if (problem == null) return "invalid";
            int s = problem.targetArea;
            if ((a + b) * (a + b) == s) return "addLengths";
            if (a * a == s || b * b == s) return "areaAsLeg";
            if (problem.LockedAxis && b * b == s + problem.targetA * problem.targetA) return "addKnownSquare";
            if (problem.LockedAxis && a != problem.targetA) return "wrongHypotenuse";
            return a * a + b * b > s ? "squareSumTooLarge" : "squareSumTooSmall";
        }

        public static string Evidence(LandingProblem problem, int a, int b)
        {
            int made = a * a + b * b;
            int gap = Math.Abs(made - problem.targetArea);
            string relation = made > problem.targetArea ? "초과" : "부족";
            if (made == problem.targetArea)
                return a + "² + " + b + "² = " + problem.targetArea + " m²";
            return a + "² + " + b + "² = " + made + " m² · 목표보다 " + gap + " m² " + relation;
        }

        public static List<LandingProblem> BuildBank()
        {
            if (bankCache != null) return bankCache;
            var bank = new List<LandingProblem>(343);
            int serial = 1;
            // 밴드 1: 같은 목표 넓이는 한 번만 넣는다. 화면 방향은 presentation seed이고
            // 수학 문항의 고유성을 부풀리는 근거로 쓰지 않는다.
            var seenBand1Area = new HashSet<int>();
            for (int a = 1; a <= 6; a++)
            for (int b = 1; b <= 6; b++)
            {
                int s = a * a + b * b;
                if (seenBand1Area.Add(s)) bank.Add(Make(BankId(serial++), 1, a, b, (a + b) & 3, 0));
            }
            // 밴드 2: 목표 넓이뿐 아니라 두 직각변의 길이 차를 함께 판단한다.
            // (S, AC-CB)는 양의 정수 격자에서 순서쌍을 하나로 정하므로 100개가
            // 모두 실제로 다른 문제이며, 같은 IsCorrect 경로로 화면 입력을 판정한다.
            for (int a = 1; a <= 10; a++)
            for (int b = 1; b <= 10; b++)
                bank.Add(Make(BankId(serial++), 2, a, b, (a * 3 + b) & 7, 0, a - b));
            // 밴드 3: 알려진 a와 미지 b의 225쌍. a 고정이면 양의 해 b는 정확히 하나다.
            for (int a = 1; a <= 15; a++)
            for (int b = 1; b <= 15; b++)
                bank.Add(Make(BankId(serial++), 3, a, b, (a + b) & 7, 0));

            AuditBank(bank);
            bankCache = bank;
            return bankCache;
        }

        static string BankId(int serial) => "mj-bank-" + serial.ToString("000");

        static void AuditBank(List<LandingProblem> bank)
        {
            if (bank.Count < 300) throw new Exception("문제 은행 300개 미만");
            var ids = new HashSet<string>();
            var prompts = new HashSet<string>();
            foreach (var p in bank)
            {
                if (!ids.Add(p.id) || !prompts.Add(p.prompt)) throw new Exception("문제 은행 중복: " + p.id);
                if (!IsCorrect(p, p.targetA, p.targetB)) throw new Exception("생성 정답 불일치: " + p.id);
                int solutions = CountSolutions(p);
                if (p.band == 3 && solutions != 1) throw new Exception("수리 문항 유일해 아님: " + p.id + " / " + solutions);
                if (p.band == 2 && solutions != 1) throw new Exception("길이 차 문항 유일해 아님: " + p.id + " / " + solutions);
                if (p.band == 1 && solutions > 2) throw new Exception("6×6 해 상한 초과: " + p.id);
            }

            // distractor_rules 전수 검사: 아래 세 오개념식은 양의 정수 풀에서 정답과 우연히 같지 않는다.
            for (int a = 1; a <= 15; a++)
            for (int b = 1; b <= 15; b++)
            {
                int s = a * a + b * b;
                if ((a + b) * (a + b) == s) throw new Exception("addLengths 우연 정답: " + a + "," + b);
                if (a * a == s || b * b == s) throw new Exception("areaAsLeg 우연 정답: " + a + "," + b);
                for (int wrong = 1; wrong <= 15; wrong++)
                    if (wrong * wrong == s + a * a && a * a + wrong * wrong == s)
                        throw new Exception("addKnownSquare 우연 정답: " + a + "," + b + "," + wrong);
            }
        }

        public static LandingProblem DrawBand(int band, System.Random rng, HashSet<string> used = null)
        {
            var bank = BuildBank();
            int matches = 0;
            for (int i = 0; i < bank.Count; i++)
                if (bank[i].band == band && (used == null || !used.Contains(bank[i].id))) matches++;
            if (matches == 0) throw new Exception("뽑을 문항 없음: band " + band);
            int pick = rng.Next(matches);
            for (int i = 0; i < bank.Count; i++)
            {
                var p = bank[i];
                if (p.band != band || (used != null && used.Contains(p.id))) continue;
                if (pick-- == 0) { used?.Add(p.id); return Clone(p); }
            }
            throw new Exception("문항 추출 실패");
        }

        public static LandingProblem RepairFromCarry(int knownA, System.Random rng, int order)
        {
            int b = rng.Next(1, 16); // 독립 균등: 무뇌 우연 1/15.
            return Make("mj-run-repair-" + order + "-" + knownA + "-" + b,
                3, knownA, b, (knownA + b) & 7, 0);
        }

        static LandingProblem Clone(LandingProblem p)
        {
            return new LandingProblem {
                id=p.id, band=p.band, gridN=p.gridN, targetA=p.targetA, targetB=p.targetB,
                targetArea=p.targetArea, startB=p.startB, orientation=p.orientation,
                deltaAB=p.deltaAB,
                prompt=p.prompt, unitConcept=p.unitConcept, misconceptionId=p.misconceptionId
            };
        }

        public static List<MgfProblem> BridgeBank()
        {
            var source = BuildBank();
            var result = new List<MgfProblem>(source.Count);
            foreach (var p in source)
            {
                result.Add(new MgfProblem {
                    id = p.id,
                    prompt = p.prompt,
                    choices = null,
                    answer = AnswerText(p),
                    answerNumeric = p.AnswerNumeric,
                    unitConcept = p.unitConcept
                });
            }
            return result;
        }
    }
}
