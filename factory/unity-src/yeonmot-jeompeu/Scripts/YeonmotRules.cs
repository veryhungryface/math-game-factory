// 연못 점프 — 런타임과 문제 은행이 함께 쓰는 삼각형 닮음·대응점 생성기.
//
// curriculum/2022-middle-math.json style_guide / expression_traps:
// - 발문은 중학교 교과서 지시형 「~을 찾으시오」로 통일한다.
// - △ABC∽△DEF는 A↔D, B↔E, C↔F처럼 꼭짓점 대응 순서대로 쓴다.
// - 길이는 6 cm처럼 띄어 쓰고 각은 60°로 쓴다.
// - SSS 닮음·SAS 닮음·AA 닮음 명칭을 그대로 쓴다.
// - 중2 범위이므로 근호·제곱근·삼각비를 만들거나 표시하지 않는다.
//
// 오개념 역산(misconceptionId):
// - additive_difference: 대응변의 차가 같으면 닮음이라고 판단한다.
// - partial_ratio: 두 변만 비례하면 세 변이 모두 비례한다고 생각한다.
// - sas_wrong_angle: 두 변의 비만 보고 끼인각을 확인하지 않는다.
// - sas_ratio_mismatch: 끼인각만 같으면 닮음이라고 생각한다.
// - aa_one_angle: 한 각만 같아도 AA 닮음이라고 생각한다.
// - correspondence_by_position: 그림의 위·아래 위치로 대응점을 잡는다.
using System;
using System.Collections.Generic;
using Mgf;

namespace Mgf.YeonmotJeompeu
{
    public enum PondKind { Sss, Sas, Aa }

    [Serializable]
    public sealed class TriangleDatum
    {
        public string name = "";                 // DEF 또는 GHI
        public int[] sides = new int[3];          // 01, 12, 20 순서
        public int[] angles = new int[3];         // 꼭짓점 0, 1, 2
        public int[] posToVertex = new int[3];     // 화면 위치 -> 기호 꼭짓점 인덱스
        public string misconceptionId = "";
        public bool similar;
    }

    [Serializable]
    public sealed class PondProblem
    {
        public string id = "";
        public PondKind kind;
        public int band;
        public int serial;
        public int[] sourceSides = new int[3];
        public int[] sourceAngles = new int[3];
        // 원본 꼭짓점 인덱스(A=0,B=1,C=2) -> 정답 잎의 기호 꼭짓점 인덱스.
        // 화면 배치(posToVertex)와 수학적 대응을 분리해 뒤집힌 삼각형도 같은 모델로 판정한다.
        public int[] sourceToCandidateVertex = new int[3];
        public int sourceActiveVertex;
        public TriangleDatum[] candidates = new TriangleDatum[2];
        public int validLeafSlot;
        public int correctVertexPosition;
        public int correctSlot;
        public string prompt = "";
        public string sourceDisplay = "";
        public string explanation = "";
        public string unitConcept = "";
    }

    public struct PondRng
    {
        uint state;
        public PondRng(int seed) { state = (uint)seed + 0x9E3779B9u; if (state == 0) state = 1; }
        public uint NextU() { uint x = state; x ^= x << 13; x ^= x >> 17; x ^= x << 5; state = x; return x; }
        public int Range(int min, int max) { return min + (int)(NextU() % (uint)(max - min)); }
    }

    public static class YeonmotRules
    {
        public const int StartLives = 3;
        public const int TargetHops = 8;
        public const int MasteryFirstCorrect = 6;
        public const float RunSeconds = 90f;

        static readonly int[][] SssBases = {
            new[]{3,4,5}, new[]{4,5,6}, new[]{4,6,7}, new[]{5,7,9}
        };
        static readonly int[][] SasPairs = {
            new[]{2,3}, new[]{3,4}, new[]{3,5}, new[]{4,5}
        };
        static readonly int[] SasAngles = { 40, 50, 60, 80 };
        static readonly int[][] AaSets = {
            new[]{40,60,80}, new[]{35,65,80}, new[]{45,55,80}, new[]{30,70,80}, new[]{40,65,75}
        };
        static readonly int[][] AaFakes = {
            new[]{40,65,75}, new[]{35,60,85}, new[]{45,60,75}, new[]{30,65,85}, new[]{40,60,80}
        };
        static readonly int[][] Permutations = {
            new[]{0,1,2}, new[]{0,2,1}, new[]{1,0,2},
            new[]{1,2,0}, new[]{2,0,1}, new[]{2,1,0}
        };

        static int[] Copy3(int[] a) => new[]{a[0],a[1],a[2]};

        static int Mod(int value, int modulo)
        {
            int r = value % modulo;
            return r < 0 ? r + modulo : r;
        }

        static int[] Sorted3(int[] source)
        {
            var a = Copy3(source); Array.Sort(a); return a;
        }

        static bool Triangle(int[] sides)
        {
            var a = Sorted3(sides);
            return a[0] > 0 && a[0] + a[1] > a[2];
        }

        public static bool IsSssSimilar(int[] a0, int[] b0)
        {
            if (a0 == null || b0 == null || !Triangle(a0) || !Triangle(b0)) return false;
            var a = Sorted3(a0); var b = Sorted3(b0);
            return (long)a[0] * b[1] == (long)a[1] * b[0] &&
                   (long)a[1] * b[2] == (long)a[2] * b[1];
        }

        public static bool IsSasSimilar(int[] a, int angleA, int[] b, int angleB)
        {
            return a != null && b != null && a[0] > 0 && a[2] > 0 && b[0] > 0 && b[2] > 0 &&
                   angleA == angleB && (long)a[0] * b[2] == (long)a[2] * b[0];
        }

        public static bool IsAaSimilar(int[] a0, int[] b0)
        {
            if (a0 == null || b0 == null) return false;
            var a = Sorted3(a0); var b = Sorted3(b0);
            return a[0] > 0 && a[0] + a[1] + a[2] == 180 &&
                   b[0] > 0 && b[0] + b[1] + b[2] == 180 &&
                   a[0] == b[0] && a[1] == b[1] && a[2] == b[2];
        }

        public static bool IsLeafSimilar(PondProblem p, int leaf)
        {
            if (p == null || leaf < 0 || leaf > 1 || p.candidates[leaf] == null) return false;
            TriangleDatum d = p.candidates[leaf];
            if (p.kind == PondKind.Sss) return IsSssSimilar(p.sourceSides, d.sides);
            if (p.kind == PondKind.Sas) return IsSasSimilar(p.sourceSides, p.sourceAngles[0], d.sides, d.angles[0]);
            return IsAaSimilar(p.sourceAngles, d.angles);
        }

        public static bool IsCorrect(PondProblem p, int slot)
        {
            if (p == null || slot < 0 || slot >= 6) return false;
            int leaf = slot / 3, position = slot % 3;
            if (leaf != p.validLeafSlot || !IsLeafSimilar(p, leaf)) return false;
            return p.candidates[leaf].posToVertex[position] == CorrespondingVertex(p, p.sourceActiveVertex);
        }

        public static int CorrespondingVertex(PondProblem p, int sourceVertex)
        {
            if (p == null || p.sourceToCandidateVertex == null || p.sourceToCandidateVertex.Length != 3) return -1;
            return p.sourceToCandidateVertex[Math.Max(0, Math.Min(2, sourceVertex))];
        }

        public static string SimilarityStatement(PondProblem p)
        {
            if (p == null || p.validLeafSlot < 0 || p.validLeafSlot > 1) return "";
            string rhs = "";
            for (int source = 0; source < 3; source++)
                rhs += VertexChar(p.validLeafSlot, CorrespondingVertex(p, source));
            return "△ABC∽△" + rhs;
        }

        public static string MisconceptionId(PondProblem p, int slot)
        {
            if (p == null || slot < 0 || slot >= 6 || IsCorrect(p, slot)) return "";
            int leaf = slot / 3;
            if (leaf != p.validLeafSlot) return p.candidates[leaf].misconceptionId;
            return "correspondence_by_position";
        }

        public static char VertexChar(int leaf, int canonicalVertex)
        {
            return (char)((leaf == 0 ? 'D' : 'G') + canonicalVertex);
        }

        public static string LeafName(int leaf) => leaf == 0 ? "DEF" : "GHI";

        public static string ChoiceLabel(PondProblem p, int slot)
        {
            int leaf = slot / 3, pos = slot % 3;
            char v = VertexChar(leaf, p.candidates[leaf].posToVertex[pos]);
            return "잎 " + LeafName(leaf) + "의 " + v;
        }

        public static int SideBetween(int a, int b)
        {
            if ((a == 0 && b == 1) || (a == 1 && b == 0)) return 0;
            if ((a == 1 && b == 2) || (a == 2 && b == 1)) return 1;
            return 2;
        }

        static int[] PermutationWith(int activeVertex, int targetPosition, bool flip)
        {
            int[] r = {-1,-1,-1}; r[targetPosition] = activeVertex;
            int a = (activeVertex + 1) % 3, b = (activeVertex + 2) % 3;
            if (flip) { int t = a; a = b; b = t; }
            int k = 0;
            for (int i = 0; i < 3; i++) if (i != targetPosition) r[i] = k++ == 0 ? a : b;
            return r;
        }

        static TriangleDatum Datum(string name, int[] sides, int[] angles, int[] permutation, string misconception, bool similar)
        {
            return new TriangleDatum {
                name = name, sides = Copy3(sides), angles = Copy3(angles), posToVertex = Copy3(permutation),
                misconceptionId = misconception, similar = similar
            };
        }

        static string SourceText(PondProblem p)
        {
            if (p.kind == PondKind.Sss)
                return "△ABC · AB=" + p.sourceSides[0] + " cm · BC=" + p.sourceSides[1] + " cm · CA=" + p.sourceSides[2] + " cm";
            if (p.kind == PondKind.Sas)
                return "△ABC · AB=" + p.sourceSides[0] + " cm · AC=" + p.sourceSides[2] + " cm · ∠A=" + p.sourceAngles[0] + "°";
            return "△ABC · ∠A=" + p.sourceAngles[0] + "° · ∠B=" + p.sourceAngles[1] + "°";
        }

        public static string CandidateText(PondProblem p, int leaf)
        {
            TriangleDatum d = p.candidates[leaf];
            char x = VertexChar(leaf, 0), y = VertexChar(leaf, 1), z = VertexChar(leaf, 2);
            if (p.kind == PondKind.Sss)
                return "△" + d.name + " · " + x + y + "=" + d.sides[0] + " cm · " + y + z + "=" + d.sides[1] + " cm · " + z + x + "=" + d.sides[2] + " cm";
            if (p.kind == PondKind.Sas)
                return "△" + d.name + " · " + x + y + "=" + d.sides[0] + " cm · " + x + z + "=" + d.sides[2] + " cm · ∠" + x + "=" + d.angles[0] + "°";
            return "△" + d.name + " · ∠" + x + "=" + d.angles[0] + "° · ∠" + y + "=" + d.angles[1] + "°";
        }

        static PondProblem Make(PondKind kind, int seed, int serial, int desiredSlot)
        {
            var rng = new PondRng(seed * 37 + serial * 101 + 17);
            var p = new PondProblem {
                kind = kind, serial = serial, band = kind == PondKind.Sss ? 1 : kind == PondKind.Sas ? 2 : 3,
                sourceToCandidateVertex = new[]{0,1,2}
            };
            p.sourceActiveVertex = rng.Range(0, 3);
            p.validLeafSlot = Math.Max(0, Math.Min(1, desiredSlot / 3));
            p.correctVertexPosition = Math.Max(0, Math.Min(2, desiredSlot % 3));
            p.correctSlot = p.validLeafSlot * 3 + p.correctVertexPosition;
            int[] correctPermutation = PermutationWith(p.sourceActiveVertex, p.correctVertexPosition, (serial & 1) == 1);
            int[] fakePermutation = Copy3(Permutations[(serial * 5 + 2) % Permutations.Length]);

            TriangleDatum correct, fake;
            if (kind == PondKind.Sss)
            {
                int[] b = SssBases[Mod(seed + serial, SssBases.Length)];
                int m = 1 + Mod(seed / 7 + serial, 4);
                int n = 1 + Mod(seed / 13 + serial * 2, 4); if (n == m) n = n % 4 + 1;
                p.sourceSides = new[]{b[0]*m,b[1]*m,b[2]*m}; p.sourceAngles = new[]{0,0,0};
                int[] good = {b[0]*n,b[1]*n,b[2]*n};
                int d = 1 + Mod(serial, 3);
                bool additive = (serial & 1) == 0;
                int[] bad = additive ? new[]{p.sourceSides[0]+d,p.sourceSides[1]+d,p.sourceSides[2]+d}
                                     : new[]{good[0],good[1],good[2]+1};
                correct = Datum("", good, new[]{0,0,0}, correctPermutation, "", true);
                fake = Datum("", bad, new[]{0,0,0}, fakePermutation, additive ? "additive_difference" : "partial_ratio", false);
                p.explanation = "세 대응변의 비가 모두 같다 · SSS 닮음";
                p.unitConcept = "SSS 닮음과 대응변";
            }
            else if (kind == PondKind.Sas)
            {
                int[] b = SasPairs[Mod(seed + serial, SasPairs.Length)];
                int m = 1 + Mod(seed / 5 + serial, 3);
                int n = 1 + Mod(seed / 11 + serial * 2, 3); if (n == m) n = n % 3 + 1;
                int angle = SasAngles[Mod(seed / 17 + serial, SasAngles.Length)];
                p.sourceSides = new[]{b[0]*m,0,b[1]*m}; p.sourceAngles = new[]{angle,0,0};
                int[] good = {b[0]*n,0,b[1]*n};
                bool wrongAngle = (serial & 1) == 0;
                int[] badSides = wrongAngle ? Copy3(good) : new[]{good[0],0,good[2]+1+Mod(serial,2)};
                int[] badAngles = {wrongAngle ? 180-angle : angle,0,0};
                correct = Datum("", good, new[]{angle,0,0}, correctPermutation, "", true);
                fake = Datum("", badSides, badAngles, fakePermutation, wrongAngle ? "sas_wrong_angle" : "sas_ratio_mismatch", false);
                p.explanation = "두 대응변의 비와 그 사이의 각이 같다 · SAS 닮음";
                p.unitConcept = "SAS 닮음과 끼인각";
            }
            else
            {
                int index = Mod(seed + serial, AaSets.Length);
                p.sourceAngles = Copy3(AaSets[index]); p.sourceSides = new[]{0,0,0};
                int[] good = Copy3(p.sourceAngles); int[] bad = Copy3(AaFakes[index]);
                correct = Datum("", new[]{0,0,0}, good, correctPermutation, "", true);
                fake = Datum("", new[]{0,0,0}, bad, fakePermutation, "aa_one_angle", false);
                p.explanation = "두 대응각이 각각 같다 · AA 닮음";
                p.unitConcept = "AA 닮음과 대응각의 역추적";
            }

            if (p.validLeafSlot == 0) { correct.name = "DEF"; fake.name = "GHI"; p.candidates[0] = correct; p.candidates[1] = fake; }
            else { fake.name = "DEF"; correct.name = "GHI"; p.candidates[0] = fake; p.candidates[1] = correct; }
            p.id = kind.ToString().ToLowerInvariant() + "-" + serial + "-" + seed;
            p.sourceDisplay = SourceText(p);
            char source = (char)('A' + p.sourceActiveVertex);
            p.prompt = "닮은 잎에서 " + source + "의 대응점을 찾으시오.";
            char answer = VertexChar(p.validLeafSlot, CorrespondingVertex(p, p.sourceActiveVertex));
            p.explanation += " · " + SimilarityStatement(p) + " · " + source + "↔" + answer;
            return p;
        }

        public static PondProblem Practice()
        {
            var p = new PondProblem {
                id="practice-345", kind=PondKind.Sss, band=0, serial=0,
                sourceSides=new[]{3,4,5}, sourceAngles=new[]{0,0,0}, sourceActiveVertex=1,
                sourceToCandidateVertex=new[]{0,2,1},
                validLeafSlot=0, correctVertexPosition=1, correctSlot=1,
                prompt="닮은 잎에서 B의 대응점을 찾으시오.",
                sourceDisplay="△ABC · AB=3 cm · BC=4 cm · CA=5 cm",
                explanation="3:6 = 4:8 = 5:10 = 1:2 · △ABC∽△DFE (SSS 닮음) · B↔F",
                unitConcept="SSS 닮음과 대응점"
            };
            // 화면 위치: 위=D, 오른쪽 아래=F, 왼쪽 아래=E. 학생이 B→F로 실제 드래그한다.
            p.candidates[0]=Datum("DEF",new[]{10,8,6},new[]{0,0,0},new[]{0,2,1},"",true);
            p.candidates[1]=Datum("GHI",new[]{4,5,6},new[]{0,0,0},new[]{0,1,2},"additive_difference",false);
            return p;
        }

        public static List<PondProblem> RunDeck(int seed)
        {
            var list = new List<PondProblem>(TargetHops);
            var slotRng = new PondRng(seed ^ unchecked((int)0xB5297A4D));
            PondKind[] kinds = { PondKind.Sss, PondKind.Sss, PondKind.Sss, PondKind.Sas, PondKind.Sas, PondKind.Sas, PondKind.Aa, PondKind.Aa };
            for (int i = 0; i < kinds.Length; i++) list.Add(Make(kinds[i], seed + i * 7919, i, slotRng.Range(0, 6)));
            return list;
        }

        public static List<MgfProblem> BuildBank()
        {
            var bank = new List<MgfProblem>(384);
            for (int kindIndex = 0; kindIndex < 3; kindIndex++)
            for (int i = 0; i < 128; i++)
            {
                PondKind kind = kindIndex == 0 ? PondKind.Sss : kindIndex == 1 ? PondKind.Sas : PondKind.Aa;
                PondProblem p = Make(kind, 100003 + kindIndex * 900001 + i * 1543, kindIndex * 128 + i, i % 6);
                string[] choices = new string[6];
                for (int slot = 0; slot < 6; slot++) choices[slot] = ChoiceLabel(p, slot);
                bank.Add(new MgfProblem {
                    id=p.id,
                    prompt=p.prompt+" "+p.sourceDisplay+" / "+CandidateText(p,0)+" / "+CandidateText(p,1),
                    choices=choices, answer=choices[p.correctSlot], answerNumeric=p.correctSlot,
                    unitConcept=p.unitConcept
                });
            }
            return bank;
        }

        // 전수 검산: 정답 잎 1개·대응 꼭짓점 1개, 삼각형 성립, 오답 우연 참 0건,
        // choices 중복 0건을 검사한다. Awake에서 0이 아니면 QA 콘솔 fatal로 드러난다.
        public static int AuditBank()
        {
            int errors = 0;
            errors += AuditProblem(Practice(), true);
            for (int kindIndex = 0; kindIndex < 3; kindIndex++)
            for (int i = 0; i < 384; i++)
            {
                PondKind kind = kindIndex == 0 ? PondKind.Sss : kindIndex == 1 ? PondKind.Sas : PondKind.Aa;
                PondProblem p = Make(kind, 700001 + kindIndex * 400009 + i * 977, i, i % 6);
                errors += AuditProblem(p, false);
                if (kind == PondKind.Sss && (!Triangle(p.sourceSides) || !Triangle(p.candidates[0].sides) || !Triangle(p.candidates[1].sides))) errors++;
                if (kind == PondKind.Aa && (p.sourceAngles[0]+p.sourceAngles[1]+p.sourceAngles[2] != 180 ||
                    p.candidates[0].angles[0]+p.candidates[0].angles[1]+p.candidates[0].angles[2] != 180 ||
                    p.candidates[1].angles[0]+p.candidates[1].angles[1]+p.candidates[1].angles[2] != 180)) errors++;
            }
            return errors;
        }

        static int AuditProblem(PondProblem p, bool practice)
        {
            int errors = 0, similarLeaves = 0, correctSlots = 0;
            for (int leaf = 0; leaf < 2; leaf++) if (IsLeafSimilar(p, leaf)) similarLeaves++;
            var seen = new HashSet<string>();
            for (int slot = 0; slot < 6; slot++) {
                if (IsCorrect(p, slot)) correctSlots++;
                if (!seen.Add(ChoiceLabel(p, slot))) errors++;
            }
            if (similarLeaves != 1 || correctSlots != 1 || !IsCorrect(p, p.correctSlot)) errors++;
            int expected = CorrespondingVertex(p, p.sourceActiveVertex);
            if (expected < 0 || expected > 2 || p.candidates[p.validLeafSlot].posToVertex[p.correctVertexPosition] != expected) errors++;
            string answerPair = ((char)('A' + p.sourceActiveVertex)) + "↔" + VertexChar(p.validLeafSlot, expected).ToString();
            if (!p.explanation.Contains(SimilarityStatement(p)) || !p.explanation.Contains(answerPair)) errors++;
            if (practice && (SimilarityStatement(p) != "△ABC∽△DFE" || ChoiceLabel(p, p.correctSlot) != "잎 DEF의 F")) errors++;
            return errors;
        }
    }
}
