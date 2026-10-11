// 연 띄워 — 정수 벡터로만 계산하는 대각선 판정과 제약 만족 묶음 생성기.
// 중학교 표현 함정(2022-middle-math.json): 사각형의 포함 관계를 정의대로 판정한다.
// 정사각형은 직사각형·마름모·평행사변형에도 포함되며, 증명 발문에 '가정/결론'을 쓰지 않는다.
using System;
using System.Collections.Generic;

namespace Mgf.YeonTtwiwo
{
    public enum FrameGoal { Parallelogram, Rectangle, Rhombus, Square }

    [Serializable]
    public struct IVec2
    {
        public int x;
        public int y;
        public IVec2(int px, int py) { x = px; y = py; }
        public int Sqr => x * x + y * y;
        public int Length
        {
            get
            {
                int sqr = Sqr;
                for (int n = 0; n * n <= sqr; n++)
                    if (n * n == sqr) return n;
                throw new InvalidOperationException("정수 길이가 아닌 대각선 벡터: " + x + "," + y);
            }
        }
        public IVec2 Rotate(int quarter)
        {
            quarter = ((quarter % 4) + 4) % 4;
            if (quarter == 1) return new IVec2(-y, x);
            if (quarter == 2) return new IVec2(-x, -y);
            if (quarter == 3) return new IVec2(y, -x);
            return this;
        }
        public static int Dot(IVec2 a, IVec2 b) => a.x * b.x + a.y * b.y;
        public static int Cross(IVec2 a, IVec2 b) => a.x * b.y - a.y * b.x;
    }

    [Serializable]
    public sealed class DockSpec
    {
        public int id;
        public int ux;
        public int uy;
        public int uLength;
        public FrameGoal goal;
        public IVec2 U => new IVec2(ux, uy);
    }

    [Serializable]
    public sealed class CartridgeSpec
    {
        public string id;
        public int vx;
        public int vy;
        public int p;
        public int q;
        public string misconceptionId;
        public IVec2 V => new IVec2(vx, vy);
        // 화면 라벨, 문제 은행, 판정이 모두 같은 정수 벡터의 실제 길이를 쓴다.
        // 일반 문항은 |V|=5이고 연습 문항은 |V|=1이므로 고정 배율 5를 쓰면 안 된다.
        public int BOLength => p * V.Length;
        public int DOLength => q * V.Length;
    }

    [Serializable]
    public sealed class PlacementSpec
    {
        public int dock;
        public int cartridge;
        public int rotation;
        public PlacementSpec(int d, int c, int r) { dock = d; cartridge = c; rotation = r; }
        public string Key => cartridge + "-" + dock + "-" + rotation;
    }

    [Serializable]
    public sealed class BundleSpec
    {
        public string id;
        public int band;
        public DockSpec[] docks;
        public CartridgeSpec[] cartridges;
        public PlacementSpec[] accepted;
        public int legalCount;
        public int acceptedCount;
        public double chanceBaseline;
        public string prompt;
        public string concept;
        public int preferredDock;
        public FrameGoal preferredGoal;
    }

    [Serializable]
    public sealed class BankDock
    {
        public int id;
        public string target;
        public int ux;
        public int uy;
        public int ao;
        public int co;
    }

    [Serializable]
    public sealed class BankCartridge
    {
        public string id;
        public int vx;
        public int vy;
        public int bo;
        public int dO;
        public string misconceptionId;
    }

    [Serializable]
    public sealed class YeonBankItem
    {
        public string id;
        public string prompt;
        public string[] choices;
        public string answer;
        public double answerNumeric;
        public string unitConcept;
        public BankDock[] docks;
        public BankCartridge[] cartridges;
        public int legalCount;
        public int acceptedCount;
        public string[] acceptedPlacements;
        public double chanceBaseline;
    }

    [Serializable]
    public sealed class YeonBankWrap { public YeonBankItem[] items; }

    [Serializable]
    public sealed class BotAudit
    {
        public string policy;
        public int games;
        public int firstAttempts;
        public int firstCorrect;
        public double firstRate;
        public double chanceBaseline;
        public int completions;
    }

    public static class YeonRules
    {
        public const int BoardCount = 9;
        public const int CartCount = 3;
        public const int RotationCount = 4;
        public const int StartLives = 3;
        public const int TargetKites = 12;
        public const int RequiredFirstCorrect = 9;

        static readonly int[] Lengths = { 10, 15, 20 };
        static readonly IVec2[] DiagonalFive = {
            new IVec2(3, 4), new IVec2(4, 3), new IVec2(-3, 4), new IVec2(-4, 3)
        };

        public static string GoalName(FrameGoal goal)
        {
            switch (goal)
            {
                case FrameGoal.Rectangle: return "직사각형";
                case FrameGoal.Rhombus: return "마름모";
                case FrameGoal.Square: return "정사각형";
                default: return "평행사변형";
            }
        }

        public static bool Bisects(CartridgeSpec c) => c.p == c.q;
        public static bool EqualDiagonals(DockSpec d, CartridgeSpec c)
        {
            long ac2 = 4L * d.U.Sqr;
            long pq = c.p + c.q;
            long bd2 = pq * pq * c.V.Sqr;
            return ac2 == bd2;
        }
        public static bool Perpendicular(DockSpec d, CartridgeSpec c, int rotation)
            => IVec2.Dot(d.U, c.V.Rotate(rotation)) == 0;
        public static bool NonDegenerate(DockSpec d, CartridgeSpec c, int rotation)
            => IVec2.Cross(d.U, c.V.Rotate(rotation)) != 0;

        // 학생이 놓은 카트리지·연틀·회전을 그대로 판정한다. 회전/카트리지를 자동 보정하지 않는다.
        public static bool IsAccepted(DockSpec d, CartridgeSpec c, int rotation)
        {
            bool p = Bisects(c);
            bool equal = EqualDiagonals(d, c);
            bool perp = Perpendicular(d, c, rotation);
            if (!NonDegenerate(d, c, rotation)) return false;
            switch (d.goal)
            {
                case FrameGoal.Rectangle: return p && equal;
                case FrameGoal.Rhombus: return p && perp;
                case FrameGoal.Square: return p && equal && perp;
                default: return p;
            }
        }

        public static string ClassifyWrong(DockSpec d, CartridgeSpec c, int rotation)
        {
            bool p = Bisects(c);
            bool equal = EqualDiagonals(d, c);
            bool perp = Perpendicular(d, c, rotation);
            if (!NonDegenerate(d, c, rotation)) return "degenerate_diagonals";
            if (perp && !p) return "perpendicular_without_bisect";
            if (equal && !p) return "equal_without_bisect";
            if (p && !equal && d.goal == FrameGoal.Rectangle) return "parallelogram_diagonals_always_equal";
            if (p && !perp && d.goal == FrameGoal.Rhombus) return "bisect_without_perpendicular";
            if (d.goal == FrameGoal.Square && p && (equal || perp)) return "square_missing_one_condition";
            return "conditions_not_satisfied";
        }

        public static string WrongExplanation(DockSpec d, CartridgeSpec c, int rotation)
        {
            bool p = Bisects(c);
            bool equal = EqualDiagonals(d, c);
            bool perp = Perpendicular(d, c, rotation);
            // 외적 0은 수직 여부나 길이보다 먼저 설명한다. 두 대각선이 같은 직선 위에
            // 놓이면 애초에 사각형이 만들어지지 않으므로 다른 성질의 일부가 우연히
            // 맞더라도 학생에게 유효한 사각형처럼 말하면 안 된다.
            if (!NonDegenerate(d, c, rotation))
                return "두 대각선이 한 직선에 놓여 사각형이 만들어지지 않는다";
            if (!p) return "BO와 DO가 달라 서로 다른 것을 이등분하지 않는다";
            if (d.goal == FrameGoal.Rectangle && !equal) return "서로 이등분하지만 AC와 BD의 길이가 다르다";
            if (d.goal == FrameGoal.Rhombus && !perp) return "서로 이등분하지만 두 대각선이 수직이 아니다";
            if (d.goal == FrameGoal.Square && !equal && !perp)
                return "AC와 BD의 길이가 다르고 두 대각선도 수직이 아니다";
            if (d.goal == FrameGoal.Square && !equal)
                return "두 대각선은 수직이지만 AC와 BD의 길이가 다르다";
            if (d.goal == FrameGoal.Square && !perp)
                return "AC와 BD의 길이는 같지만 두 대각선이 수직이 아니다";
            return "목표 사각형의 대각선 조건을 만족하지 않는다";
        }

        public static int AcSquared(DockSpec d) => 4 * d.U.Sqr;
        public static int BdSquared(CartridgeSpec c)
        {
            int sum = c.p + c.q;
            return sum * sum * c.V.Sqr;
        }
        public static int DotValue(DockSpec d, CartridgeSpec c, int rotation)
            => IVec2.Dot(d.U, c.V.Rotate(rotation));

        public static BundleSpec BuildPractice()
        {
            var docks = new DockSpec[BoardCount];
            for (int i = 0; i < BoardCount; i++)
                docks[i] = Dock(i, i == 4 ? 3 : 5 + (i % 3), i % 2 == 0, i == 4 ? FrameGoal.Parallelogram : FrameGoal.Rectangle);
            docks[4].ux = 3; docks[4].uy = 0; docks[4].uLength = 3;
            var carts = new[] {
                new CartridgeSpec { id="연습 살대", vx=1, vy=0, p=4, q=4, misconceptionId="practice-correct" },
                new CartridgeSpec { id="잠긴 살대", vx=1, vy=0, p=2, q=5, misconceptionId="perpendicular_without_bisect" },
                new CartridgeSpec { id="잠긴 살대", vx=1, vy=0, p=1, q=6, misconceptionId="equal_without_bisect" }
            };
            return FinalizeBundle("practice-fixed", 0, docks, carts, 1 << 4, 4, FrameGoal.Parallelogram,
                "평행사변형이 되도록 연습 살대를 90° 돌려 가운데 연틀에 꽂으시오.", "평행사변형의 대각선");
        }

        public static BundleSpec BuildBundle(int seed, int band, int emptyMask, int preferredDock)
        {
            int emptyCount = CountBits(emptyMask);
            if (emptyCount < 3) return null;
            if ((emptyMask & (1 << preferredDock)) == 0) preferredDock = FirstEmpty(emptyMask);
            band = Math.Max(1, Math.Min(3, band));
            FrameGoal goal = band == 1 ? FrameGoal.Parallelogram
                : band == 2 ? ((seed & 1) == 0 ? FrameGoal.Rectangle : FrameGoal.Rhombus)
                : FrameGoal.Square;

            // P/직사각형은 회전 네 방향이 모두 성립할 수 있어 빈 칸이 3개면 우연 상한을 넘는다.
            if (emptyCount == 3 && (goal == FrameGoal.Parallelogram || goal == FrameGoal.Rectangle))
                goal = band == 1 ? FrameGoal.Rhombus : FrameGoal.Square;

            int preferredLength = Lengths[Math.Abs(seed + preferredDock * 7) % Lengths.Length];
            bool preferredHorizontal = ((seed / 3 + preferredDock) & 1) == 0;
            DockSpec target = Dock(preferredDock, preferredLength, preferredHorizontal, goal);
            int correctRotation = Math.Abs(seed * 7 + preferredDock) % 4;
            CartridgeSpec solution = BuildSolution(seed, target, correctRotation);

            // 한 묶음에는 의도한 목표 연틀만 정답이 되게 만든다. 다른 연틀은 정답 살대의
            // 반대각선 길이와 다른 직사각형/정사각형으로 역생성해, 빈 칸 수가 줄어도
            // accepted/legal 우연 확률이 1/12를 넘지 않는다.
            int solutionHalfLength = solution.p * 5;
            var docks = new DockSpec[BoardCount];
            for (int i = 0; i < BoardCount; i++)
            {
                int lengthIndex = Math.Abs(seed * 5 + i * 11) % Lengths.Length;
                int len = Lengths[lengthIndex];
                if (len == solutionHalfLength) len = Lengths[(lengthIndex + 1) % Lengths.Length];
                if (len == solutionHalfLength) len = Lengths[(lengthIndex + 2) % Lengths.Length];
                bool horizontal = ((seed + i * 3) & 1) == 0;
                FrameGoal other = ((seed + i) & 1) == 0 ? FrameGoal.Rectangle : FrameGoal.Square;
                docks[i] = Dock(i, len, horizontal, other);
            }
            docks[preferredDock] = target;

            CartridgeSpec perpendicularTrap = BuildPerpendicularTrap(seed, target, correctRotation);
            CartridgeSpec equalTrap = BuildEqualTrap(seed, target);
            var carts = new[] { solution, perpendicularTrap, equalTrap };
            ShuffleCarts(carts, seed);

            string prompt = BuildPrompt(goal, docks, carts, preferredDock);
            string concept = goal == FrameGoal.Parallelogram ? "평행사변형의 대각선"
                : goal == FrameGoal.Rectangle ? "직사각형의 대각선"
                : goal == FrameGoal.Rhombus ? "마름모의 대각선"
                : "정사각형의 대각선과 사각형의 포함 관계";
            var bundle = FinalizeBundle("bundle-" + seed + "-b" + band, band, docks, carts, emptyMask,
                preferredDock, goal, prompt, concept);

            if (bundle.acceptedCount < 1 || bundle.legalCount < 36 || bundle.chanceBaseline > 1.0 / 12.0 + 1e-12)
                throw new InvalidOperationException("제약을 만족하지 않는 살대 묶음: " + bundle.id + " " + bundle.acceptedCount + "/" + bundle.legalCount);
            return bundle;
        }

        static DockSpec Dock(int id, int length, bool horizontal, FrameGoal goal)
        {
            return new DockSpec {
                id = id, uLength = length, goal = goal,
                ux = horizontal ? length : 0,
                uy = horizontal ? 0 : length
            };
        }

        static CartridgeSpec BuildSolution(int seed, DockSpec target, int correctRotation)
        {
            int p;
            IVec2 baseV;
            if (target.goal == FrameGoal.Parallelogram)
            {
                p = 1 + Math.Abs(seed) % 3;
                if (p * 5 == target.uLength) p = p % 3 + 1;
                baseV = DiagonalFive[Math.Abs(seed / 3) % DiagonalFive.Length];
            }
            else if (target.goal == FrameGoal.Rectangle)
            {
                p = target.uLength / 5;
                baseV = DiagonalFive[Math.Abs(seed / 5) % DiagonalFive.Length];
            }
            else
            {
                p = target.goal == FrameGoal.Square ? target.uLength / 5 : 1 + Math.Abs(seed) % 3;
                if (target.goal == FrameGoal.Rhombus && p * 5 == target.uLength) p = p % 3 + 1;
                IVec2 perpendicular = target.ux != 0 ? new IVec2(0, 5) : new IVec2(5, 0);
                baseV = perpendicular.Rotate(-correctRotation);
            }
            return new CartridgeSpec {
                id = "살대 " + (char)('A' + Math.Abs(seed) % 7),
                vx = baseV.x, vy = baseV.y, p = p, q = p,
                misconceptionId = "correct_bisect_first"
            };
        }

        static CartridgeSpec BuildPerpendicularTrap(int seed, DockSpec target, int correctRotation)
        {
            IVec2 perpendicular = target.ux != 0 ? new IVec2(0, 5) : new IVec2(5, 0);
            IVec2 baseV = perpendicular.Rotate(-correctRotation);
            int p = 1 + Math.Abs(seed) % 2;
            int q = p + 1 + Math.Abs(seed / 7) % 2;
            return new CartridgeSpec {
                id = "살대 " + (char)('H' + Math.Abs(seed) % 7),
                vx = baseV.x, vy = baseV.y, p = p, q = q,
                misconceptionId = "perpendicular_without_bisect"
            };
        }

        static CartridgeSpec BuildEqualTrap(int seed, DockSpec target)
        {
            int sum = 2 * target.uLength / 5;
            int p = 1 + Math.Abs(seed / 11) % Math.Max(1, sum - 2);
            int q = sum - p;
            if (p == q) { p = 1; q = sum - 1; }
            IVec2 v = DiagonalFive[Math.Abs(seed / 13 + 1) % DiagonalFive.Length];
            return new CartridgeSpec {
                id = "살대 " + (char)('P' + Math.Abs(seed) % 7),
                vx = v.x, vy = v.y, p = p, q = q,
                misconceptionId = "equal_without_bisect"
            };
        }

        static void ShuffleCarts(CartridgeSpec[] carts, int seed)
        {
            var rng = new Random(unchecked(seed * 104729 + 17));
            for (int i = carts.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                CartridgeSpec t = carts[i]; carts[i] = carts[j]; carts[j] = t;
            }
        }

        static BundleSpec FinalizeBundle(string id, int band, DockSpec[] docks, CartridgeSpec[] carts,
            int emptyMask, int preferredDock, FrameGoal goal, string prompt, string concept)
        {
            var accepted = new List<PlacementSpec>();
            int empties = CountBits(emptyMask);
            for (int d = 0; d < docks.Length; d++) if ((emptyMask & (1 << d)) != 0)
            for (int c = 0; c < carts.Length; c++)
            for (int r = 0; r < RotationCount; r++)
                if (IsAccepted(docks[d], carts[c], r)) accepted.Add(new PlacementSpec(d, c, r));
            int legal = empties * carts.Length * RotationCount;
            return new BundleSpec {
                id = id, band = band, docks = docks, cartridges = carts, accepted = accepted.ToArray(),
                legalCount = legal, acceptedCount = accepted.Count,
                chanceBaseline = legal == 0 ? 0.0 : (double)accepted.Count / legal,
                prompt = prompt, concept = concept, preferredDock = preferredDock, preferredGoal = goal
            };
        }

        static string BuildPrompt(FrameGoal goal, DockSpec[] docks, CartridgeSpec[] carts, int preferredDock)
        {
            DockSpec target = docks[preferredDock];
            int row = preferredDock / 3 + 1, col = preferredDock % 3 + 1;
            string instruction = "두 대각선의 교점을 O라 할 때, " + row + "행 " + col + "열 "
                + GoalName(goal) + " 연틀은 AO=CO=" + target.uLength + " cm이고 AC는 "
                + (target.ux != 0 ? "가로" : "세로") + "이다. 알맞은 살대를 돌려 꽂으시오. ";
            // 문항 다양성은 일련번호가 아니라 실제 목표 위치·AC 측정값·살대 이름과 BO/DO 측정값에서 나온다.
            string bd = "";
            for (int i = 0; i < carts.Length; i++)
                bd += (i == 0 ? "" : " / ") + carts[i].id + ": BO=" + carts[i].BOLength
                    + " cm, DO=" + carts[i].DOLength + " cm";
            return instruction + bd + ".";
        }

        public static List<YeonBankItem> BuildBank(int count = 360)
        {
            var items = new List<YeonBankItem>(count);
            int full = (1 << BoardCount) - 1;
            for (int i = 0; i < count; i++)
            {
                int band = i % 12 < 4 ? 1 : i % 12 < 10 ? 2 : 3;
                BundleSpec b = BuildBundle(20011 + i * 7919, band, full, (i * 5 + i / 7) % BoardCount);
                PlacementSpec a = b.accepted[0];
                var docks = new BankDock[BoardCount];
                for (int d = 0; d < BoardCount; d++) docks[d] = new BankDock {
                    id = d, target = GoalName(b.docks[d].goal), ux = b.docks[d].ux, uy = b.docks[d].uy,
                    ao = b.docks[d].uLength, co = b.docks[d].uLength
                };
                var carts = new BankCartridge[CartCount];
                for (int c = 0; c < CartCount; c++) carts[c] = new BankCartridge {
                    id = b.cartridges[c].id, vx = b.cartridges[c].vx, vy = b.cartridges[c].vy,
                    bo = b.cartridges[c].BOLength, dO = b.cartridges[c].DOLength,
                    misconceptionId = b.cartridges[c].misconceptionId
                };
                string[] accepted = new string[b.accepted.Length];
                for (int k = 0; k < accepted.Length; k++) accepted[k] = b.accepted[k].Key;
                items.Add(new YeonBankItem {
                    id = "yeon-" + (i + 1), prompt = b.prompt, choices = null,
                    answer = a.Key, answerNumeric = a.dock * 100 + a.cartridge * 10 + a.rotation,
                    unitConcept = b.concept, docks = docks, cartridges = carts,
                    legalCount = b.legalCount, acceptedCount = b.acceptedCount,
                    acceptedPlacements = accepted, chanceBaseline = b.chanceBaseline
                });
            }
            return items;
        }

        public static int ValidateAll()
        {
            List<YeonBankItem> bank = BuildBank(360);
            var ids = new HashSet<string>();
            var prompts = new HashSet<string>();
            for (int i = 0; i < bank.Count; i++)
            {
                YeonBankItem p = bank[i];
                if (!ids.Add(p.id)) throw new InvalidOperationException("문항 id 중복: " + p.id);
                prompts.Add(p.prompt);
                if (p.legalCount < 36 || p.acceptedCount < 1 || p.chanceBaseline > 1.0 / 12.0 + 1e-12)
                    throw new InvalidOperationException("문항 우연 상한 위반: " + p.id);
                int misconceptionCount = 0;
                for (int c = 0; c < p.cartridges.Length; c++)
                    if (p.cartridges[c].misconceptionId == "perpendicular_without_bisect" ||
                        p.cartridges[c].misconceptionId == "equal_without_bisect") misconceptionCount++;
                if (misconceptionCount < 2) throw new InvalidOperationException("오개념 역산 후보 부족: " + p.id);
            }
            if (prompts.Count < 300)
                throw new InvalidOperationException("서로 다른 발문 300개 미만: " + prompts.Count);

            // 전체 파라미터 풀 × 오개념 조합에서 우연히 정답이 되는 경우가 0인지 다시 직접 검사한다.
            int full = (1 << BoardCount) - 1;
            int wrongExplanationChecks = 0;
            for (int i = 0; i < 480; i++)
            {
                BundleSpec b = BuildBundle(90001 + i * 3571, 1 + i % 3, full, i % BoardCount);
                DockSpec target = b.docks[b.preferredDock];
                for (int c = 0; c < b.cartridges.Length; c++)
                {
                    CartridgeSpec cart = b.cartridges[c];
                    if (cart.misconceptionId == "perpendicular_without_bisect" || cart.misconceptionId == "equal_without_bisect")
                        for (int r = 0; r < 4; r++)
                            if (IsAccepted(target, cart, r))
                                throw new InvalidOperationException("오개념 카트리지가 정답과 일치: " + cart.misconceptionId);
                }

                // 실제 화면의 모든 오답 설명도 같은 정수 판정기에서 전수 검증한다.
                for (int d = 0; d < b.docks.Length; d++)
                for (int c = 0; c < b.cartridges.Length; c++)
                for (int r = 0; r < RotationCount; r++)
                {
                    DockSpec dock = b.docks[d];
                    CartridgeSpec cart = b.cartridges[c];
                    if (IsAccepted(dock, cart, r)) continue;
                    string explanation = WrongExplanation(dock, cart, r);
                    ValidateWrongExplanation(dock, cart, r, explanation);
                    wrongExplanationChecks++;
                }
            }
            return wrongExplanationChecks;
        }

        static void ValidateWrongExplanation(DockSpec d, CartridgeSpec c, int rotation, string explanation)
        {
            bool p = Bisects(c);
            bool equal = EqualDiagonals(d, c);
            bool perp = Perpendicular(d, c, rotation);
            bool nonDegenerate = NonDegenerate(d, c, rotation);
            string expected;
            if (!nonDegenerate) expected = "두 대각선이 한 직선에 놓여 사각형이 만들어지지 않는다";
            else if (!p) expected = "BO와 DO가 달라 서로 다른 것을 이등분하지 않는다";
            else if (d.goal == FrameGoal.Rectangle && !equal) expected = "서로 이등분하지만 AC와 BD의 길이가 다르다";
            else if (d.goal == FrameGoal.Rhombus && !perp) expected = "서로 이등분하지만 두 대각선이 수직이 아니다";
            else if (d.goal == FrameGoal.Square && !equal && !perp) expected = "AC와 BD의 길이가 다르고 두 대각선도 수직이 아니다";
            else if (d.goal == FrameGoal.Square && !equal) expected = "두 대각선은 수직이지만 AC와 BD의 길이가 다르다";
            else if (d.goal == FrameGoal.Square && !perp) expected = "AC와 BD의 길이는 같지만 두 대각선이 수직이 아니다";
            else expected = "목표 사각형의 대각선 조건을 만족하지 않는다";
            if (!string.Equals(explanation, expected, StringComparison.Ordinal))
                throw new InvalidOperationException("오답 설명 진릿값 불일치: " + explanation + " / " + expected);
        }

        public static BotAudit[] RunBotAudit(int games = 200)
        {
            string[] names = { "repeat", "cycle", "random", "idle" };
            var result = new BotAudit[names.Length];
            int full = (1 << BoardCount) - 1;
            for (int policy = 0; policy < names.Length; policy++)
            {
                int correct = 0, attempts = 0, completions = 0, baselineBundles = 0;
                double chanceSum = 0;
                var rng = new Random(55109 + policy * 13007);
                for (int g = 0; g < games; g++)
                {
                    int streak = 0, lives = StartLives;
                    for (int q = 0; q < 12 && lives > 0; q++)
                    {
                        BundleSpec b = BuildBundle(300001 + g * 1217 + q * 7919, 1 + Math.Min(2, q / 4), full, (g + q * 2) % BoardCount);
                        chanceSum += b.chanceBaseline;
                        baselineBundles++;
                        if (policy == 3) continue;
                        int candidate = policy == 0 ? 0 : policy == 1 ? (q * 13 + g) % b.legalCount : rng.Next(b.legalCount);
                        int rotation = candidate % 4;
                        candidate /= 4;
                        int cart = candidate % 3;
                        int dock = candidate / 3;
                        bool hit = IsAccepted(b.docks[dock], b.cartridges[cart], rotation);
                        attempts++;
                        if (hit) { correct++; streak++; }
                        else { lives--; }
                    }
                    if (streak >= 12) completions++;
                }
                result[policy] = new BotAudit {
                    policy = names[policy], games = games, firstAttempts = attempts, firstCorrect = correct,
                    firstRate = attempts == 0 ? 0 : (double)correct / attempts,
                    chanceBaseline = policy == 3 ? 0 : chanceSum / Math.Max(1, baselineBundles),
                    completions = completions
                };
            }
            return result;
        }

        public static int CountBits(int mask)
        {
            int n = 0; while (mask != 0) { n += mask & 1; mask >>= 1; } return n;
        }
        public static int FirstEmpty(int mask)
        {
            for (int i = 0; i < BoardCount; i++) if ((mask & (1 << i)) != 0) return i;
            return -1;
        }
    }
}
