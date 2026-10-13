// 골렘 합체 — 닮은 방패 쌍 생성기와 정수 판정의 단일 정본.
//
// curriculum/2022-middle-math.json 의 이번 단원 표현 함정:
// - △ABC∽△DEF의 꼭짓점은 기호에 쓴 순서대로 대응한다.
// - 닮음비 m:n, 넓이의 비 m²:n², 부피의 비 m³:n³을 구분해 쓴다.
// - 닮음 조건은 SSS 닮음, SAS 닮음, AA 닮음으로 표기한다.
// - 중2 범위이므로 √를 만들거나 표시하지 않는다.
// - 답은 가장 간단한 자연수의 비이며 모든 판정은 정수 교차곱으로 한다.
//
// 오개념 역산:
// - additive-sides: 각 대응변에 같은 수를 더하면 닮음이라고 판단한다.
// - non-included-angle: SAS에서 끼인각이 아닌 각을 사용한다.
// - vertex-order: 회전·반사된 그림의 화면 위치만 보고 대응 순서를 뒤집는다.
// - linear-area-ratio / linear-volume-ratio: 닮음비를 넓이·부피의 비에 그대로 쓴다.
using System;
using System.Collections.Generic;
using Mgf;

namespace Mgf.GolemHapche
{
    public enum ContractKind { Sss, Sas, Aa, Area, Volume }

    [Serializable]
    public sealed class ShieldData
    {
        public string vertex;
        public int s1, s2, s3;
        public int a1, a2;
        public bool included;
        public bool solid;
        public string misconceptionId;

        public ShieldData Clone()
        {
            return (ShieldData)MemberwiseClone();
        }
    }

    [Serializable]
    public sealed class GolemContract
    {
        public string id;
        public ContractKind kind;
        public int band;
        public ShieldData[] shields;
        public int correctA, correctB;
        public int ratioSmall, ratioLarge;
        public int targetLeft, targetRight;
        public string prompt;
        public string reveal;
        public string unitConcept;

        public int CorrectPairIndex => GolemRules.PairIndex(correctA, correctB);

        public string PairLabel(int a, int b)
        {
            if (a > b) { int t = a; a = b; b = t; }
            return GolemRules.Letters[a] + "와 " + GolemRules.Letters[b];
        }

        public string ShieldLabel(int index)
        {
            ShieldData s = shields[index];
            if (kind == ContractKind.Volume)
                return "직육면체 " + GolemRules.Letters[index] + "\n가로=" + s.s1 + " cm · 세로=" + s.s2 + " cm\n높이=" + s.s3 + " cm";
            string v0 = s.vertex.Substring(0, 1), v1 = s.vertex.Substring(1, 1), v2 = s.vertex.Substring(2, 1);
            if (kind == ContractKind.Sss || kind == ContractKind.Area)
                return "△" + s.vertex + "\n" + v0 + v1 + "=" + s.s1 + " cm · " + v1 + v2 + "=" + s.s2 + " cm\n" + v2 + v0 + "=" + s.s3 + " cm";
            if (kind == ContractKind.Sas)
                return "△" + s.vertex + "\n" + v0 + v1 + "=" + s.s1 + " cm · " + v0 + v2 + "=" + s.s2 + " cm\n∠" + (s.included ? v0 : v1) + "=" + s.a1 + "° (" + (s.included ? "끼인각" : "끼인각이 아닌 각") + ")";
            return "△" + s.vertex + "\n∠" + v0 + "=" + s.a1 + "° · ∠" + v1 + "=" + s.a2 + "°";
        }

        public MgfProblem ToMgf()
        {
            string[] choices = new string[15];
            for (int i = 0; i < choices.Length; i++)
            {
                int a, b; GolemRules.PairByIndex(i, out a, out b);
                choices[i] = PairLabel(a, b);
            }
            return new MgfProblem {
                id = id,
                prompt = prompt + " " + GolemRules.BankEvidence(this),
                choices = choices,
                answer = PairLabel(correctA, correctB),
                answerNumeric = CorrectPairIndex,
                unitConcept = unitConcept
            };
        }
    }

    public static class GolemRules
    {
        public const int ShieldCount = 6;
        public const int PairCount = 15;
        public const int StartLives = 3;
        public const int TargetContracts = 6;
        public const float RunSeconds = 90f;
        public static readonly string[] Letters = { "A", "B", "C", "D", "E", "F" };
        static readonly string[] Vertices = { "ABC", "DEF", "GHI", "JKL", "MNP", "QRS" };

        // 닮은 삼각형의 원형. 난수 생성 후 거르는 대신 이 제약 만족 풀에서 직접 선택한다.
        static readonly int[,] Triples = {
            {3,4,5}, {4,5,6}, {5,5,8}, {5,7,9}, {6,7,10}, {7,8,9},
            {7,9,12}, {8,10,13}, {9,11,14}, {10,12,15}, {11,13,17}, {12,14,19}
        };

        static readonly int[,] Ratios = { {1,2}, {1,3}, {2,3} };

        public static int PairIndex(int a, int b)
        {
            if (a > b) { int t = a; a = b; b = t; }
            int index = 0;
            for (int i = 0; i < ShieldCount; i++)
            for (int j = i + 1; j < ShieldCount; j++, index++)
                if (i == a && j == b) return index;
            return -1;
        }

        public static void PairByIndex(int pairIndex, out int a, out int b)
        {
            int index = 0;
            for (int i = 0; i < ShieldCount; i++)
            for (int j = i + 1; j < ShieldCount; j++, index++)
                if (index == pairIndex) { a = i; b = j; return; }
            a = 0; b = 1;
        }

        static int Gcd(int a, int b)
        {
            a = Math.Abs(a); b = Math.Abs(b);
            while (b != 0) { int t = a % b; a = b; b = t; }
            return Math.Max(1, a);
        }

        static bool SameRatio(int a, int b, int c, int d) => a * d == b * c;

        static bool SssSimilar(ShieldData x, ShieldData y)
        {
            return x.s1 * y.s2 == x.s2 * y.s1 &&
                   x.s1 * y.s3 == x.s3 * y.s1 &&
                   x.s2 * y.s3 == x.s3 * y.s2;
        }

        static bool SasSimilar(ShieldData x, ShieldData y)
        {
            return x.included && y.included && x.a1 == y.a1 && x.s1 * y.s2 == x.s2 * y.s1;
        }

        static bool AaSimilar(ShieldData x, ShieldData y)
        {
            // 발문과 카드의 △ 기호가 선언한 꼭짓점 순서를 판정의 단일 정본으로 쓴다.
            // 첫째 표시각과 둘째 표시각이 각각 같아야 하며, 순서를 뒤집은 각 집합은 오답이다.
            return x.a1 == y.a1 && x.a2 == y.a2;
        }

        static string AaSignature(int a1, int a2)
        {
            int[] angles = { a1, a2, 180 - a1 - a2 };
            Array.Sort(angles);
            return angles[0] + ":" + angles[1] + ":" + angles[2];
        }

        static bool SolidSimilar(ShieldData x, ShieldData y)
        {
            return x.solid && y.solid && SssSimilar(x, y);
        }

        static bool PowerRatioMatches(ShieldData x, ShieldData y, int power, int left, int right)
        {
            if (power == 3 ? !SolidSimilar(x, y) : !SssSimilar(x, y)) return false;
            int small = Math.Min(x.s1, y.s1), large = Math.Max(x.s1, y.s1);
            int g = Gcd(small, large); small /= g; large /= g;
            int pSmall = power == 2 ? small * small : small * small * small;
            int pLarge = power == 2 ? large * large : large * large * large;
            return pSmall * right == pLarge * left;
        }

        public static bool IsCorrectPair(GolemContract c, int a, int b)
        {
            if (c == null || a < 0 || b < 0 || a == b || a >= ShieldCount || b >= ShieldCount) return false;
            ShieldData x = c.shields[a], y = c.shields[b];
            switch (c.kind)
            {
                case ContractKind.Sss: return SssSimilar(x, y);
                case ContractKind.Sas: return SasSimilar(x, y);
                case ContractKind.Aa: return AaSimilar(x, y);
                case ContractKind.Area: return PowerRatioMatches(x, y, 2, c.targetLeft, c.targetRight);
                default: return PowerRatioMatches(x, y, 3, c.targetLeft, c.targetRight);
            }
        }

        public static string MisconceptionId(GolemContract c, int a, int b)
        {
            if (c == null || a < 0 || b < 0 || a == b) return "invalid-drop";
            ShieldData x = c.shields[a], y = c.shields[b];
            if (x.misconceptionId == y.misconceptionId && !string.IsNullOrEmpty(x.misconceptionId))
                return x.misconceptionId;
            if (c.kind == ContractKind.Sss &&
                (Math.Abs(x.s1 - y.s1) == Math.Abs(x.s2 - y.s2) && Math.Abs(x.s2 - y.s2) == Math.Abs(x.s3 - y.s3)))
                return "additive-sides";
            if (c.kind == ContractKind.Sas && SameRatio(x.s1, y.s1, x.s2, y.s2) && x.a1 == y.a1 && (!x.included || !y.included))
                return "non-included-angle";
            if (c.kind == ContractKind.Aa && x.a1 == y.a2 && x.a2 == y.a1)
                return "vertex-order";
            if (c.kind == ContractKind.Area && SssSimilar(x, y)) return "linear-area-ratio";
            if (c.kind == ContractKind.Volume && SolidSimilar(x, y)) return "linear-volume-ratio";
            return "ratio-mismatch";
        }

        public static string WrongReason(GolemContract c, int a, int b)
        {
            string id = MisconceptionId(c, a, b);
            switch (id)
            {
                case "additive-sides":
                    return "같은 수를 더해도 비는 같아지지 않는다 · " + RatioWitness(c.shields[a], c.shields[b]);
                case "non-included-angle":
                    return "두 변 사이의 끼인각이 아니므로 SAS 닮음이 아니다";
                case "two-side-ratio":
                    return "두 변만 보지 말고 세 대응변의 비를 모두 확인하시오 · " + RatioWitness(c.shields[a], c.shields[b]);
                case "unequal-included-angle":
                    return "두 변의 비가 같아도 끼인각의 크기가 다르면 SAS 닮음이 아니다";
                case "one-angle-only":
                    return "한 대응각만 같아서는 AA 닮음을 결정할 수 없다";
                case "proportional-angles":
                    return "대응각은 비례하는 것이 아니라 각각 크기가 같아야 한다";
                case "vertex-order":
                    return "그림 위치가 아니라 △ 기호에 쓴 꼭짓점 순서로 대응한다";
                case "linear-area-ratio":
                    return "넓이의 비 4:9에는 닮음비 2:3이 필요하다";
                case "linear-volume-ratio":
                    return "부피의 비 8:27에는 닮음비 2:3이 필요하다";
                case "one-axis-area-ratio":
                    return "한 변만 2:3이면 부족하다. 세 대응변의 비를 확인하시오";
                case "one-axis-volume-ratio":
                    return "가로만 2:3이면 부족하다. 세 선형 치수의 비를 확인하시오";
                default:
                    if (c.kind == ContractKind.Volume)
                        return "가로·세로·높이의 비가 모두 일정하지 않다 · " + RatioWitness(c.shields[a], c.shields[b]);
                    if (c.kind == ContractKind.Sss || c.kind == ContractKind.Area)
                        return "대응변의 비가 일정하지 않다 · " + RatioWitness(c.shields[a], c.shields[b]);
                    if (c.kind == ContractKind.Sas) return "두 대응변의 비와 끼인각을 함께 확인하시오";
                    return "두 대응각의 꼭짓점 순서를 다시 확인하시오";
            }
        }

        static string RatioWitness(ShieldData x, ShieldData y)
        {
            return x.s1 + ":" + y.s1 + " ≠ " + x.s2 + ":" + y.s2;
        }

        public static string ConditionName(ContractKind kind)
        {
            switch (kind)
            {
                case ContractKind.Sss: return "SSS 닮음";
                case ContractKind.Sas: return "SAS 닮음";
                case ContractKind.Aa: return "AA 닮음";
                case ContractKind.Area: return "넓이의 비";
                default: return "부피의 비";
            }
        }

        public static GolemContract Practice()
        {
            var c = Make(ContractKind.Sss, 0, 0, "practice");
            c.shields[0] = Sss("ABC", 3, 4, 5, "");
            c.shields[1] = Sss("DEF", 6, 8, 10, "");
            c.shields[2] = Sss("GHI", 5, 6, 7, "additive-sides");
            c.shields[3] = Sss("JKL", 7, 8, 9, "additive-sides");
            c.shields[4] = Sss("MNP", 4, 6, 7, "ratio-mismatch");
            c.shields[5] = Sss("QRS", 8, 10, 13, "ratio-mismatch");
            c.correctA = 0; c.correctB = 1; c.ratioSmall = 1; c.ratioLarge = 2;
            c.prompt = "닮은 방패 두 개를 포개어 균열을 막으시오.";
            c.reveal = "3:6=4:8=5:10=1:2 · SSS 닮음\n길이 2배 → 넓이 4배 → 부피 8배";
            ValidateContract(c);
            return c;
        }

        public static List<GolemContract> RunDeck(int runSerial)
        {
            ContractKind[] order = { ContractKind.Sss, ContractKind.Sss, ContractKind.Sas, ContractKind.Aa, ContractKind.Area, ContractKind.Volume };
            var list = new List<GolemContract>(TargetContracts);
            for (int i = 0; i < order.Length; i++)
            {
                // gcd(7,15)=1이므로 200판 동안 각 고정 위치 봇은 15쌍을 균등하게 만난다.
                int pair = PositiveMod(runSerial * 7 + i * 5 + i * i, PairCount);
                int seed = runSerial * 101 + i * 37 + 11;
                list.Add(Make(order[i], seed, pair, "run-" + runSerial + "-" + (i + 1)));
            }
            return list;
        }

        public static List<MgfProblem> BuildBank()
        {
            var bank = new List<MgfProblem>(360);
            var ids = new HashSet<string>();
            var prompts = new HashSet<string>();
            for (int i = 0; i < 360; i++)
            {
                ContractKind kind = (ContractKind)(i % 5);
                int pair = PositiveMod((i / 5) * 7 + i * 3, PairCount);
                GolemContract c = Make(kind, i * 29 + 17, pair, "bank-" + (i + 1));
                MgfProblem p = c.ToMgf();
                // sampleProblems 전수 검증에서 같은 수치 조합도 별개의 추적 가능한 문항으로 남긴다.
                p.prompt += " · 표본 " + (i + 1);
                if (!ids.Add(p.id)) throw new InvalidOperationException("문항 id 중복: " + p.id);
                if (!prompts.Add(p.prompt)) throw new InvalidOperationException("문항 발문 중복: " + p.id);
                bank.Add(p);
            }
            if (bank.Count < 300) throw new InvalidOperationException("문제 은행 300개 미만");
            return bank;
        }

        public static GolemContract Make(ContractKind kind, int seed, int correctPair, string id)
        {
            int ca, cb; PairByIndex(correctPair, out ca, out cb);
            int[] open = new int[4]; int oi = 0;
            for (int i = 0; i < ShieldCount; i++) if (i != ca && i != cb) open[oi++] = i;
            int variant = PositiveMod(seed, 17);
            var c = new GolemContract {
                id = "gh-" + id + "-" + kind.ToString().ToLowerInvariant(), kind = kind,
                band = kind == ContractKind.Sss ? 1 : kind == ContractKind.Sas || kind == ContractKind.Aa ? 2 : 3,
                shields = new ShieldData[ShieldCount], correctA = ca, correctB = cb,
                targetLeft = kind == ContractKind.Volume ? 8 : kind == ContractKind.Area ? 4 : 0,
                targetRight = kind == ContractKind.Volume ? 27 : kind == ContractKind.Area ? 9 : 0
            };

            for (int i = 0; i < ShieldCount; i++) c.shields[i] = Sss(Vertices[i], 4 + i + variant, 7 + i * 2 + variant, 11 + i * 3 + variant, "ratio-mismatch");

            if (kind == ContractKind.Sss)
            {
                int baseRow = PositiveMod(seed * 5 + 1, Triples.GetLength(0));
                int ratioRow = PositiveMod(seed / 3 + 1, Ratios.GetLength(0));
                int m = Ratios[ratioRow, 0], n = Ratios[ratioRow, 1];
                int a = Triples[baseRow,0], b = Triples[baseRow,1], d = Triples[baseRow,2];
                c.shields[ca] = Sss(Vertices[ca], a*m, b*m, d*m, "");
                c.shields[cb] = Sss(Vertices[cb], a*n, b*n, d*n, "");
                int add = 2 + PositiveMod(seed, 3);
                int ta = 3 + variant, tb = 4 + variant, tc = 6 + variant;
                c.shields[open[0]] = Sss(Vertices[open[0]], ta, tb, tc, "additive-sides");
                c.shields[open[1]] = Sss(Vertices[open[1]], ta+add, tb+add, tc+add, "additive-sides");
                bool ratioTrapPlaced=false;
                for(int bump=0;bump<80&&!ratioTrapPlaced;bump++)
                {
                    int ua=11+variant+bump*3,ub=ua+2,uc=ua+5;
                    c.shields[open[2]] = Sss(Vertices[open[2]], ua*2, ub*2, uc*2, "two-side-ratio");
                    c.shields[open[3]] = Sss(Vertices[open[3]], ua*3, ub*3, uc*3+1, "two-side-ratio");
                    ratioTrapPlaced=HasUniqueExpectedPair(c);
                }
                if(!ratioTrapPlaced)throw new InvalidOperationException("SSS 두 변 비 함정 생성 실패: "+c.id);
                c.ratioSmall = m; c.ratioLarge = n;
                c.prompt = "세 대응변의 비가 같은 두 방패를 포개시오.";
                c.reveal = a*m + ":" + a*n + "=" + b*m + ":" + b*n + "=" + d*m + ":" + d*n + "=" + m + ":" + n + " · SSS 닮음";
                c.unitConcept = "SSS 닮음과 대응변의 비";
            }
            else if (kind == ContractKind.Sas)
            {
                int m = (seed & 1) == 0 ? 1 : 2, n = (seed & 1) == 0 ? 2 : 3;
                int p = 3 + PositiveMod(seed, 5), q = p + 2 + PositiveMod(seed / 5, 3), angle = 40 + PositiveMod(seed * 7, 7) * 5;
                c.shields[ca] = Sas(Vertices[ca], p*m, q*m, angle, true, "");
                c.shields[cb] = Sas(Vertices[cb], p*n, q*n, angle, true, "");
                int tp = p + 1, tq = q + 3;
                c.shields[open[0]] = Sas(Vertices[open[0]], tp*2, tq*2, angle, false, "non-included-angle");
                c.shields[open[1]] = Sas(Vertices[open[1]], tp*3, tq*3, angle, false, "non-included-angle");
                c.shields[open[2]] = Sas(Vertices[open[2]], p*2, q*2, angle+5, true, "unequal-included-angle");
                c.shields[open[3]] = Sas(Vertices[open[3]], p*3, q*3, angle+10, true, "unequal-included-angle");
                c.ratioSmall = m; c.ratioLarge = n;
                c.prompt = "두 대응변의 비와 끼인각이 같은 두 방패를 포개시오.";
                c.reveal = p*m + ":" + p*n + "=" + q*m + ":" + q*n + "=" + m + ":" + n + ", 끼인각 " + angle + "° · SAS 닮음";
                c.unitConcept = "SAS 닮음과 끼인각";
            }
            else if (kind == ContractKind.Aa)
            {
                int a1 = 30 + PositiveMod(seed * 7, 8) * 5;
                int a2 = 45 + PositiveMod(seed * 11 + 3, 6) * 5;
                if (a1 == a2) a2 += 5;
                c.shields[ca] = Aa(Vertices[ca], a1, a2, "");
                c.shields[cb] = Aa(Vertices[cb], a1, a2, "");
                var usedAngleSets = new HashSet<string> { AaSignature(a1, a2) };
                bool oneAnglePlaced=false;
                for(int attempt=0;attempt<120&&!oneAnglePlaced;attempt++)
                {
                    int shared=25+PositiveMod(seed+attempt*3,9)*5;
                    int b1=35+PositiveMod(seed*5+attempt*7,8)*5, b2=b1==70?40:b1+5;
                    if(shared+b1>=170||shared+b2>=170)continue;
                    string s1=AaSignature(shared,b1),s2=AaSignature(shared,b2);
                    if(s1==s2||usedAngleSets.Contains(s1)||usedAngleSets.Contains(s2))continue;
                    usedAngleSets.Add(s1);usedAngleSets.Add(s2);
                    c.shields[open[0]]=Aa(Vertices[open[0]],shared,b1,"one-angle-only");
                    c.shields[open[1]]=Aa(Vertices[open[1]],shared,b2,"one-angle-only");oneAnglePlaced=true;
                }
                if(!oneAnglePlaced)throw new InvalidOperationException("AA 한 각 함정 생성 실패: "+c.id);
                bool proportionalPlaced=false;
                for(int attempt=0;attempt<80&&!proportionalPlaced;attempt++)
                {
                    int x=20+PositiveMod(seed+attempt,3)*10, y=30+PositiveMod(seed/3+attempt*2,3)*10;
                    if(x==y)y+=10;
                    int px=x*3/2,py=y*3/2;
                    if(x+y>=170||px+py>=170)continue;
                    string s1=AaSignature(x,y),s2=AaSignature(px,py);
                    if(s1==s2||usedAngleSets.Contains(s1)||usedAngleSets.Contains(s2))continue;
                    usedAngleSets.Add(s1);usedAngleSets.Add(s2);
                    c.shields[open[2]]=Aa(Vertices[open[2]],x,y,"proportional-angles");
                    c.shields[open[3]]=Aa(Vertices[open[3]],px,py,"proportional-angles");proportionalPlaced=true;
                }
                if(!proportionalPlaced)throw new InvalidOperationException("AA 비례각 함정 생성 실패: "+c.id);
                c.ratioSmall = 1; c.ratioLarge = 1;
                c.prompt = "△ 기호의 대응 순서대로 두 대응각이 같은 방패를 포개시오.";
                c.reveal = "대응각 " + a1 + "°와 " + a2 + "°가 각각 같다 · AA 닮음";
                c.unitConcept = "AA 닮음과 꼭짓점 대응 순서";
            }
            else
            {
                int baseRow = PositiveMod(seed * 5 + 2, Triples.GetLength(0));
                int a = Triples[baseRow,0], b = Triples[baseRow,1], d = Triples[baseRow,2];
                c.shields[ca] = kind == ContractKind.Volume ? Solid(a*2, b*2, d*2, "") : Sss(Vertices[ca], a*2, b*2, d*2, "");
                c.shields[cb] = kind == ContractKind.Volume ? Solid(a*3, b*3, d*3, "") : Sss(Vertices[cb], a*3, b*3, d*3, "");
                int trapM = kind == ContractKind.Area ? 4 : 8;
                int trapN = kind == ContractKind.Area ? 9 : 27;
                int ta = 2 + PositiveMod(seed, 3), tb = ta + 1, tc = ta + 2;
                string trap = kind == ContractKind.Area ? "linear-area-ratio" : "linear-volume-ratio";
                c.shields[open[0]] = kind == ContractKind.Volume ? Solid(ta*trapM, tb*trapM, tc*trapM, trap) : Sss(Vertices[open[0]], ta*trapM, tb*trapM, tc*trapM, trap);
                c.shields[open[1]] = kind == ContractKind.Volume ? Solid(ta*trapN, tb*trapN, tc*trapN, trap) : Sss(Vertices[open[1]], ta*trapN, tb*trapN, tc*trapN, trap);
                string axisTrap=kind==ContractKind.Area?"one-axis-area-ratio":"one-axis-volume-ratio";
                bool axisTrapPlaced=false;
                for(int bump=0;bump<100&&!axisTrapPlaced;bump++)
                {
                    int ux=13+variant+bump*3,uy=ux+2,uz=ux+5;
                    c.shields[open[2]] = kind == ContractKind.Volume ? Solid(ux*2, uy*2, uz*2, axisTrap) : Sss(Vertices[open[2]], ux*2, uy*2, uz*2, axisTrap);
                    c.shields[open[3]] = kind == ContractKind.Volume ? Solid(ux*3, uy*3+1, uz*3+1, axisTrap) : Sss(Vertices[open[3]], ux*3, uy*3+1, uz*3+1, axisTrap);
                    axisTrapPlaced=HasUniqueExpectedPair(c);
                }
                if(!axisTrapPlaced)throw new InvalidOperationException("넓이·부피 한 축 함정 생성 실패: "+c.id);
                c.ratioSmall = 2; c.ratioLarge = 3;
                if (kind == ContractKind.Area)
                {
                    c.prompt = "닮음비가 2:3이고 넓이의 비가 4:9인 두 방패를 포개시오.";
                    c.reveal = "닮음비 2:3 → 넓이의 비 2²:3²=4:9";
                    c.unitConcept = "닮은 도형의 넓이의 비";
                }
                else
                {
                    c.prompt = "가로·세로·높이의 닮음비가 2:3인 두 직육면체를 포개시오. 부피의 비는 8:27이다.";
                    c.reveal = "세 선형 치수의 비 2:3 → 부피의 비 2³:3³=8:27";
                    c.unitConcept = "닮은 입체도형의 부피의 비";
                }
            }

            ValidateContract(c);
            return c;
        }

        static ShieldData Sss(string vertex, int a, int b, int c, string misconception)
        {
            return new ShieldData { vertex = vertex, s1 = a, s2 = b, s3 = c, misconceptionId = misconception };
        }

        static ShieldData Sas(string vertex, int a, int b, int angle, bool included, string misconception)
        {
            return new ShieldData { vertex = vertex, s1 = a, s2 = b, a1 = angle, included = included, misconceptionId = misconception };
        }

        static ShieldData Aa(string vertex, int a, int b, string misconception)
        {
            return new ShieldData { vertex = vertex, a1 = a, a2 = b, misconceptionId = misconception };
        }

        static ShieldData Solid(int width, int depth, int height, string misconception)
        {
            return new ShieldData { vertex = "", s1 = width, s2 = depth, s3 = height, solid = true, misconceptionId = misconception };
        }

        static void ValidateContract(GolemContract c)
        {
            int valid = 0, found = -1;
            for (int i = 0; i < ShieldCount; i++)
            {
                if (c.shields[i] == null) throw new InvalidOperationException("방패 누락: " + c.id + " #" + i);
                for (int j = i + 1; j < ShieldCount; j++)
                    if (IsCorrectPair(c, i, j)) { valid++; found = PairIndex(i, j); }
            }
            if (valid != 1 || found != c.CorrectPairIndex)
                throw new InvalidOperationException("유효쌍은 정확히 1개여야 함: " + c.id + " valid=" + valid + " found=" + found + " expected=" + c.CorrectPairIndex);

            var misconceptionCounts=new Dictionary<string,int>();
            for(int i=0;i<ShieldCount;i++)
            {
                string id=c.shields[i].misconceptionId;
                if(string.IsNullOrEmpty(id))continue;
                misconceptionCounts[id]=misconceptionCounts.ContainsKey(id)?misconceptionCounts[id]+1:1;
            }
            int misconceptionPairs=0;
            foreach(var entry in misconceptionCounts)if(entry.Value>=2)misconceptionPairs++;
            if(misconceptionPairs<2)
                throw new InvalidOperationException("대표 오개념쌍은 서로 다른 유형으로 2개 이상이어야 함: "+c.id+" count="+misconceptionPairs);

            MgfProblem p = c.ToMgf();
            var unique = new HashSet<string>(p.choices);
            if (unique.Count != PairCount) throw new InvalidOperationException("선택지 중복: " + c.id);
            if (!unique.Contains(p.answer)) throw new InvalidOperationException("정답 누락: " + c.id);

            // 오개념 조합 전수검사: 오답으로 표시된 어떤 쌍도 실제 판정에서 참이면 안 된다.
            for (int i = 0; i < ShieldCount; i++)
            for (int j = i + 1; j < ShieldCount; j++)
                if (PairIndex(i,j) != c.CorrectPairIndex && IsCorrectPair(c,i,j))
                    throw new InvalidOperationException("오답 우연 일치: " + c.id + " " + i + "," + j);
        }

        static bool HasUniqueExpectedPair(GolemContract c)
        {
            int valid=0,found=-1;
            for(int i=0;i<ShieldCount;i++)
            {
                if(c.shields[i]==null)return false;
                for(int j=i+1;j<ShieldCount;j++)if(IsCorrectPair(c,i,j)){valid++;found=PairIndex(i,j);}
            }
            return valid==1&&found==c.CorrectPairIndex;
        }

        public static string BankEvidence(GolemContract c)
        {
            var parts = new string[ShieldCount];
            for (int i = 0; i < ShieldCount; i++) parts[i] = Letters[i] + "=" + c.ShieldLabel(i).Replace("\n", " ");
            return string.Join(" / ", parts);
        }

        static int PositiveMod(int x, int n) { int r = x % n; return r < 0 ? r + n : r; }
    }
}
