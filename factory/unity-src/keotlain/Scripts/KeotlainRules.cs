// 컷라인 — 중2 피타고라스 정리의 단일 정수 문제 모델과 생성기.
//
// curriculum/2022-middle-math.json의 이번 단원 표현 함정:
// - 중2 피타고라스 정리는 제곱근(√)을 배우기 전이다. 모든 길이 답은 자연수가 되는
//   피타고라스 수와 그 자연수 배만 쓴다. 풀이는 x²=25, x>0이므로 x=5처럼 쓴다.
// - 빗변은 화면의 아래쪽 변이 아니라 직각의 대변(가장 긴 변)이다.
// - 빗변과 한 변이 주어지면 제곱의 합이 아니라 c²-a²=b²를 쓴다.
// - 값 요구 발문은 「~의 길이를 구하시오」로 통일하고 수와 단위 cm는 띄어 쓴다.
// - 「가정」「결론」은 쓰지 않으며 근호 기호·제곱근 용어를 생성하지 않는다.
//
// 오답 후처리 순서: 정답 제거 → 1..29 범위 검사 → 중복 제거 → 부족분 보충 → 셔플.
// 두 대표 오개념은 misconceptionId로 남긴다.
// - add-sides: a+b=c로 길이를 단순히 더함.
// - add-squares-for-leg: 빗변과 한 변이 주어져도 제곱을 더하거나 c+a를 고름.
using System;
using System.Collections.Generic;
using Mgf;

namespace Mgf.Keotlain
{
    public enum CutMode { Hypotenuse, Leg, Double }

    [Serializable]
    public sealed class CutProblem
    {
        public string id;
        public CutMode mode;
        public int band;
        public int a;
        public int b;
        public int c;
        public int height;
        public int leftHyp;
        public int rightHyp;
        public int[] pieces;
        public string prompt;
        public string boardLine;
        public string reveal;
        public string unitConcept;
        public string[] choices;
        public string misconceptionA;
        public string misconceptionB;

        public int Answer { get { return mode == CutMode.Double ? pieces[0] + pieces[1] : pieces[0]; } }
        public int PieceAt(int index) { return pieces[Math.Max(0, Math.Min(index, pieces.Length - 1))]; }

        public MgfProblem ToMgf()
        {
            return new MgfProblem {
                id = id,
                prompt = prompt,
                choices = choices,
                answer = AnswerText(),
                answerNumeric = Answer,
                unitConcept = unitConcept
            };
        }

        public string AnswerText()
        {
            if (mode == CutMode.Double) return "BD=" + pieces[0] + " cm, DC=" + pieces[1] + " cm";
            return pieces[0] + " cm";
        }
    }

    public static class KeotlainRules
    {
        public const int StartLives = 3;
        public const int Goal = 8;
        public const int CartridgeLength = 29;
        public const int CartridgeCount = 4;

        // (직각변 a, 직각변 b, 빗변 c). 모든 항목은 int 제곱으로 전수 검증한다.
        static readonly int[,] Triples = {
            {3,4,5}, {6,8,10}, {9,12,15}, {12,16,20}, {15,20,25},
            {5,12,13}, {10,24,26}, {8,15,17}, {7,24,25}, {20,21,29}
        };

        // 공통 높이, 왼쪽 빗변, 오른쪽 빗변, 왼쪽 밑변, 오른쪽 밑변.
        static readonly int[,] DoubleSets = {
            {3,5,5,4,4}, {4,5,5,3,3},
            {5,13,13,12,12}, {6,10,10,8,8}, {7,25,25,24,24},
            {8,10,17,6,15}, {8,17,17,15,15}, {12,13,15,5,9}, {12,15,20,9,16},
            {15,17,25,8,20}, {20,25,29,15,21}
        };

        static List<CutProblem> cachedCatalog;

        public static CutProblem Practice()
        {
            return Hyp("cut-practice-345", 3, 4, 5, 0, 0);
        }

        static string Context(int variant)
        {
            string[] places = {
                "북쪽 안테나 옆", "보라 스텐실 앞", "동쪽 환풍기 옆", "주황 안전선 안쪽",
                "서쪽 난간 옆", "청록 벽화 앞", "중앙 계단 옆", "빗물 홈통 앞",
                "방수포 왼쪽", "옥상문 오른쪽", "낮은 파라펫 옆", "조명 기둥 앞",
                "폐자재 통 옆", "급수 탱크 앞"
            };
            return places[Math.Abs(variant) % places.Length];
        }

        static CutProblem Hyp(string id, int a, int b, int c, int band, int variant)
        {
            var p = new CutProblem {
                id = id, mode = CutMode.Hypotenuse, band = band,
                a = a, b = b, c = c, pieces = new[] { c },
                prompt = Context(variant) + " 램프에서 직각을 낀 두 변이 " + a + " cm, " + b + " cm일 때 빗변 버팀목 길이를 구하시오.",
                boardLine = "직각변 " + a + " cm · " + b + " cm   |   빗변 ?",
                reveal = a + "²+" + b + "²=" + (a * a + b * b) + "=" + c + "², " + c + ">0이므로 " + c + " cm",
                unitConcept = "피타고라스 정리로 빗변의 길이 구하기",
                misconceptionA = "add-sides",
                misconceptionB = "wrong-hypotenuse-position"
            };
            p.choices = Choices(p, variant);
            return p;
        }

        static CutProblem Leg(string id, int known, int answer, int c, int band, int variant)
        {
            var p = new CutProblem {
                id = id, mode = CutMode.Leg, band = band,
                a = known, b = answer, c = c, pieces = new[] { answer },
                prompt = Context(variant) + " 램프에서 빗변이 " + c + " cm이고 한 변이 " + known + " cm일 때 다른 버팀목 길이를 구하시오.",
                boardLine = "빗변 " + c + " cm · 한 변 " + known + " cm   |   다른 변 ?",
                reveal = c + "²−" + known + "²=" + (c * c - known * known) + "=" + answer + "², " + answer + ">0이므로 " + answer + " cm",
                unitConcept = "빗변과 한 변으로 다른 한 변의 길이 구하기",
                misconceptionA = "add-squares-for-leg",
                misconceptionB = "subtract-lengths-only"
            };
            p.choices = Choices(p, variant);
            return p;
        }

        static CutProblem Double(string id, int h, int lh, int rh, int left, int right, int band, int variant)
        {
            var p = new CutProblem {
                id = id, mode = CutMode.Double, band = band,
                height = h, leftHyp = lh, rightHyp = rh, pieces = new[] { left, right },
                prompt = Context(variant) + " 이중 램프에서 D는 BC 위의 점이고 AD⊥BC이다. AB=" + lh + " cm, AD=" + h + " cm, AC=" + rh + " cm일 때 BD와 DC의 길이를 각각 구하시오.",
                boardLine = "A에서 BC에 내린 수선의 발 D (AD⊥BC)  |  AB " + lh + " · AD " + h + " · AC " + rh + " cm  |  BD ? · DC ?",
                reveal = "BD²=" + lh + "²−" + h + "²=" + (left * left) + ", DC²=" + rh + "²−" + h + "²=" + (right * right) + " → BC=" + (left + right) + " cm",
                unitConcept = "피타고라스 정리를 두 번 적용하기",
                misconceptionA = "double-only-one-piece",
                misconceptionB = "double-subtract-pieces"
            };
            p.choices = Choices(p, variant);
            return p;
        }

        static string[] Choices(CutProblem p, int variant)
        {
            var raw = new List<string>();
            string answer = p.AnswerText();
            if (p.mode == CutMode.Hypotenuse)
            {
                int add = p.a + p.b; // add-sides 역산
                if (add <= 29) raw.Add(add + " cm");
                raw.Add(Math.Max(1, p.c - 1) + " cm");
                raw.Add(Math.Min(29, p.c + 1) + " cm");
                raw.Add(p.a + " cm"); // 화면 아래쪽 변을 빗변으로 보는 오류
            }
            else if (p.mode == CutMode.Leg)
            {
                int plus = p.c + p.a; // add-squares-for-leg/c+a 역산
                if (plus <= 29) raw.Add(plus + " cm");
                raw.Add(Math.Max(1, p.c - p.a) + " cm"); // 제곱 없이 길이만 뺌
                raw.Add(Math.Max(1, p.PieceAt(0) - 1) + " cm");
                raw.Add(Math.Min(29, p.PieceAt(0) + 1) + " cm");
            }
            else
            {
                raw.Add("BD=" + p.pieces[0] + " cm, DC=" + p.pieces[0] + " cm");
                raw.Add("BD=" + p.pieces[1] + " cm, DC=" + p.pieces[1] + " cm");
                raw.Add("BD=" + Math.Abs(p.pieces[0] - p.pieces[1]) + " cm, DC=" + Math.Min(p.pieces[0], p.pieces[1]) + " cm");
                raw.Add((p.pieces[0] + p.pieces[1]) + " cm");
            }

            var outList = new List<string>(4);
            var seen = new HashSet<string>();
            // 정답 제거 → 범위 검사는 위 생성에서 수행 → 중복 제거.
            for (int i = 0; i < raw.Count && outList.Count < 3; i++)
                if (raw[i] != answer && seen.Add(raw[i])) outList.Add(raw[i]);
            // 부족분 보충.
            for (int k = 1; outList.Count < 3; k++)
            {
                string value = p.mode == CutMode.Double
                    ? "BD=" + ((p.pieces[0] + k) % 28 + 1) + " cm, DC=" + ((p.pieces[1] + k * 2) % 28 + 1) + " cm"
                    : ((p.Answer + k * 7 - 1) % 29 + 1) + " cm";
                if (value != answer && seen.Add(value)) outList.Add(value);
            }
            outList.Add(answer);
            // 결정적 셔플.
            var rng = new Random(variant * 7919 + StableHash(p.id));
            for (int i = outList.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); string t = outList[i]; outList[i] = outList[j]; outList[j] = t; }
            return outList.ToArray();
        }

        public static List<CutProblem> BuildCatalog()
        {
            if (cachedCatalog != null) return cachedCatalog;
            var list = new List<CutProblem>(400);
            int serial = 0;
            // 제약 만족 풀에서 직접 만든다. 난수 후 필터링이 아니다.
            for (int t = 0; t < Triples.GetLength(0); t++)
            for (int variant = 0; variant < 14; variant++)
            {
                int a = Triples[t,0], b = Triples[t,1], c = Triples[t,2];
                list.Add(Hyp("cut-h-" + (++serial), a, b, c, 1, variant));
                list.Add(Leg("cut-la-" + (++serial), a, b, c, 2, variant));
                list.Add(Leg("cut-lb-" + (++serial), b, a, c, 2, variant));
            }
            for (int d = 0; d < DoubleSets.GetLength(0); d++)
            for (int variant = 0; variant < 6; variant++)
            {
                list.Add(Double("cut-d-" + (++serial), DoubleSets[d,0], DoubleSets[d,1], DoubleSets[d,2],
                    DoubleSets[d,3], DoubleSets[d,4], 3, variant));
            }
            Validate(list);
            cachedCatalog = list;
            return cachedCatalog;
        }

        public static List<CutProblem> BuildRunDeck(int runSerial)
        {
            var all = BuildCatalog();
            var h = new List<CutProblem>(); var l = new List<CutProblem>(); var d = new List<CutProblem>();
            var seenH = new HashSet<int>(); var seenL = new HashSet<int>(); var seenD = new HashSet<string>();
            for (int i = 0; i < all.Count; i++)
            {
                var p = all[i];
                if (p.mode == CutMode.Hypotenuse && seenH.Add(p.Answer)) h.Add(p);
                else if (p.mode == CutMode.Leg && seenL.Add(p.Answer)) l.Add(p);
                else if (p.mode == CutMode.Double && seenD.Add(p.pieces[0] + ":" + p.pieces[1])) d.Add(p);
            }
            // 첫 주문은 9개 자연수 빗변 답에 회차별 균등 순환한다. 고정 눈금 정책이
            // 저용량 탐색의 선택 편향을 이용하지 못하게 한 뒤, 나머지 두 빗변은 짧은 순으로 둔다.
            var firstHyp = h[(runSerial - 1) % h.Count];
            h.Remove(firstHyp);
            h.Sort((x,y) => x.Answer.CompareTo(y.Answer));
            var hyp = new List<CutProblem> { firstHyp, h[0], h[1] };
            Shuffle(l, runSerial * 47 + 13); Shuffle(d, runSerial * 61 + 17);
            // 3+3+2의 밴드 순서는 지킨다. 총 절단 길이 104 이하인 조합을 작은 정수 탐색으로 찾는다.
            for (int li = 0; li < l.Count; li++)
            for (int di = 0; di < d.Count; di++)
            {
                var deck = new List<CutProblem>(8);
                int sum = 0;
                for (int i = 0; i < 3; i++) { var p = hyp[i]; deck.Add(p); sum += p.Answer; }
                for (int i = 0; i < 3; i++) { var p = l[(li + i) % l.Count]; deck.Add(p); sum += p.Answer; }
                for (int i = 0; i < 2; i++) { var p = d[(di + i) % d.Count]; deck.Add(p); sum += p.Answer; }
                // 87 이하이면 최대 29 cm짜리 일반 오절단 한 번 뒤에도 4×29 cm 안에서 완주 가능하다.
                if (sum <= 87) return CloneForRun(deck, runSerial);
            }
            // 연속 회전 후보가 우연히 모두 큰 시드의 결정적 폴백. 각 밴드의 가장 짧은 주문을
            // 고르므로 난도 순서와 1회 최대 오절단 회복 계약을 보존한다.
            l.Sort((x,y) => x.Answer.CompareTo(y.Answer));
            d.Sort((x,y) => x.Answer.CompareTo(y.Answer));
            var fallback = new List<CutProblem>(8);
            for (int i = 0; i < 3; i++) fallback.Add(hyp[i]);
            for (int i = 0; i < 3; i++) fallback.Add(l[i]);
            for (int i = 0; i < 2; i++) fallback.Add(d[i]);
            return CloneForRun(fallback, runSerial);
        }

        static List<CutProblem> CloneForRun(List<CutProblem> source, int runSerial)
        {
            // 카탈로그와 판정 객체 자체를 공유하되, 런 리스트만 새로 만든다. 원본 id를 변형하지 않는다.
            return new List<CutProblem>(source);
        }

        static void Shuffle<T>(List<T> list, int seed)
        {
            var rng = new Random(seed);
            for (int i = list.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); T t = list[i]; list[i] = list[j]; list[j] = t; }
        }

        public static string MisconceptionId(CutProblem p, int cut)
        {
            if (p.mode == CutMode.Hypotenuse && cut == p.a + p.b) return "add-sides";
            if (p.mode == CutMode.Leg && cut == p.c + p.a) return "add-squares-for-leg";
            if (p.mode == CutMode.Leg && cut == p.c - p.a) return "subtract-lengths-only";
            if (p.mode == CutMode.Double && (cut == p.pieces[0] + p.pieces[1] || cut == Math.Abs(p.pieces[0] - p.pieces[1]))) return "double-combine-before-cut";
            return "wrong-integer-length";
        }

        static void Validate(List<CutProblem> list)
        {
            if (list.Count < 300) throw new InvalidOperationException("문제 은행 300개 미만: " + list.Count);
            var ids = new HashSet<string>();
            for (int i = 0; i < list.Count; i++)
            {
                var p = list[i];
                if (!ids.Add(p.id)) throw new InvalidOperationException("중복 id: " + p.id);
                if (p.mode == CutMode.Hypotenuse && p.a * p.a + p.b * p.b != p.c * p.c) throw new InvalidOperationException("빗변 정수식 오류");
                if (p.mode == CutMode.Leg && p.c * p.c - p.a * p.a != p.pieces[0] * p.pieces[0]) throw new InvalidOperationException("다른 변 정수식 오류");
                if (p.mode == CutMode.Double)
                {
                    if (p.leftHyp * p.leftHyp - p.height * p.height != p.pieces[0] * p.pieces[0]) throw new InvalidOperationException("BD 오류");
                    if (p.rightHyp * p.rightHyp - p.height * p.height != p.pieces[1] * p.pieces[1]) throw new InvalidOperationException("DC 오류");
                }
                for (int k = 0; k < p.pieces.Length; k++) if (p.pieces[k] < 1 || p.pieces[k] > 29) throw new InvalidOperationException("답 범위 오류");
                if (p.choices == null || p.choices.Length != 4) throw new InvalidOperationException("선택지 수 오류");
                var choiceSet = new HashSet<string>(p.choices);
                if (choiceSet.Count != 4 || !choiceSet.Contains(p.AnswerText())) throw new InvalidOperationException("선택지 중복/정답 누락");
                if (p.prompt.IndexOf('√') >= 0 || p.reveal.IndexOf('√') >= 0 || p.prompt.Contains("제곱근")) throw new InvalidOperationException("중2 금지 표현");
            }
        }

        static int StableHash(string s)
        {
            unchecked { int h = 17; for (int i = 0; i < s.Length; i++) h = h * 31 + s[i]; return h & 0x7fffffff; }
        }
    }
}
