using System;
using System.Collections.Generic;
using Mgf;

namespace Mgf.ByeolkanGwancheukso
{
    public enum ObservationKind { Union, Product, DiceSum, DigitCards, Representatives }
    public enum RackTopology { None, EndJoined, Crossed }
    public enum CellAction { None, KeepTargets, MaskForbidden, FoldAndMask }

    [Serializable]
    public sealed class ObservationProblem
    {
        public string id;
        public string prompt;
        public string concept;
        public string reveal;
        public ObservationKind kind;
        public RackTopology expectedTopology;
        public CellAction cellAction;
        public int rows;
        public int cols;
        public string[] rowLabels;
        public string[] colLabels;
        public bool[] expectedCells;
        public bool needsFold;
        public int answerCount;
        public string[] choices;
        public string[] distractorMisconceptionIds;

        public MgfProblem ToMgf()
        {
            return new MgfProblem {
                id = id,
                prompt = prompt,
                choices = choices,
                answer = answerCount + "가지",
                answerNumeric = answerCount,
                unitConcept = concept
            };
        }
    }

    public static class OutcomeGraphRules
    {
        public const int Goal = 6;
        public const int StartLives = 3;

        // 중2 표현 함정 정본을 생성기 옆에 둔다.
        // 1) 두 주사위는 반드시 "서로 다른 두 개"라고 써 (a,b)와 (b,a)를 구별한다.
        // 2) 합의 법칙의 "또는" 사건은 겹치지 않게 구성한다. 포함배제 공식을 내지 않는다.
        // 3) 숫자 카드는 각각 한 장이며 0은 십의 자리에 올 수 없고 같은 카드를 두 번 쓰지 않는다.
        // 4) 역할 없는 대표 2명은 순서 없는 쌍, 회장/부회장은 역할 있는 순서쌍이다.
        // 5) 발문은 중학교 교과서 관습인 "~의 경우의 수를 구하시오"를 쓴다.

        static readonly string[] FirstObjects = {
            "관측 렌즈", "유성 필터", "별자리 지도", "월식 기록지", "광학 프리즘",
            "천체 사진", "분광 카드", "궤도 표식", "초승달 판", "혜성 꼬리표",
            "구름 창", "행성 고리", "별빛 거울", "망원경 덮개", "관측 노트"
        };
        static readonly string[] SecondObjects = {
            "삼각대", "방향 표지", "시간 눈금", "돔 좌석", "추적 손잡이",
            "필름 통", "관측 창", "별빛 램프", "안개 판", "고도 눈금",
            "렌즈 받침", "기록 상자", "위치 표", "야간 조끼", "관측 깃"
        };
        static readonly string[] StationNames = {
            "북극성 우물", "푸른 분화구", "고리 협곡", "유리 절벽", "달그림자 층",
            "혜성 계단", "남빛 돔", "오팔 회랑", "은하 수문", "새벽 환",
            "운석 정원", "고요 관측환", "보랏빛 단층", "별비 경사로", "구름 아래층",
            "빙하 렌즈", "긴밤 수로", "초승달 탑", "은빛 홈", "자정 기록실"
        };

        public static ObservationProblem Practice()
        {
            return Product("practice-2x3", 2, 3, 0, true);
        }

        public static ObservationProblem Mission(int index, int runSerial)
        {
            bool unionFirst = (runSerial & 1) == 1;
            switch (index)
            {
                case 0: return unionFirst ? Union("mission-union", 3, 4, 1) : Product("mission-product", 3, 3, 2, false);
                case 1: return unionFirst ? Product("mission-product", 3, 3, 2, false) : Union("mission-union", 3, 4, 1);
                case 2: return Dice("mission-dice", 5, 3);
                case 3: return Digits("mission-digits-a", new[] { 0, 1, 2, 3 }, 4);
                case 4: return Digits("mission-digits-b", new[] { 0, 2, 4, 7 }, 5);
                default: return Representatives("mission-representatives", 5, 6);
            }
        }

        public static List<ObservationProblem> BuildCatalog()
        {
            var all = new List<ObservationProblem>(620);
            int serial = 0;

            // 제약을 만족하는 매개변수 풀을 먼저 구성한다. 무작위 생성 후 필터링하지 않는다.
            for (int v = 0; v < FirstObjects.Length; v++)
            for (int m = 2; m <= 6; m++)
            for (int n = 2; n <= 6; n++)
            {
                if (!(m == 2 && n == 2)) all.Add(Union("u-" + (++serial), m, n, v));
                if (!(m == 2 && n == 2)) all.Add(Product("p-" + (++serial), m, n, v, false));
            }

            for (int v = 0; v < StationNames.Length; v++)
            for (int sum = 3; sum <= 11; sum++) all.Add(Dice("d-" + (++serial), sum, v));

            for (int a = 1; a <= 6; a++)
            for (int b = a + 1; b <= 8; b++)
            for (int c = b + 1; c <= 9; c++)
            {
                int v = (a * 31 + b * 7 + c) % StationNames.Length;
                all.Add(Digits("c-" + (++serial), new[] { 0, a, b, c }, v));
            }

            for (int v = 0; v < StationNames.Length; v++)
            for (int n = 4; n <= 8; n++) all.Add(Representatives("r-" + (++serial), n, v));
            return all;
        }

        static ObservationProblem Base(string id, ObservationKind kind, RackTopology topology, CellAction action, int rows, int cols)
        {
            return new ObservationProblem {
                id = id,
                kind = kind,
                expectedTopology = topology,
                cellAction = action,
                rows = rows,
                cols = cols,
                rowLabels = new string[rows],
                colLabels = new string[cols],
                expectedCells = new bool[Math.Max(1, rows * cols)],
                distractorMisconceptionIds = new string[3]
            };
        }

        static ObservationProblem Union(string id, int m, int n, int variant)
        {
            var p = Base(id, ObservationKind.Union, RackTopology.EndJoined, CellAction.None, 1, Math.Min(10, m + n));
            string a = FirstObjects[variant % FirstObjects.Length], b = SecondObjects[variant % SecondObjects.Length];
            p.prompt = a + " " + m + "종류 또는 " + b + " " + n + "종류 중 하나만 고르는 경우의 수를 구하시오.";
            p.concept = "두 사건이 동시에 일어나지 않을 때의 합의 법칙";
            p.answerCount = m + n;
            p.reveal = m + "칸 + " + n + "칸 = " + p.answerCount + "가지";
            p.choices = Choices(p.answerCount, new[] { m * n, Math.Max(m, n), Math.Abs(m - n) }, p.distractorMisconceptionIds,
                new[] { "sum-product-swapped", "one-event-only", "subtract-events" });
            for (int i = 0; i < p.cols; i++) p.colLabels[i] = (i < m ? "A" + (i + 1) : "B" + (i - m + 1));
            return p;
        }

        static ObservationProblem Product(string id, int m, int n, int variant, bool practice)
        {
            var p = Base(id, ObservationKind.Product, RackTopology.Crossed, CellAction.None, m, n);
            string a = FirstObjects[variant % FirstObjects.Length], b = SecondObjects[variant % SecondObjects.Length];
            p.prompt = a + " " + m + "종류와 " + b + " " + n + "종류 중에서 각각 하나씩 고르는 경우의 수를 구하시오.";
            p.concept = "곱의 법칙";
            p.answerCount = m * n;
            p.reveal = m + "줄 × " + n + "칸 = " + p.answerCount + "가지";
            p.choices = Choices(p.answerCount, new[] { m + n, Math.Max(m, n), m * n - 1 }, p.distractorMisconceptionIds,
                new[] { "sum-product-swapped", "one-stage-only", "missed-one-pair" });
            for (int y = 0; y < m; y++) p.rowLabels[y] = "A" + (y + 1);
            for (int x = 0; x < n; x++) p.colLabels[x] = "B" + (x + 1);
            for (int i = 0; i < p.expectedCells.Length; i++) p.expectedCells[i] = true;
            return p;
        }

        static ObservationProblem Dice(string id, int sum, int variant)
        {
            var p = Base(id, ObservationKind.DiceSum, RackTopology.Crossed, CellAction.KeepTargets, 6, 6);
            p.prompt = StationNames[variant % StationNames.Length] + " 기록이다. 서로 다른 두 개의 주사위를 동시에 던질 때, 두 눈의 수의 합이 " + sum + "인 경우의 수를 구하시오.";
            p.concept = "서로 다른 두 주사위의 순서쌍";
            for (int y = 0; y < 6; y++) for (int x = 0; x < 6; x++)
            {
                int i = y * 6 + x;
                p.expectedCells[i] = y + x + 2 == sum;
            }
            p.answerCount = Count(p.expectedCells);
            p.reveal = "순서쌍 " + PairList(p) + " → " + p.answerCount + "가지";
            p.choices = Choices(p.answerCount, new[] { (p.answerCount + 1) / 2, sum, 36 }, p.distractorMisconceptionIds,
                new[] { "ordered-pairs-merged", "target-sum-used", "all-outcomes-used" });
            for (int i = 0; i < 6; i++) { p.rowLabels[i] = "첫 " + (i + 1); p.colLabels[i] = "둘 " + (i + 1); }
            return p;
        }

        static ObservationProblem Digits(string id, int[] digits, int variant)
        {
            int n = digits.Length;
            var p = Base(id, ObservationKind.DigitCards, RackTopology.Crossed, CellAction.MaskForbidden, n, n);
            p.prompt = StationNames[variant % StationNames.Length] + "에 " + Join(digits) + "이 각각 하나씩 적힌 카드가 있다. 이 중 2장을 뽑아 만들 수 있는 두 자리 자연수의 경우의 수를 구하시오.";
            p.concept = "0을 포함한 숫자 카드로 두 자리 자연수 만들기";
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                int i = y * n + x;
                p.rowLabels[y] = "십 " + digits[y]; p.colLabels[x] = "일 " + digits[x];
                p.expectedCells[i] = digits[y] == 0 || y == x; // true = 학생이 가려야 하는 금지 접점
            }
            p.answerCount = (n - 1) * (n - 1);
            p.reveal = "0인 십의 자리와 같은 카드 접점을 가리면 " + p.answerCount + "가지";
            p.choices = Choices(p.answerCount, new[] { n * (n - 1), n * n, (n - 1) * (n - 2) }, p.distractorMisconceptionIds,
                new[] { "leading-zero-allowed", "repeated-card-allowed", "zero-card-removed-entirely" });
            return p;
        }

        static ObservationProblem Representatives(string id, int n, int variant)
        {
            var p = Base(id, ObservationKind.Representatives, RackTopology.Crossed, CellAction.FoldAndMask, n, n);
            p.prompt = StationNames[variant % StationNames.Length] + "의 후보 " + n + "명 중에서 역할 구분 없이 대표 2명을 뽑는 경우의 수를 구하시오.";
            p.concept = "순서 없는 대표 2명 뽑기";
            p.needsFold = true;
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                int i = y * n + x;
                p.rowLabels[y] = "후보 " + (y + 1); p.colLabels[x] = "후보 " + (x + 1);
                p.expectedCells[i] = y == x; // 대각선은 같은 사람을 두 번 뽑으므로 마개 필요. 역순은 접기로 포갠다.
            }
            p.answerCount = n * (n - 1) / 2;
            p.reveal = "같은 사람은 가리고 역순을 포개면 " + p.answerCount + "가지";
            p.choices = Choices(p.answerCount, new[] { n * (n - 1), n * n, n }, p.distractorMisconceptionIds,
                new[] { "ordered-representatives", "same-person-allowed", "one-representative-only" });
            return p;
        }

        static string[] Choices(int correct, int[] raw, string[] outIds, string[] rawIds)
        {
            // 오답 후처리 순서: 정답 제거 → 범위 검사 → 중복 제거 → 부족분 보충 → 셔플(결정적 회전).
            var values = new List<int> { correct };
            var ids = new List<string> { "correct" };
            for (int i = 0; i < raw.Length; i++)
            {
                int v = raw[i];
                if (v <= 0 || v > 99 || v == correct || values.Contains(v)) continue;
                values.Add(v); ids.Add(rawIds[i]);
            }
            int bump = 1;
            while (values.Count < 4)
            {
                int v = correct + bump++;
                if (v <= 99 && !values.Contains(v)) { values.Add(v); ids.Add("near-count"); }
            }
            while (values.Count > 4) { values.RemoveAt(values.Count - 1); ids.RemoveAt(ids.Count - 1); }
            int rotate = correct % values.Count;
            var choices = new string[values.Count];
            for (int i = 0; i < values.Count; i++) choices[i] = values[(i + rotate) % values.Count] + "가지";
            int di = 0;
            for (int i = 0; i < ids.Count && di < outIds.Length; i++) if (ids[i] != "correct") outIds[di++] = ids[i];
            while (di < outIds.Length) outIds[di++] = "near-count";
            return choices;
        }

        public static bool Matches(ObservationProblem p, RackTopology a, RackTopology b, bool[] marked, bool folded)
        {
            if (a != p.expectedTopology || b != p.expectedTopology) return false;
            if (p.needsFold != folded) return false;
            if (p.cellAction == CellAction.None) return true;
            if (marked == null || marked.Length < p.expectedCells.Length) return false;
            for (int i = 0; i < p.expectedCells.Length; i++) if (marked[i] != p.expectedCells[i]) return false;
            return true;
        }

        public static string Diagnose(ObservationProblem p, RackTopology a, RackTopology b, bool[] marked, bool folded)
        {
            if (a != p.expectedTopology || b != p.expectedTopology) return "sum-product-swapped";
            if (p.needsFold && !folded) return "ordered-representatives";
            if (p.cellAction == CellAction.MaskForbidden)
            {
                for (int i = 0; i < p.expectedCells.Length; i++)
                    if (p.expectedCells[i] && !marked[i]) return i < p.cols ? "leading-zero-allowed" : "repeated-card-allowed";
            }
            if (p.cellAction == CellAction.FoldAndMask) return "same-person-allowed";
            if (p.cellAction == CellAction.KeepTargets) return "ordered-pair-set-mismatch";
            return "incomplete-or-extra";
        }

        static int Count(bool[] a) { int n = 0; for (int i = 0; i < a.Length; i++) if (a[i]) n++; return n; }
        static string Join(int[] a)
        {
            string s = "";
            for (int i = 0; i < a.Length; i++) s += (i == 0 ? "" : i == a.Length - 1 ? ", " : ", ") + a[i];
            return s;
        }
        static string PairList(ObservationProblem p)
        {
            string s = ""; int shown = 0;
            for (int y = 0; y < p.rows; y++) for (int x = 0; x < p.cols; x++) if (p.expectedCells[y * p.cols + x])
            {
                if (shown++ > 0) s += ", ";
                s += "(" + (y + 1) + "," + (x + 1) + ")";
            }
            return s;
        }
    }
}
