// 제곱 활주로 문제 규칙. 실제 플레이와 QA 은행이 같은 PanelProblem 객체를 사용한다.
// 중2 피타고라스 표현 함정:
// - 모든 계산은 int 제곱으로 한다. float 비교와 중3 기호 표현을 쓰지 않는다.
// - 판별은 가장 긴 변을 c로 둔다. 길이 답은 자연수인 피타고라스 수만 쓴다.
// - 수와 단위는 띄어 쓰고(cm, cm²), 발문은 지시형 「~찾으시오」로 통일한다.
// 오답 패널은 misconceptionId로 생성 근거를 보존한다.
using System;
using System.Collections.Generic;
using Mgf;

namespace Mgf.JegopHwaljuro
{
    [Serializable]
    public sealed class PanelDatum
    {
        public int side;
        public string misconceptionId;
        public bool locked;
    }

    [Serializable]
    public sealed class PanelProblem
    {
        public string id;
        public string prompt;
        public string concept;
        public string reveal;
        public int band;
        public int targetC;
        public int answerA;
        public int answerB;
        public int correctI;
        public int correctJ;
        public PanelDatum[] panels;
        public string[] pairChoices;
        public string answerLabel;

        public int TargetArea => targetC * targetC;
        public bool IsCorrectPair(int i, int j)
        {
            if (i == j || i < 0 || j < 0 || i >= panels.Length || j >= panels.Length) return false;
            int x = panels[i].side, y = panels[j].side;
            return x * x + y * y == TargetArea;
        }

        public string PanelLabel(int index)
        {
            int s = panels[index].side;
            return band <= 1 ? (s * s) + " cm²" : s + " cm";
        }

        // 학생에게 보이는 오답식도 판정에 쓰는 같은 정수 데이터를 사용한다.
        // 길이 문항에서는 반드시 각 변을 제곱한 뒤 cm²끼리 비교한다.
        public string WrongReveal(int i, int j)
        {
            int x = panels[i].side, y = panels[j].side;
            int sum = x * x + y * y;
            if (band <= 1)
                return (x * x) + " + " + (y * y) + " = " + sum + " cm²  ≠  " + TargetArea + " cm²";
            return x + "² + " + y + "² = " + sum + " cm²  ≠  " + targetC + "² = " + TargetArea + " cm²";
        }

        string PairLabel(int i, int j)
        {
            return band <= 1
                ? (panels[i].side * panels[i].side) + " cm² + " + (panels[j].side * panels[j].side) + " cm²"
                : panels[i].side + " cm + " + panels[j].side + " cm";
        }

        public void BuildChoices()
        {
            var all = new List<string>(10);
            for (int i = 0; i < 5; i++)
            for (int j = i + 1; j < 5; j++) all.Add(PairLabel(i, j));
            pairChoices = all.ToArray();
            answerLabel = PairLabel(Math.Min(correctI, correctJ), Math.Max(correctI, correctJ));
        }

        public MgfProblem ToMgf()
        {
            return new MgfProblem
            {
                id = id,
                prompt = prompt,
                choices = pairChoices,
                answer = answerLabel,
                answerNumeric = TargetArea,
                unitConcept = concept
            };
        }
    }

    public static class JegopRules
    {
        public const int Goal = 7;
        public const int StartLives = 3;

        static readonly int[,] PairSlots =
        {
            {0,1},{0,2},{0,3},{0,4},{1,2},{1,3},{1,4},{2,3},{2,4},{3,4}
        };

        public static PanelProblem Practice()
        {
            var p = new PanelProblem
            {
                id = "practice-3-4-5",
                band = 1,
                targetC = 5,
                answerA = 3,
                answerB = 4,
                correctI = 0,
                correctJ = 1,
                concept = "세 변 위 정사각형의 넓이 관계",
                prompt = "넓이의 합이 25 cm²인 두 패널을 직접 포개시오.",
                reveal = "9 + 16 = 25",
                panels = new[]
                {
                    new PanelDatum { side = 3 },
                    new PanelDatum { side = 4 },
                    new PanelDatum { side = 6, locked = true, misconceptionId = "practice-locked" },
                    new PanelDatum { side = 7, locked = true, misconceptionId = "practice-locked" },
                    new PanelDatum { side = 8, locked = true, misconceptionId = "practice-locked" }
                }
            };
            p.BuildChoices();
            return p;
        }

        public static List<PanelProblem> BuildCatalog()
        {
            var result = new List<PanelProblem>(420);
            var tripleSeen = new HashSet<string>();
            int serial = 0;
            for (int m = 2; m <= 32 && result.Count < 420; m++)
            for (int n = 1; n < m && result.Count < 420; n++)
            {
                int a0 = m * m - n * n;
                int b0 = 2 * m * n;
                int c0 = m * m + n * n;
                int lo = Math.Min(a0, b0), hi = Math.Max(a0, b0);
                for (int scale = 1; scale <= 7 && result.Count < 420; scale++)
                {
                    int a = lo * scale, b = hi * scale, c = c0 * scale;
                    // 3-4-5처럼 c가 너무 작은 삼각형은 c보다 작은 서로 다른 오답 3개를
                    // 함께 제시할 수 없다. 실제 플레이와 문제 은행 모두 5-12-13 이상으로
                    // 시작해 값의 크기만으로 정답 두 장을 찾는 우회를 막는다.
                    if (c < 13 || c > 600) continue;
                    string tripleKey = a + ":" + b + ":" + c;
                    if (!tripleSeen.Add(tripleKey)) continue;
                    int band = serial % 7 < 2 ? 1 : serial % 7 < 5 ? 2 : 3;
                    var p = Make("runway-" + (serial + 1), a, b, c, band, serial % 10, serial);
                    ValidateOne(p);
                    result.Add(p);
                    serial++;
                }
            }
            if (result.Count < 300) throw new InvalidOperationException("문제 은행이 300개보다 작습니다: " + result.Count);
            Validate(result);
            return result;
        }

        static PanelProblem Make(string id, int a, int b, int c, int band, int pairSlot, int serial)
        {
            int ci = PairSlots[pairSlot, 0], cj = PairSlots[pairSlot, 1];
            var panel = new PanelDatum[5];
            panel[ci] = new PanelDatum { side = a };
            panel[cj] = new PanelDatum { side = b };

            // 오답도 모두 c보다 작고, 어느 두 패널을 골라도 삼각형이 되게 만든다.
            // 높은 정답변 b의 제곱 변화량을 무시하고 b-k로 읽는 세 구체적 오개념에서
            // 역산한다. 따라서 smallest/largest/target-nearest 정책도 정답이 아니다.
            var wrong = BuildDistractors(a, b, c, serial);
            int cursor = 0;
            for (int i = 0; i < 5; i++) if (panel[i] == null) panel[i] = wrong[cursor++];

            string prompt;
            string concept;
            if (band == 1)
            {
                prompt = "목표 대각선이 " + c + " cm이다. 넓이의 합이 " + (c * c) + " cm²인 두 패널을 찾으시오.";
                concept = "세 변 위 정사각형의 넓이 관계";
            }
            else if (band == 2)
            {
                prompt = "빗변이 " + c + " cm인 직각삼각형의 빛길을 만들 두 변 패널을 찾으시오.";
                concept = "피타고라스 정리로 다른 한 변 찾기";
            }
            else
            {
                prompt = "가장 긴 변의 길이가 " + c + " cm인 삼각형이 직각삼각형이 되도록 하는 나머지 두 변의 길이를 고르시오.";
                concept = "피타고라스 정리의 역과 가장 긴 변";
            }

            var p = new PanelProblem
            {
                id = id,
                prompt = prompt,
                concept = concept,
                reveal = a + "² + " + b + "² = " + (a * a) + " + " + (b * b) + " = " + (c * c) + " = " + c + "²",
                band = band,
                targetC = c,
                answerA = a,
                answerB = b,
                correctI = ci,
                correctJ = cj,
                panels = panel
            };
            p.BuildChoices();
            return p;
        }

        static List<PanelDatum> BuildDistractors(int a, int b, int c, int serial)
        {
            var wrong = new List<PanelDatum>(3);
            var used = new HashSet<int> { a, b };

            Action<int, string> add = (value, misconception) =>
            {
                if (wrong.Count >= 3 || value <= 0 || value >= c || used.Contains(value)) return;
                // 오답끼리 또는 정답 한 변과 우연히 또 다른 정답쌍을 만들지 않는다.
                foreach (int side in used)
                    if (side * side + value * value == c * c) return;
                used.Add(value);
                wrong.Add(new PanelDatum { side = value, misconceptionId = misconception });
            };

            int lo = Math.Min(a, b), hi = Math.Max(a, b);
            // 정확한 빠진 변 hi를 구한 뒤 제곱근 계산에서 ±k를 붙이는 오개념을
            // 양쪽에 배치한다. 가능한 경우 아래·위 오답을 하나씩 먼저 넣어
            // 정답 두 변이 정렬 양끝에 함께 남지 않게 한다.
            for (int delta = 1; delta < hi && wrong.Count < 1; delta++)
            {
                int value = hi - delta;
                if (lo + value > c) add(value, "square-root-off-by-minus-" + delta);
            }
            for (int delta = 1; hi + delta < c && wrong.Count < 2; delta++)
                add(hi + delta, "square-root-off-by-plus-" + delta);
            for (int delta = 1; delta < c && wrong.Count < 3; delta++)
            {
                int lower = hi - delta;
                if (lower > 0 && lo + lower > c) add(lower, "square-root-off-by-minus-" + delta);
                if (hi + delta < c) add(hi + delta, "square-root-off-by-plus-" + delta);
            }

            if (wrong.Count != 3)
                throw new InvalidOperationException("오답 패널 3개 생성 실패: " + a + "," + b + "," + c);
            return wrong;
        }

        public static PanelProblem PickSession(List<PanelProblem> catalog, int solved, int runSerial)
        {
            int[] bands = { 1, 1, 2, 2, 2, 3, 3 };
            int wanted = bands[Math.Min(solved, bands.Length - 1)];
            var pool = new List<PanelProblem>();
            // 플레이 화면은 한눈에 제곱할 수 있는 수로 한정한다. 큰 자연수 문항은 QA 은행 다양성에만 남긴다.
            for (int i = 0; i < catalog.Count; i++) if (catalog[i].band == wanted && catalog[i].targetC <= 65) pool.Add(catalog[i]);
            int index = PositiveMod(runSerial * 43 + solved * 71, pool.Count);
            int desiredPairSlot = PositiveMod(runSerial + solved * 3, 10);
            return Reposition(pool[index], desiredPairSlot);
        }

        // 수학값과 화면 위치를 분리한다. 각 판의 정답 위치는 10개 unordered pair에 균등 배치되고,
        // 색·크기·광택은 그대로라 위치 고정/인접쌍 봇이 수학을 우회하지 못한다.
        static PanelProblem Reposition(PanelProblem source, int pairSlot)
        {
            int ci = PairSlots[pairSlot, 0], cj = PairSlots[pairSlot, 1];
            var panel = new PanelDatum[5];
            panel[ci] = new PanelDatum { side = source.answerA };
            panel[cj] = new PanelDatum { side = source.answerB };
            var wrong = new List<PanelDatum>(3);
            for (int i = 0; i < source.panels.Length; i++)
                if (!string.IsNullOrEmpty(source.panels[i].misconceptionId))
                    wrong.Add(new PanelDatum { side = source.panels[i].side, misconceptionId = source.panels[i].misconceptionId });
            int w = 0;
            for (int i = 0; i < 5; i++) if (panel[i] == null) panel[i] = wrong[w++];
            var p = new PanelProblem
            {
                id = source.id + "-slot-" + pairSlot,
                prompt = source.prompt,
                concept = source.concept,
                reveal = source.reveal,
                band = source.band,
                targetC = source.targetC,
                answerA = source.answerA,
                answerB = source.answerB,
                correctI = ci,
                correctJ = cj,
                panels = panel
            };
            p.BuildChoices();
            ValidateOne(p);
            return p;
        }

        static int PositiveMod(int x, int n) { int r = x % n; return r < 0 ? r + n : r; }

        static void Validate(List<PanelProblem> all)
        {
            var ids = new HashSet<string>();
            var prompts = new HashSet<string>();
            for (int i = 0; i < all.Count; i++)
            {
                ValidateOne(all[i]);
                if (!ids.Add(all[i].id)) throw new InvalidOperationException("중복 문제 ID");
                if (!prompts.Add(all[i].prompt + "|" + all[i].answerLabel)) throw new InvalidOperationException("중복 문항");
            }
        }

        static void ValidateOne(PanelProblem p)
        {
            int validPairs = 0;
            var sideSet = new HashSet<int>();
            for (int i = 0; i < 5; i++)
            {
                if (p.panels[i].side <= 0 || !sideSet.Add(p.panels[i].side)) throw new InvalidOperationException("패널 값 중복/범위 오류: " + p.id);
                for (int j = i + 1; j < 5; j++) if (p.IsCorrectPair(i, j)) validPairs++;
            }
            if (validPairs != 1) throw new InvalidOperationException("정답쌍이 1개가 아님: " + p.id + " / " + validPairs);
            if (!p.IsCorrectPair(p.correctI, p.correctJ)) throw new InvalidOperationException("정답 위치 불일치: " + p.id);
            if (Array.IndexOf(p.pairChoices, p.answerLabel) < 0) throw new InvalidOperationException("choices에 answer 없음: " + p.id);
            if (p.answerA * p.answerA + p.answerB * p.answerB != p.targetC * p.targetC) throw new InvalidOperationException("피타고라스 정수식 오류: " + p.id);
            if (p.answerA + p.answerB <= p.targetC) throw new InvalidOperationException("삼각형 불성립: " + p.id);
            if (!p.id.StartsWith("practice-", StringComparison.Ordinal))
            {
                for (int i = 0; i < p.panels.Length; i++)
                    if (p.panels[i].side >= p.targetC)
                        throw new InvalidOperationException("c 이상 패널로 정답 누설: " + p.id);

                var order = new List<int> { 0, 1, 2, 3, 4 };
                order.Sort((x, y) => p.panels[x].side.CompareTo(p.panels[y].side));
                if (p.IsCorrectPair(order[0], order[1])) throw new InvalidOperationException("smallest-two 우회: " + p.id);
                if (p.IsCorrectPair(order[3], order[4])) throw new InvalidOperationException("largest-two 우회: " + p.id);
                if (p.IsCorrectPair(order[4], order[3])) throw new InvalidOperationException("target-nearest 우회: " + p.id);
                for (int i = 0; i < p.panels.Length; i++)
                for (int j = i + 1; j < p.panels.Length; j++)
                    if (p.panels[i].side + p.panels[j].side <= p.targetC)
                        throw new InvalidOperationException("삼각형이 아닌 선택쌍: " + p.id + " / " + i + "," + j);
            }
        }
    }
}
