// 비누 썰어 — 문제 생성·정수 판정 정본.
// 중학교 style_guide: 지시형 「~이 되도록 자르시오」, 수와 단위 사이 띄어쓰기.
// expression_traps: 원기둥의 π는 근삿값으로 바꾸지 않고 정수 계수로만 계산한다.
using System;
using System.Collections.Generic;
using Mgf;

namespace Mgf.BinuSseoreo
{
    public enum SolidKind { RectPrism, Cylinder }
    public enum PieceKind { Lower, Upper }

    [Serializable]
    public sealed class SoapProblem
    {
        public string id;
        public SolidKind solid;
        public PieceKind piece;
        public int a;
        public int b;
        public int radius;
        public int answerHeight;
        public int pieceHeight;
        public int baseCoefficient;
        public int targetCoefficient;
        public int band;
        public string prompt;
        public string answer;
        public string unitConcept;
    }

    public static class BinuRules
    {
        public const int TotalHeight = 25;
        public const int StartLives = 3;
        public const int TargetSolved = 8;
        public const int MasteryNeeded = 6;
        public const float SessionSeconds = 120f;

        public static SoapProblem MakeRect(int a, int b, int cutHeight, PieceKind piece, string prefix = "rect")
        {
            if (a > b) { int t = a; a = b; b = t; }
            if (a < 2 || b > 5 || cutHeight < 1 || cutHeight > 24) throw new ArgumentOutOfRangeException();
            int pieceHeight = piece == PieceKind.Lower ? cutHeight : TotalHeight - cutHeight;
            int area = a * b;
            int volume = area * pieceHeight;
            string which = piece == PieceKind.Lower ? "아래 조각" : "위쪽 조각";
            string prompt = "밑면은 가로 " + a + " cm, 세로 " + b +
                " cm인 직사각형이고, 전체 높이는 25 cm인 사각기둥입니다. " + which + "의 부피가 " + volume +
                " cm³가 되도록 바닥에서 몇 cm 높이에서 자르시오.";
            return new SoapProblem {
                id = prefix + "-a" + a + "-b" + b + "-h" + cutHeight + "-" + (piece == PieceKind.Lower ? "lo" : "up"),
                solid = SolidKind.RectPrism, piece = piece, a = a, b = b, radius = 0,
                answerHeight = cutHeight, pieceHeight = pieceHeight, baseCoefficient = area,
                targetCoefficient = volume, band = piece == PieceKind.Upper ? 3 : 1,
                prompt = prompt, answer = cutHeight + " cm", unitConcept = piece == PieceKind.Lower
                    ? "각기둥의 부피와 필요한 높이" : "전체 높이에서 위쪽 조각의 높이 역추적"
            };
        }

        public static SoapProblem MakeCylinder(int radius, int cutHeight, PieceKind piece, string prefix = "cyl")
        {
            if ((radius != 1 && radius != 3 && radius != 4) || cutHeight < 1 || cutHeight > 24)
                throw new ArgumentOutOfRangeException();
            int pieceHeight = piece == PieceKind.Lower ? cutHeight : TotalHeight - cutHeight;
            int areaPi = radius * radius;
            int volumePi = areaPi * pieceHeight;
            string which = piece == PieceKind.Lower ? "아래 조각" : "위쪽 조각";
            string prompt = "전체 높이가 25 cm이고 밑면의 반지름의 길이가 " + radius +
                " cm인 원기둥입니다. " + which + "의 부피가 " + Pi(volumePi) +
                " cm³가 되도록 바닥에서 몇 cm 높이에서 자르시오.";
            return new SoapProblem {
                id = prefix + "-r" + radius + "-h" + cutHeight + "-" + (piece == PieceKind.Lower ? "lo" : "up"),
                solid = SolidKind.Cylinder, piece = piece, a = 0, b = 0, radius = radius,
                answerHeight = cutHeight, pieceHeight = pieceHeight, baseCoefficient = areaPi,
                targetCoefficient = volumePi, band = piece == PieceKind.Upper ? 3 : 2,
                prompt = prompt, answer = cutHeight + " cm", unitConcept = piece == PieceKind.Lower
                    ? "원기둥의 부피와 반지름" : "전체 높이에서 위쪽 원기둥의 높이 역추적"
            };
        }

        public static SoapProblem Practice() => MakeRect(2, 3, 4, PieceKind.Lower, "practice");

        public static List<SoapProblem> BuildPool()
        {
            var pool = new List<SoapProblem>(624);
            for (int a = 2; a <= 5; a++)
            for (int b = a; b <= 5; b++)
            for (int h = 1; h <= 24; h++)
            {
                pool.Add(MakeRect(a, b, h, PieceKind.Lower));
                pool.Add(MakeRect(a, b, h, PieceKind.Upper));
            }
            int[] radii = { 1, 3, 4 };
            for (int i = 0; i < radii.Length; i++)
            for (int h = 1; h <= 24; h++)
            {
                pool.Add(MakeCylinder(radii[i], h, PieceKind.Lower));
                pool.Add(MakeCylinder(radii[i], h, PieceKind.Upper));
            }
            ValidatePool(pool);
            return pool;
        }

        public static List<MgfProblem> BuildBank()
        {
            var pool = BuildPool();
            var bank = new List<MgfProblem>(pool.Count);
            for (int i = 0; i < pool.Count; i++)
            {
                SoapProblem p = pool[i];
                bank.Add(new MgfProblem {
                    id = p.id, prompt = p.prompt, choices = null, answer = p.answer,
                    answerNumeric = p.answerHeight, unitConcept = p.unitConcept
                });
            }
            return bank;
        }

        public static List<SoapProblem> BuildDeck(int serial, bool deterministic = false)
        {
            int seed = deterministic ? unchecked(0x517A21 + serial * 104729) : unchecked(Environment.TickCount ^ serial * 104729 ^ 0x6F4A8E);
            var rng = new Random(seed);
            var deck = new List<SoapProblem>(TargetSolved);
            var used = new HashSet<string>();
            for (int round = 0; round < TargetSolved; round++)
            {
                SoapProblem p;
                do
                {
                    int h = 1 + rng.Next(24); // 제약 만족 풀: 합법 눈금부터 균등 선택한다.
                    if (round < 3)
                    {
                        int a = 2 + rng.Next(4); int b = a + rng.Next(6 - a);
                        p = MakeRect(a, b, h, PieceKind.Lower, "run" + serial + "-q" + round);
                    }
                    else if (round < 6)
                    {
                        int[] radii = { 1, 3, 4 };
                        p = MakeCylinder(radii[rng.Next(radii.Length)], h, PieceKind.Lower, "run" + serial + "-q" + round);
                    }
                    else if (round == 6)
                    {
                        int a = 2 + rng.Next(4); int b = a + rng.Next(6 - a);
                        p = MakeRect(a, b, h, PieceKind.Upper, "run" + serial + "-q" + round);
                    }
                    else
                    {
                        int[] radii = { 1, 3, 4 };
                        p = MakeCylinder(radii[rng.Next(radii.Length)], h, PieceKind.Upper, "run" + serial + "-q" + round);
                    }
                } while (!used.Add(p.id));
                deck.Add(p);
            }
            return deck;
        }

        public static bool IsCorrect(SoapProblem p, int selectedHeight)
        {
            if (selectedHeight < 1 || selectedHeight > 24) return false;
            int pieceH = p.piece == PieceKind.Lower ? selectedHeight : TotalHeight - selectedHeight;
            return p.baseCoefficient * pieceH == p.targetCoefficient;
        }

        public static string MisconceptionId(SoapProblem p, int selectedHeight)
        {
            if (selectedHeight == p.answerHeight) return "";
            if (p.piece == PieceKind.Upper && selectedHeight == p.pieceHeight)
                return "upper_piece_height_as_cut";
            if (p.solid == SolidKind.RectPrism && p.piece == PieceKind.Lower)
            {
                int sum = p.a + p.b;
                if (p.targetCoefficient % sum == 0 && selectedHeight == p.targetCoefficient / sum)
                    return "rectangle_side_sum_as_area";
            }
            if (p.solid == SolidKind.Cylinder && p.piece == PieceKind.Lower)
            {
                if (p.targetCoefficient % p.radius == 0 && selectedHeight == p.targetCoefficient / p.radius)
                    return "radius_not_squared";
                int circumferenceCoefficient = 2 * p.radius;
                if (p.targetCoefficient % circumferenceCoefficient == 0 && selectedHeight == p.targetCoefficient / circumferenceCoefficient)
                    return "circumference_as_base_area";
            }
            return "volume_height_mismatch";
        }

        public static string Pi(int coefficient) => coefficient == 1 ? "π" : coefficient + "π";

        static void ValidatePool(List<SoapProblem> pool)
        {
            if (pool.Count != 624) throw new InvalidOperationException("문제 은행은 624개여야 합니다.");
            var ids = new HashSet<string>();
            var prompts = new HashSet<string>();
            for (int i = 0; i < pool.Count; i++)
            {
                SoapProblem p = pool[i];
                if (!ids.Add(p.id) || !prompts.Add(p.prompt)) throw new InvalidOperationException("중복 문항: " + p.id);
                int solutions = 0;
                for (int h = 1; h <= 24; h++) if (IsCorrect(p, h)) solutions++;
                if (solutions != 1 || !IsCorrect(p, p.answerHeight)) throw new InvalidOperationException("유일해 오류: " + p.id);
                if (p.prompt.Contains("자리에서") || p.prompt.Contains("√")) throw new InvalidOperationException("금지 표현: " + p.id);
                if (!p.prompt.Contains(" cm") || !p.prompt.EndsWith("자르시오.")) throw new InvalidOperationException("발문 형식 오류: " + p.id);
                if (!p.prompt.Contains("전체 높이") || !p.prompt.Contains("25 cm") || !p.prompt.Contains("바닥에서 몇 cm 높이"))
                    throw new InvalidOperationException("절단 기준 누락: " + p.id);
                if (p.solid == SolidKind.RectPrism && (!p.prompt.Contains("직사각형이고") ||
                    !p.prompt.Contains("가로 ") || !p.prompt.Contains("세로 ")))
                    throw new InvalidOperationException("사각기둥 밑면 조건 누락: " + p.id);
                // 오개념 역산 전수 검사: 어떤 규칙도 정답 높이를 오답으로 분류할 수 없다.
                for (int h = 1; h <= 24; h++)
                {
                    string id = MisconceptionId(p, h);
                    if (h == p.answerHeight && id.Length != 0) throw new InvalidOperationException("오개념 우연 참: " + p.id + "/" + id);
                    if (h != p.answerHeight && IsCorrect(p, h)) throw new InvalidOperationException("다중 정답: " + p.id);
                }
            }
        }
    }
}
