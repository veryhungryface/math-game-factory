using System;
using System.Collections.Generic;
using Mgf;

namespace Mgf.DiceCaravan
{
    enum BoardKind { NumberCards, PairGrid, DiceGrid, DigitCards, Representatives }

    sealed class CaravanProblem
    {
        public string id = "";
        public string prompt = "";
        public string concept = "";
        public string reveal = "";
        public string misconceptionId = "";
        public BoardKind kind;
        public int rows;
        public int cols;
        public string[] rowLabels = Array.Empty<string>();
        public string[] colLabels = Array.Empty<string>();
        public string[] cellLabels = Array.Empty<string>();
        public bool[] answerSet = Array.Empty<bool>();
        public int[] misconceptionCounts = Array.Empty<int>();
        public int AnswerCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < answerSet.Length; i++) if (answerSet[i]) n++;
                return n;
            }
        }

        public MgfProblem ToMgf()
        {
            int correct = AnswerCount;
            var candidates = new List<int>();
            // expression_traps: 두 주사위는 반드시 '서로 다른'을 밝히고 순서쌍으로 센다.
            // 합의 법칙은 서로 배반인 사건만 생성하며, 카드의 0은 십의 자리에 올 수 없다.
            // Each factory method supplies at least two values tied to that
            // problem's actual misconception.  Keep them ahead of generic
            // +/-1 fillers so the exported QA choices retain diagnostic value.
            for (int i = 0; i < misconceptionCounts.Length; i++) candidates.Add(misconceptionCounts[i]);
            candidates.Add(Math.Max(1, rows + cols)); // add-instead-of-multiply
            candidates.Add(Math.Max(1, rows * cols)); // multiply-instead-of-add / all-cells
            candidates.Add(Math.Max(1, correct - 1));
            candidates.Add(correct + 1);

            var values = new List<int> { correct };
            for (int i = 0; i < candidates.Count && values.Count < 4; i++)
            {
                int v = candidates[i];
                if (v <= 0 || v > 36 || v == correct || values.Contains(v)) continue;
                values.Add(v);
            }
            for (int v = 1; values.Count < 4; v++) if (v != correct && !values.Contains(v)) values.Add(v);
            // Deterministic shuffle keeps the bank reproducible while preventing answer-position leakage.
            int rot = Math.Abs(id.GetHashCode()) % values.Count;
            var choices = new string[values.Count];
            for (int i = 0; i < values.Count; i++) choices[i] = values[(i + rot) % values.Count] + "가지";
            return new MgfProblem
            {
                id = id,
                prompt = prompt,
                choices = choices,
                answer = correct + "가지",
                answerNumeric = correct,
                unitConcept = concept
            };
        }
    }

    static class DiceCaravanRules
    {
        public const int Goal = 6;
        public const int StartLives = 3;

        static readonly string[] RouteNames = {
            "서리 언덕", "은종 야영지", "북극성 천막", "흰여우 능선", "초승달 우물", "유리 협로",
            "고요한 부두", "푸른 돛터", "바람종 고개", "별가루 정박지", "긴그림자 벌판", "남빛 등대",
            "소금꽃 평원", "새벽 나루", "쌍둥이 봉화", "달무리 관측소", "은빛 수로", "혜성 마루",
            "겨울별 길목", "나침반 언덕", "하현달 쉼터", "유백색 모래톱"
        };

        public static CaravanProblem Practice()
        {
            return DiceSum("practice-sum5", 5, true);
        }

        public static List<CaravanProblem> BuildCatalog()
        {
            var all = new List<CaravanProblem>(420);
            int serial = 0;

            // Band 1: one event and disjoint unions. Parameters generate the valid pool directly.
            for (int n = 12; n <= 18; n++)
            for (int a = 3; a <= 6; a++)
            {
                if (n / a >= 3) all.Add(NumberMultiples("num-" + (++serial), n, a));
                for (int b = a + 1; b <= 9; b++)
                {
                    if (Lcm(a, b) <= n) continue; // construct only disjoint events; no post-hoc random filtering.
                    all.Add(NumberUnion("union-" + (++serial), n, a, b));
                }
            }

            // Band 2: products and ordered dice outcomes.
            for (int r = 2; r <= 5; r++)
            for (int c = 2; c <= 6; c++)
            for (int variant = 0; variant < 4; variant++) all.Add(ProductGrid("product-" + (++serial), r, c, variant));
            for (int sum = 4; sum <= 10; sum++)
            for (int variant = 0; variant < 10; variant++) all.Add(DiceSum("dice-sum-" + (++serial), sum, false, variant));
            for (int product = 2; product <= 24; product++)
            {
                var p = DiceProduct("dice-product-" + (++serial), product);
                if (p.AnswerCount >= 3 && p.AnswerCount <= 10) all.Add(p);
            }

            // Band 3: two-digit numbers and role/no-role representatives.
            int[][] digitPools = {
                new[]{0,1,2,3}, new[]{0,1,2,4}, new[]{0,1,3,5}, new[]{0,2,3,6},
                new[]{0,2,4,7}, new[]{0,3,5,8}, new[]{0,4,6,9}, new[]{0,1,5,9}
            };
            for (int p = 0; p < digitPools.Length; p++)
            for (int variant = 0; variant < 8; variant++) all.Add(DigitCards("digits-" + (++serial), digitPools[p], variant));
            for (int n = 4; n <= 6; n++)
            for (int variant = 0; variant < 22; variant++)
            {
                all.Add(Representatives("rep-pair-" + (++serial), n, false, variant));
                all.Add(Representatives("rep-role-" + (++serial), n, true, variant));
            }

            return all;
        }

        public static CaravanProblem Pick(List<CaravanProblem> all, int solved, int seed)
        {
            BoardKind[] band = solved < 2
                ? new[] { BoardKind.NumberCards }
                : solved < 4 ? new[] { BoardKind.PairGrid, BoardKind.DiceGrid }
                : solved == 4 ? new[] { BoardKind.DigitCards }
                : new[] { BoardKind.Representatives };
            var pool = new List<CaravanProblem>();
            for (int i = 0; i < all.Count; i++)
                for (int k = 0; k < band.Length; k++) if (all[i].kind == band[k]) { pool.Add(all[i]); break; }
            return pool[Math.Abs(seed) % pool.Count];
        }

        public static bool SetsEqual(bool[] selected, bool[] answer)
        {
            if (selected == null || answer == null || selected.Length < answer.Length) return false;
            for (int i = 0; i < answer.Length; i++) if (selected[i] != answer[i]) return false;
            for (int i = answer.Length; i < selected.Length; i++) if (selected[i]) return false;
            return true;
        }

        public static string Diagnose(CaravanProblem p, bool[] selected)
        {
            if (p.kind == BoardKind.DiceGrid)
            {
                bool half = true, any = false;
                for (int r = 0; r < p.rows; r++) for (int c = 0; c < p.cols; c++)
                {
                    int i = r * p.cols + c;
                    if (!p.answerSet[i]) continue;
                    bool expectedHalf = r <= c;
                    if (selected[i]) any = true;
                    if (selected[i] != expectedHalf) half = false;
                }
                if (half && any) return "ordered-pair-half";
            }
            if (p.kind == BoardKind.DigitCards)
            {
                for (int c = 0; c < p.cols; c++) if (selected[c] && p.cellLabels[c].StartsWith("0")) return "leading-zero";
            }
            if (p.kind == BoardKind.Representatives && p.misconceptionId == "unordered-representatives")
            {
                for (int r = 0; r < p.rows; r++) for (int c = 0; c < p.cols; c++)
                    if (r > c && selected[r * p.cols + c]) return "ordered-duplicate";
            }
            if (p.kind == BoardKind.PairGrid && Count(selected, p.answerSet.Length) == p.rows + p.cols) return "sum-product-swapped";
            return "incomplete-or-extra";
        }

        static CaravanProblem NumberMultiples(string id, int n, int divisor)
        {
            var p = NumberBoard(id, n);
            p.prompt = "1부터 " + n + "까지의 자연수 중 " + divisor + "의 배수에 해당하는 경우를 모두 표시하시오.";
            p.concept = "한 사건의 경우의 수";
            p.reveal = divisor + "의 배수 " + (n / divisor) + "가지";
            p.misconceptionId = "multiple-count";
            p.misconceptionCounts = new[] { n, divisor, n - n / divisor };
            for (int i = 1; i <= n; i++) p.answerSet[i - 1] = i % divisor == 0;
            return p;
        }

        static CaravanProblem NumberUnion(string id, int n, int a, int b)
        {
            var p = NumberBoard(id, n);
            p.prompt = "1부터 " + n + "까지의 자연수 중 " + a + "의 배수 또는 " + b + "의 배수에 해당하는 경우를 모두 표시하시오.";
            p.concept = "두 사건이 동시에 일어나지 않을 때의 합의 법칙";
            p.reveal = a + "의 배수와 " + b + "의 배수는 겹치지 않아 " + (n / a + n / b) + "가지";
            p.misconceptionId = "sum-product-swapped";
            int countA = n / a, countB = n / b;
            p.misconceptionCounts = new[] { countA * countB, Math.Max(countA, countB), n };
            for (int i = 1; i <= n; i++) p.answerSet[i - 1] = i % a == 0 || i % b == 0;
            return p;
        }

        static CaravanProblem NumberBoard(string id, int n)
        {
            int cols = n <= 15 ? 5 : 6;
            int rows = (n + cols - 1) / cols;
            var p = Base(id, BoardKind.NumberCards, rows, cols);
            for (int i = 0; i < p.cellLabels.Length; i++) p.cellLabels[i] = i < n ? (i + 1).ToString() : "·";
            return p;
        }

        static CaravanProblem ProductGrid(string id, int r, int c, int variant)
        {
            var p = Base(id, BoardKind.PairGrid, Math.Min(6, r + 1), Math.Min(6, c + 1));
            string[] first = { "망토", "천막 천", "관측 렌즈", "항로 지도" };
            string[] second = { "나침반", "풍향 깃", "삼각대", "바람종" };
            p.prompt = first[variant % first.Length] + " " + r + "종류와 " + second[variant % second.Length] + " " + c + "종류 중에서 각각 하나씩 고르는 경우를 모두 표시하시오.";
            p.concept = "곱의 법칙";
            p.reveal = r + " × " + c + " = " + (r * c) + "가지";
            p.misconceptionId = "sum-product-swapped";
            p.misconceptionCounts = new[] { r + c, Math.Max(r, c), (r + 1) * (c + 1) };
            for (int y = 0; y < p.rows; y++) p.rowLabels[y] = "망" + (y + 1);
            for (int x = 0; x < p.cols; x++) p.colLabels[x] = "나" + (x + 1);
            for (int y = 0; y < p.rows; y++) for (int x = 0; x < p.cols; x++)
            {
                int i = y * p.cols + x;
                p.cellLabels[i] = (y + 1) + "·" + (x + 1);
                p.answerSet[i] = y < r && x < c;
            }
            return p;
        }

        static CaravanProblem DiceSum(string id, int sum, bool practice, int variant = 0)
        {
            var p = Base(id, BoardKind.DiceGrid, 6, 6);
            p.prompt = "서로 다른 두 개의 주사위를 동시에 던질 때, 두 눈의 수의 합이 " + sum + "인 경우를 모두 표시하시오.";
            if (!practice) p.prompt = RouteNames[variant % RouteNames.Length] + "의 관측 기록이다. " + p.prompt;
            p.concept = "서로 다른 두 주사위의 순서쌍";
            p.misconceptionId = "ordered-pair-half";
            for (int y = 0; y < 6; y++) for (int x = 0; x < 6; x++)
            {
                int i = y * 6 + x;
                p.cellLabels[i] = "(" + (y + 1) + "," + (x + 1) + ")";
                p.answerSet[i] = y + x + 2 == sum;
            }
            p.reveal = OrderedList(p) + " → " + p.AnswerCount + "가지";
            p.misconceptionCounts = new[] { Math.Max(1, (p.AnswerCount + 1) / 2), sum, 36 };
            return p;
        }

        static CaravanProblem DiceProduct(string id, int product)
        {
            var p = Base(id, BoardKind.DiceGrid, 6, 6);
            p.prompt = "서로 다른 두 개의 주사위를 동시에 던질 때, 두 눈의 수의 곱이 " + product + "인 경우를 모두 표시하시오.";
            p.concept = "서로 다른 두 주사위의 순서쌍";
            p.misconceptionId = "ordered-pair-half";
            for (int y = 0; y < 6; y++) for (int x = 0; x < 6; x++)
            {
                int i = y * 6 + x;
                p.cellLabels[i] = "(" + (y + 1) + "," + (x + 1) + ")";
                p.answerSet[i] = (y + 1) * (x + 1) == product;
            }
            p.reveal = OrderedList(p) + " → " + p.AnswerCount + "가지";
            p.misconceptionCounts = new[] { Math.Max(1, (p.AnswerCount + 1) / 2), product, 36 };
            return p;
        }

        static CaravanProblem DigitCards(string id, int[] digits, int variant)
        {
            int n = digits.Length;
            var p = Base(id, BoardKind.DigitCards, n, n);
            p.prompt = RouteNames[variant % RouteNames.Length] + " 카드 꾸러미에 " + JoinDigits(digits) +
                " 숫자 카드가 각각 한 장씩 있을 때, 그중 2장을 뽑아 만들 수 있는 두 자리 자연수를 모두 표시하시오.";
            p.concept = "0이 포함된 카드로 두 자리 자연수 만들기";
            p.misconceptionId = "leading-zero";
            p.misconceptionCounts = new[] { n * (n - 1), n * n, (n - 1) * (n - 2) };
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                int i = y * n + x;
                p.cellLabels[i] = digits[y].ToString() + digits[x];
                p.answerSet[i] = y != x && digits[y] != 0;
            }
            p.reveal = "십의 자리에는 0을 놓지 않아 " + p.AnswerCount + "가지";
            return p;
        }

        static CaravanProblem Representatives(string id, int n, bool roles, int variant)
        {
            var p = Base(id, BoardKind.Representatives, n, n);
            p.prompt = n + "명의 후보 중에서 " + (roles ? "회장 1명과 부회장 1명" : "대표 2명") + "을 뽑는 경우를 모두 표시하시오.";
            p.prompt = RouteNames[variant % RouteNames.Length] + "의 " + p.prompt;
            p.concept = roles ? "역할이 다른 대표 뽑기" : "역할이 같은 대표 2명 뽑기";
            p.misconceptionId = roles ? "role-order-missing" : "unordered-representatives";
            p.misconceptionCounts = roles
                ? new[] { n * (n - 1) / 2, n * n }
                : new[] { n * (n - 1), n * (n + 1) / 2 };
            string[] names = { "A", "B", "C", "D", "E", "F" };
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                int i = y * n + x;
                // A-B and B-A are the same unordered pair.  Showing both but
                // accepting only y<x made an equivalent answer fail.  The
                // no-role board now exposes one canonical physical pin only.
                p.cellLabels[i] = !roles && y >= x ? "·" : names[y] + "-" + names[x];
                p.answerSet[i] = roles ? y != x : y < x;
            }
            p.reveal = roles ? n + " × " + (n - 1) + " = " + p.AnswerCount + "가지" : "같은 쌍을 한 번만 세어 " + p.AnswerCount + "가지";
            return p;
        }

        static CaravanProblem Base(string id, BoardKind kind, int rows, int cols)
        {
            var p = new CaravanProblem { id = id, kind = kind, rows = rows, cols = cols };
            p.rowLabels = new string[rows]; p.colLabels = new string[cols];
            p.cellLabels = new string[rows * cols]; p.answerSet = new bool[rows * cols];
            for (int i = 0; i < rows; i++) p.rowLabels[i] = (i + 1).ToString();
            for (int i = 0; i < cols; i++) p.colLabels[i] = (i + 1).ToString();
            return p;
        }

        static int Count(bool[] values, int n) { int c = 0; for (int i = 0; i < n; i++) if (values[i]) c++; return c; }
        static int Gcd(int a, int b) { while (b != 0) { int t = a % b; a = b; b = t; } return a; }
        static int Lcm(int a, int b) { return a / Gcd(a, b) * b; }
        static string JoinDigits(int[] d)
        {
            // Every digit is a separate physical card.  The former hand-built
            // concatenation omitted the final separator and rendered {0,2,4,7}
            // as "0, 2, 47", while the board and answer set still used four
            // cards.  Keep the prompt on the exact same integer array.
            return string.Join(", ", Array.ConvertAll(d, x => x.ToString()));
        }
        static string OrderedList(CaravanProblem p)
        {
            var s = new System.Text.StringBuilder();
            for (int i = 0; i < p.answerSet.Length; i++) if (p.answerSet[i]) { if (s.Length > 0) s.Append(", "); s.Append(p.cellLabels[i]); }
            return s.ToString();
        }
    }
}
