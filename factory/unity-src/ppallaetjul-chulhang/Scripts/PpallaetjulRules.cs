using System;
using System.Collections.Generic;
using Mgf;

namespace Mgf.PpallaetjulChulhang
{
    public enum SailOrderKind { SimilarityRatio, GivenAP, AreaRatio, RepairCut }

    [Serializable]
    public sealed class SailProblem
    {
        public string id;
        public string prompt;
        public string concept;
        public SailOrderKind kind;
        public int band;
        public int a, b, c, scale, k;
        public int ab, ac, bc, ap, aq;
        public int wrong1I, wrong1J, wrong2I, wrong2J;
        public string misconception1, misconception2;

        public bool IsCorrect(int i, int j)
        {
            // 정답 판정은 화면 좌표나 float가 아니라 두 정수 교차곱의 AND다.
            return i >= 1 && i <= 7 && j >= 1 && j <= 7
                && (i * a) * 8 == ab * k
                && (j * b) * 8 == ac * k;
        }

        public string AnswerText()
        {
            return "AP=" + ap + " cm, AQ=" + aq + " cm (눈금 " + k + ", " + k + ")";
        }

        public MgfProblem ToMgf(int number)
        {
            return new MgfProblem
            {
                id = string.IsNullOrEmpty(id) ? "pc" + number : id,
                prompt = prompt,
                choices = null,
                answer = AnswerText(),
                answerNumeric = k / 8.0,
                unitConcept = concept
            };
        }
    }

    public static class PpallaetjulRules
    {
        public const int Divisions = 8;
        public const int Goal = 9;
        public const int StartPatches = 3;
        public const float SessionSeconds = 90f;

        // curriculum/2022-middle-math.json expression_traps를 생성기 계약으로 옮긴다.
        // 1) △APQ∽△ABC는 A↔A, P↔B, Q↔C 대응 배열에서만 만든다.
        // 2) 「비」라고 뭉뚱그리지 않고 닮음비/넓이의 비를 구분한다.
        // 3) 중2에는 √가 없으므로 근호·제곱근을 생성하지 않는다.
        // 4) 수와 단위는 12 cm처럼 띄고, 지시형 「~이 되도록 자르시오」를 쓴다.
        // 5) 정당화에서도 교육과정 금지어인 「가정/결론」을 쓰지 않는다.
        static readonly int[,] Models =
        {
            { 4, 5, 6 }, { 4, 6, 7 }, { 5, 5, 6 }, { 5, 6, 7 },
            { 5, 6, 8 }, { 6, 7, 8 }, { 6, 7, 9 }, { 6, 6, 8 }
        };

        public static List<SailProblem> BuildCatalog()
        {
            var list = new List<SailProblem>(448);
            for (int model = 0; model < Models.GetLength(0); model++)
            for (int scale = 1; scale <= 2; scale++)
            for (int k = 1; k <= 7; k++)
            for (int kind = 0; kind < 4; kind++)
                list.Add(Create(model, scale, k, (SailOrderKind)kind));

            string error;
            if (!ValidateCatalog(list, out error))
                throw new InvalidOperationException("빨랫줄 출항 문제 은행 불변식 실패: " + error);
            return list;
        }

        public static SailProblem Practice()
        {
            var p = Create(1, 1, 4, SailOrderKind.SimilarityRatio);
            p.id = "practice-1-2";
            p.a = 4; p.b = 6; p.c = 7; p.scale = 1;
            FillLengths(p);
            p.prompt = "연습: AB=32 cm, AC=48 cm이다. △APQ와 △ABC의 닮음비가 1:2가 되도록 자르시오.";
            return p;
        }

        static SailProblem Create(int model, int scale, int k, SailOrderKind kind)
        {
            var p = new SailProblem
            {
                id = "pc-" + model + "-" + scale + "-" + k + "-" + (int)kind,
                kind = kind,
                band = kind == SailOrderKind.SimilarityRatio ? 1 : kind == SailOrderKind.GivenAP ? 2 : 3,
                a = Models[model, 0] * scale,
                b = Models[model, 1] * scale,
                c = Models[model, 2] * scale,
                scale = scale,
                k = k
            };
            FillLengths(p);
            FillDistractors(p);
            FillPrompt(p);
            return p;
        }

        static void FillLengths(SailProblem p)
        {
            p.ab = 8 * p.a;
            p.ac = 8 * p.b;
            p.bc = 8 * p.c;
            p.ap = p.k * p.a;
            p.aq = p.k * p.b;
        }

        static void FillPrompt(SailProblem p)
        {
            int rn, rd;
            Reduce(p.k, 8, out rn, out rd);
            int an, ad;
            Reduce(p.k * p.k, 64, out an, out ad);
            // BC도 같은 모델에서 표기해 (a,b가 같고 c만 다른) 두 원단의 핵심 발문이 겹치지 않게 한다.
            string size = "AB=" + p.ab + " cm, AC=" + p.ac + " cm, BC=" + p.bc + " cm이다. ";
            if (p.kind == SailOrderKind.SimilarityRatio)
            {
                p.concept = "닮음비와 SAS 닮음";
                p.prompt = size + "△APQ와 △ABC의 닮음비가 " + rn + ":" + rd + "가 되도록 자르시오.";
            }
            else if (p.kind == SailOrderKind.GivenAP)
            {
                p.concept = "대응변의 길이와 SAS 닮음";
                p.prompt = size + "AP=" + p.ap + " cm이고 △APQ∽△ABC가 되도록 자르시오.";
            }
            else if (p.kind == SailOrderKind.AreaRatio)
            {
                p.concept = "닮은 평면도형의 넓이의 비";
                p.prompt = size + "△APQ와 △ABC의 넓이의 비가 " + an + ":" + ad + "가 되도록 자르시오.";
            }
            else
            {
                p.concept = "잘못된 절단선 고치기";
                p.prompt = size + "기존 절단은 AP=" + (p.wrong1I * p.a) + " cm, AQ=" + (p.wrong1J * p.b)
                    + " cm이다. 닮음비가 " + rn + ":" + rd + "가 되도록 잘못된 절단선을 고치시오.";
            }
        }

        // distractor_rules의 두 오개념을 모든 문항 데이터에 역산해 남긴다.
        // choices는 없는 open_construct지만, 실제 오입력 분류와 오류 찾기 주문이 이 값을 사용한다.
        static void FillDistractors(SailProblem p)
        {
            p.wrong1I = 8 - p.k;
            p.wrong1J = 8 - p.k;
            if (p.wrong1I == p.k) { p.wrong1I = 3; p.wrong1J = 3; }
            p.misconception1 = "ratio-from-bc"; // A가 아니라 B·C 쪽에서 비를 잰다.

            int ei, ej;
            if (TryEqualCentimeters(p, out ei, out ej))
            {
                p.wrong2I = ei; p.wrong2J = ej;
                p.misconception2 = "same-centimeters"; // 두 변에 같은 cm를 남긴다.
            }
            else
            {
                int q = Math.Max(1, Math.Min(7, (p.k * p.k + 4) / 8));
                if (q == p.k) q = p.k == 7 ? 6 : p.k + 1;
                p.wrong2I = q; p.wrong2J = q;
                p.misconception2 = p.kind == SailOrderKind.AreaRatio ? "area-ratio-linear" : "parallel-wrong-ratio";
            }

            // 오답 후처리: 정답 제거 → 범위 검사 → 중복 제거 → 부족분 보충.
            // 같은 cm 오답과 반대 꼭짓점 오답이 같은 (i,j)가 되는 매개변수에서도 두 근거를 한 좌표로 겹치지 않는다.
            if (p.IsCorrect(p.wrong2I, p.wrong2J)
                || (p.wrong1I == p.wrong2I && p.wrong1J == p.wrong2J))
            {
                for (int i = 1; i <= 7; i++)
                for (int j = 1; j <= 7; j++)
                    if (!p.IsCorrect(i, j) && (i != p.wrong1I || j != p.wrong1J) && i != j)
                    {
                        p.wrong2I = i; p.wrong2J = j; p.misconception2 = "unequal-side-ratios";
                        return;
                    }
            }
        }

        static bool TryEqualCentimeters(SailProblem p, out int wi, out int wj)
        {
            for (int i = 1; i <= 7; i++)
            for (int j = 1; j <= 7; j++)
                if (i != j && i * p.a == j * p.b && !p.IsCorrect(i, j)) { wi = i; wj = j; return true; }
            wi = wj = 1;
            return false;
        }

        public static string MisconceptionFor(SailProblem p, int i, int j)
        {
            if (p.IsCorrect(i, j)) return "";
            if (i == p.wrong1I && j == p.wrong1J) return p.misconception1;
            if (i == p.wrong2I && j == p.wrong2J) return p.misconception2;
            if (i * p.a == j * p.b) return "same-centimeters";
            if (i == j) return "parallel-wrong-ratio";
            return "unequal-side-ratios";
        }

        public static string WrongExplanation(SailProblem p, int i, int j)
        {
            string id = MisconceptionFor(p, i, j);
            if (id == "ratio-from-bc") return "A부터 재시오. 버리는 쪽의 비가 아니라 남길 돛의 비를 맞추어야 한다.";
            if (id == "same-centimeters") return "같은 cm가 아니라 두 전체 변에서 같은 비율을 남겨야 한다.";
            if (id == "area-ratio-linear") return "넓이의 비는 닮음비의 제곱이다. 길이의 비로 바꾸어 다시 자르시오.";
            if (id == "parallel-wrong-ratio") return "두 비는 같지만 주문한 크기가 아니다. 주문의 닮음비를 다시 확인하시오.";
            return "AP:AB와 AQ:AC가 다르다. 두 대응변의 비를 같게 맞추시오.";
        }

        public static string Evidence(SailProblem p)
        {
            int rn, rd; Reduce(p.k, 8, out rn, out rd);
            return p.ap + ":" + p.ab + " = " + p.aq + ":" + p.ac + " = " + rn + ":" + rd
                + "  ·  ∠PAQ=∠BAC  →  SAS 닮음";
        }

        public static string RatioText(int part, int whole)
        {
            int n, d; Reduce(part, whole, out n, out d);
            return n + ":" + d;
        }

        public static void Reduce(int n, int d, out int rn, out int rd)
        {
            int a = Math.Abs(n), b = Math.Abs(d);
            while (b != 0) { int t = a % b; a = b; b = t; }
            int g = Math.Max(1, a);
            rn = n / g; rd = d / g;
        }

        public static bool ValidateCatalog(List<SailProblem> catalog, out string error)
        {
            var ids = new HashSet<string>();
            var prompts = new HashSet<string>();
            for (int pIndex = 0; pIndex < catalog.Count; pIndex++)
            {
                SailProblem p = catalog[pIndex];
                if (!ids.Add(p.id)) { error = "중복 id " + p.id; return false; }
                if (!prompts.Add(p.prompt)) { error = "중복 발문 " + p.prompt; return false; }
                if (!(p.a + p.b > p.c && p.b + p.c > p.a && p.c + p.a > p.b))
                { error = "삼각형 부등식 " + p.id; return false; }
                int answers = 0;
                for (int i = 1; i <= 7; i++)
                for (int j = 1; j <= 7; j++) if (p.IsCorrect(i, j)) answers++;
                if (answers != 1 || !p.IsCorrect(p.k, p.k)) { error = "유일 정답 " + p.id; return false; }
                if (p.IsCorrect(p.wrong1I, p.wrong1J) || p.IsCorrect(p.wrong2I, p.wrong2J))
                { error = "오개념 우연 정답 " + p.id; return false; }
                if (p.wrong1I == p.wrong2I && p.wrong1J == p.wrong2J)
                { error = "오개념 중복 " + p.id; return false; }
                if (p.prompt.Contains("√") || p.prompt.Contains("가정") || p.prompt.Contains("결론"))
                { error = "중2 금지 표현 " + p.id; return false; }
            }
            error = "";
            return catalog.Count >= 300 && prompts.Count * 10 >= catalog.Count * 7;
        }
    }
}
