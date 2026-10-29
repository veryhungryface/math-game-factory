// 조개 합창 — 원주각 문제 은행과 정수 판정 정본.
// 중학교 style_guide: 지시형 「구성하시오/고치시오」로 통일한다.
// expression_traps: 각은 70°처럼 쓰고, 호의 길이 비를 현의 길이 비로 바꾸지 않는다.
// 근호·삼각비·원과 비례에 관한 성질은 이 단원 문제에 만들지 않는다.
using System;
using System.Collections.Generic;
using Mgf;

namespace Mgf.JogaeHapchang
{
    public enum ShellProblemKind { Direct, ErrorFind, SplitReverse, ArcRatio }

    [Serializable]
    public sealed class ShellProblem
    {
        public string id;
        public ShellProblemKind kind;
        public int answerSlot;
        public int answerAngle;
        public int centerAngle;
        public int splitX;
        public int splitY;
        public int ratioM;
        public int ratioN;
        public int knownAngleN;
        public int knownAngleD = 1;
        public string wrongMark;
        public string prompt;
        public string answer;
        public string unitConcept;
        public int band;
    }

    public static class JogaeRules
    {
        public const int StartLives = 4;
        public const int TargetSolved = 8;
        public const int MasteryFirstCorrect = 6;
        public const int AllowedFirstMistakes = TargetSolved - MasteryFirstCorrect;
        public const float SessionSeconds = 90f;
        public const int SlotCount = 16;
        public const int MinAngle = 10;
        public const int StepAngle = 5;

        public static int AngleForSlot(int slot) => MinAngle + StepAngle * Math.Max(0, Math.Min(SlotCount - 1, slot));
        public static int SlotForAngle(int angle) => Math.Max(0, Math.Min(SlotCount - 1, (angle - MinAngle) / StepAngle));

        static int Gcd(int a, int b)
        {
            a = Math.Abs(a); b = Math.Abs(b);
            while (b != 0) { int t = a % b; a = b; b = t; }
            return Math.Max(1, a);
        }

        static string RationalText(int n, int d)
        {
            int g = Gcd(n, d); n /= g; d /= g;
            if (d == 1) return n + "°";
            if (d == 2 && n % 2 == 1) return (n / 2) + ".5°";
            return n + "/" + d + "°";
        }

        public static ShellProblem Direct(int slot)
        {
            int a = AngleForSlot(slot), c = a * 2;
            return new ShellProblem {
                id = "shell-direct-a" + a, kind = ShellProblemKind.Direct,
                answerSlot = slot, answerAngle = a, centerAngle = c, band = 1,
                prompt = "완성 조건: 호 AB에 대한 중심각을 " + c + "°로 만드시오. 원주각 ∠APB를 구성하시오.",
                answer = a + "°", unitConcept = "원주각은 같은 호에 대한 중심각의 1/2"
            };
        }

        public static ShellProblem ErrorFind(int slot, int variant)
        {
            int a = AngleForSlot(slot), c = a * 2;
            variant = ((variant % 3) + 3) % 3;
            string mark = variant == 0 ? c + "°" : variant == 1 ? RationalText(c, 4) : "90°";
            string tag = variant == 0 ? "same" : variant == 1 ? "quarter" : "diameter";
            string claim = variant == 0
                ? "이를 중심각과 같다고 보고 "
                : variant == 1
                    ? "이를 절반으로 두 번 나누어 "
                    : "이를 지름에 대한 각이라고 보고 ";
            return new ShellProblem {
                id = "shell-error-" + tag + "-a" + a, kind = ShellProblemKind.ErrorFind,
                answerSlot = slot, answerAngle = a, centerAngle = c, wrongMark = mark, band = 2,
                prompt = "중심각이 " + c + "°인데, " + claim + "원주각을 " + mark + "라고 잘못 적었다. ∠APB를 바르게 구성하시오.",
                answer = a + "°", unitConcept = "잘못 적힌 중심각과 원주각의 관계 고치기"
            };
        }

        public static ShellProblem Split(int slot, int x)
        {
            int a = AngleForSlot(slot), c = a * 2;
            x = Math.Max(5, Math.Min(c - 5, (x / 5) * 5));
            int y = c - x;
            return new ShellProblem {
                id = "shell-split-a" + a + "-x" + x, kind = ShellProblemKind.SplitReverse,
                answerSlot = slot, answerAngle = a, centerAngle = c, splitX = x, splitY = y, band = 3,
                prompt = "호 AB 위의 점 C에 대하여 ∠AOC=" + x + "°, ∠COB=" + y + "°이다. ∠APB를 구성하시오.",
                answer = a + "°", unitConcept = "인접 중심각의 합에서 원주각 역구성"
            };
        }

        public static ShellProblem ArcRatio(int slot, int m, int n)
        {
            int a = AngleForSlot(slot);
            int bN = a * n, bD = m;
            int g = Gcd(bN, bD); bN /= g; bD /= g;
            return new ShellProblem {
                id = "shell-ratio-" + m + "to" + n + "-a" + a, kind = ShellProblemKind.ArcRatio,
                answerSlot = slot, answerAngle = a, centerAngle = a * 2,
                ratioM = m, ratioN = n, knownAngleN = bN, knownAngleD = bD, band = 3,
                prompt = "한 원에서 호 AB의 길이:호 CD의 길이=" + m + ":" + n + "이고 ∠CPD=" + RationalText(bN, bD) + "이다. ∠APB를 구성하시오.",
                answer = a + "°", unitConcept = "한 원에서 호의 길이 비와 원주각의 크기 비"
            };
        }

        static MgfProblem ToMgf(ShellProblem p)
        {
            return new MgfProblem {
                id = p.id, prompt = p.prompt, choices = null,
                answer = p.answer, answerNumeric = p.answerAngle, unitConcept = p.unitConcept
            };
        }

        public static List<MgfProblem> BuildBank()
        {
            var bank = new List<MgfProblem>(389);
            for (int slot = 0; slot < SlotCount; slot++) bank.Add(ToMgf(Direct(slot)));
            for (int slot = 0; slot < SlotCount; slot++)
                for (int variant = 0; variant < 3; variant++) bank.Add(ToMgf(ErrorFind(slot, variant)));
            for (int slot = 0; slot < SlotCount; slot++)
            {
                int c = AngleForSlot(slot) * 2;
                for (int x = 5; x <= c - 5; x += 5) bank.Add(ToMgf(Split(slot, x)));
            }
            for (int slot = 0; slot < SlotCount; slot++)
            {
                int a = AngleForSlot(slot);
                bank.Add(ToMgf(ArcRatio(slot, 2, 1)));
                if (a <= 40) bank.Add(ToMgf(ArcRatio(slot, 1, 2)));
                if (a % 15 == 0) bank.Add(ToMgf(ArcRatio(slot, 3, 2)));
                if (a <= 50) bank.Add(ToMgf(ArcRatio(slot, 2, 3)));
            }
            ValidateBank(bank);
            return bank;
        }

        static void ValidateBank(List<MgfProblem> bank)
        {
            if (bank.Count != 389) throw new InvalidOperationException("원주각 은행은 389문항이어야 합니다: " + bank.Count);
            var ids = new HashSet<string>();
            var prompts = new HashSet<string>();
            for (int i = 0; i < bank.Count; i++)
            {
                var p = bank[i];
                if (!ids.Add(p.id)) throw new InvalidOperationException("중복 id: " + p.id);
                if (!prompts.Add(p.prompt + "|" + p.answer)) throw new InvalidOperationException("중복 발문: " + p.prompt);
                int a = (int)p.answerNumeric;
                if (a < 10 || a > 85 || a % 5 != 0 || p.answer != a + "°")
                    throw new InvalidOperationException("유효하지 않은 정답: " + p.id);
                // 오개념 역산값의 우연 정답 0건: c, c/4, 90°는 a와 다르다.
                int c = a * 2;
                if (c == a || c == a * 4 || a == 90) throw new InvalidOperationException("오개념 충돌: " + p.id);
                if (p.prompt.Contains("√")) throw new InvalidOperationException("이번 단원에 불필요한 근호: " + p.id);
            }
        }

        static int FirstSlot(int runSerial) => (((runSerial - 1) * 5 + 3) % SlotCount + SlotCount) % SlotCount;

        static int[] ValidRatios(int a)
        {
            // 21, 12, 32, 23을 각각 2:1, 1:2, 3:2, 2:3으로 인코딩한다.
            var list = new List<int>(4) { 21 };
            if (a <= 40) list.Add(12);
            if (a % 15 == 0) list.Add(32);
            if (a <= 50) list.Add(23);
            return list.ToArray();
        }

        public static List<ShellProblem> RunDeck(int runSerial)
        {
            var deck = new List<ShellProblem>(TargetSolved);
            int first = FirstSlot(runSerial);
            for (int round = 0; round < TargetSolved; round++)
            {
                int slot = (first + round * 7) % SlotCount; // 7과 16은 서로소라 한 판 안에서 답 슬롯이 겹치지 않는다.
                int a = AngleForSlot(slot);
                if (round < 3) deck.Add(Direct(slot));
                else if (round < 5) deck.Add(ErrorFind(slot, runSerial + round));
                else if (round == 5 || (round == 7 && (runSerial & 1) == 0))
                {
                    int count = (a * 2 - 5) / 5;
                    int x = 5 * (1 + Math.Abs(runSerial * 3 + round * 5) % count);
                    deck.Add(Split(slot, x));
                }
                else
                {
                    int[] ratios = ValidRatios(a);
                    int code = ratios[Math.Abs(runSerial + round * 3) % ratios.Length];
                    deck.Add(ArcRatio(slot, code / 10, code % 10));
                }
            }
            return deck;
        }

        public static string MisconceptionId(ShellProblem p, int submittedSlot)
        {
            int chosen = AngleForSlot(submittedSlot);
            if (chosen == p.centerAngle) return "same_as_center_angle";
            if (chosen * 4 == p.centerAngle) return "halved_twice";
            if (p.kind == ShellProblemKind.ArcRatio)
            {
                // 정답 a=b·m/n, 거꾸로 적용한 값은 b·n/m. 전부 정수 교차곱으로 판정한다.
                long left = (long)chosen * p.knownAngleD * p.ratioM;
                long right = (long)p.knownAngleN * p.ratioN;
                if (left == right) return "arc_ratio_reversed";
            }
            return "angle_relation_guess";
        }

        public static string MisconceptionMessage(string id)
        {
            switch (id)
            {
                case "same_as_center_angle": return "중심각을 그대로 썼습니다. 원주각은 그 절반입니다.";
                case "halved_twice": return "절반을 두 번 취했습니다. 중심각에서 한 번만 반으로 나누시오.";
                case "diameter_condition_missing": return "90°는 지름에 대한 원주각일 때입니다.";
                case "arc_ratio_reversed": return "호 AB:호 CD의 순서를 거꾸로 적용했습니다.";
                default: return "같은 호의 중심각은 원주각의 2배인지 확인하시오.";
            }
        }
    }
}
