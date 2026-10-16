// 유리나방 결속 — 삼각형 닮음 대응 문제의 단일 정본.
//
// curriculum/2022-middle-math.json style_guide / expression_traps:
// - 중학교 발문은 지시형 「~하시오」로 통일한다.
// - △ABC∽△DEF는 기호 순서대로 A↔D, B↔E, C↔F이다.
// - 길이는 "6 cm", 각은 "60°"처럼 쓰며 SSS·SAS·AA 닮음 명칭을 그대로 쓴다.
// - 중2 범위이므로 √·근호·삼각비·가정·결론 용어를 생성하지 않는다.
// - 그림은 개략도이고 실제 픽셀 길이·색·화면 위치는 닮음의 근거가 아니다.
//
// 오개념 역산(misconceptionId):
// - additive_difference: 대응변에 같은 수를 더하면 닮는다는 가산적 추론.
// - sas_wrong_included_angle: 두 변의 비만 맞고 끼인각을 확인하지 않는 오류.
// - position_correspondence: 화면에서 같은 위치의 꼭짓점끼리 대응시키는 오류.
// - aa_one_angle: 한 각만 같으면 AA 닮음이라고 판단하는 오류.
// - ratio_mismatch: 대응변 한 쌍의 비가 다른데도 닮음이라고 판단하는 오류.
using System;
using System.Collections.Generic;
using Mgf;

namespace Mgf.YurinabangGyeolsok
{
    public enum LaceCondition { Sss, Sas, Aa }

    [Serializable]
    public sealed class LaceCandidate
    {
        // sides: DE, EF, FD / angles: D, E, F. 0은 화면에 표시하지 않는 값이다.
        public int[] sides = new int[3];
        public int[] angles = new int[3];
        // labelToVisual[label] = 화면 삼각형의 어느 꼭짓점(0=좌하,1=우하,2=상)에 놓이는가.
        public int[] labelToVisual = new int[3];
        public string misconceptionId = "";
    }

    [Serializable]
    public sealed class LaceProblem
    {
        public string id;
        public LaceCondition condition;
        public int band;
        public int[] sourceSides = new int[3]; // AB, BC, CA
        public int[] sourceAngles = new int[3]; // A, B, C
        public int[] sourceLabelToVisual = new int[3];
        public LaceCandidate[] candidates = new LaceCandidate[3];
        public int correctCandidate;
        // source vertex A/B/C -> candidate label D/E/F
        public int[] vertexMap = new int[3];
        public string prompt;
        public string answer;
        public string[] choices;
        public string unitConcept;
        public string reveal;
    }

    public static class YurinabangRules
    {
        public const int Floors = 8;
        public const int StartLives = 3;
        public const float RunSeconds = 105f;

        public static readonly int[][] Permutations = {
            new[] { 0, 1, 2 }, new[] { 0, 2, 1 }, new[] { 1, 0, 2 },
            new[] { 1, 2, 0 }, new[] { 2, 0, 1 }, new[] { 2, 1, 0 }
        };

        static readonly string[] SlotNames = { "왼쪽", "위쪽", "오른쪽" };
        static readonly string[] SourceLabels = { "A", "B", "C" };
        static readonly string[] TargetLabels = { "D", "E", "F" };

        static int EdgeIndex(int a, int b)
        {
            if ((a == 0 && b == 1) || (a == 1 && b == 0)) return 0;
            if ((a == 1 && b == 2) || (a == 2 && b == 1)) return 1;
            return 2;
        }

        public static int PermutationIndex(int[] map)
        {
            for (int i = 0; i < Permutations.Length; i++)
                if (Permutations[i][0] == map[0] && Permutations[i][1] == map[1] && Permutations[i][2] == map[2]) return i;
            return -1;
        }

        public static string MappingText(int candidate, int[] map)
        {
            return SlotNames[candidate] + " 창 · "
                + SourceLabels[0] + "↔" + TargetLabels[map[0]] + ", "
                + SourceLabels[1] + "↔" + TargetLabels[map[1]] + ", "
                + SourceLabels[2] + "↔" + TargetLabels[map[2]];
        }

        static string[] AllChoices()
        {
            var result = new string[18];
            int k = 0;
            for (int c = 0; c < 3; c++)
                for (int p = 0; p < Permutations.Length; p++) result[k++] = MappingText(c, Permutations[p]);
            return result;
        }

        static LaceCandidate Candidate(int layoutPermutation, string misconception)
        {
            return new LaceCandidate {
                labelToVisual = (int[])Permutations[layoutPermutation % 6].Clone(),
                misconceptionId = misconception
            };
        }

        static void SetSide(int[] sides, int va, int vb, int value) => sides[EdgeIndex(va, vb)] = value;
        public static int GetSide(int[] sides, int va, int vb) => sides[EdgeIndex(va, vb)];

        static LaceCandidate ScaledCandidate(int[] sourceSides, int[] sourceAngles, int[] map,
            int scale, int layoutPermutation, string misconception)
        {
            var c = Candidate(layoutPermutation, misconception);
            SetSide(c.sides, map[0], map[1], sourceSides[0] * scale);
            SetSide(c.sides, map[1], map[2], sourceSides[1] * scale);
            SetSide(c.sides, map[2], map[0], sourceSides[2] * scale);
            for (int i = 0; i < 3; i++) c.angles[map[i]] = sourceAngles[i];
            return c;
        }

        static LaceCandidate AdditiveCandidate(int[] sourceSides, int[] sourceAngles, int[] map,
            int delta, int layoutPermutation)
        {
            var c = Candidate(layoutPermutation, "additive_difference");
            SetSide(c.sides, map[0], map[1], sourceSides[0] + delta);
            SetSide(c.sides, map[1], map[2], sourceSides[1] + delta);
            SetSide(c.sides, map[2], map[0], sourceSides[2] + delta);
            for (int i = 0; i < 3; i++) c.angles[map[i]] = sourceAngles[i];
            return c;
        }

        static LaceProblem MakeSss(int i)
        {
            int a = 3 + (i % 7);
            int b = a + 1 + ((i / 7) % 3);
            int c = b + 1 + ((i / 21) % 3);
            // a+b>c is 구조적으로 참: c<=b+3, a>=3이며 등호 가능 조합은 아래 +1로 피한다.
            if (a + b <= c) c = a + b - 1;
            int[] sides = { a, b, c };
            // SSS에서 각 수치는 판정에 쓰지 않는다. 세 값이 서로 다른 기호만 보존한다.
            int[] angles = { 47 + (i % 3), 61 + (i % 5), 72 - (i % 3) - (i % 5) };
            int scale = 2 + ((i / 63) % 2);
            return Assemble("lace-sss-" + i.ToString("D3"), LaceCondition.Sss, 1, sides, angles,
                i % 3, (i * 5 + 1) % 6, scale, i);
        }

        static LaceProblem MakeSas(int i)
        {
            int s1 = 2 + (i % 8);
            int s2 = s1 + 1 + ((i / 8) % 4);
            int theta = new[] { 40, 50, 60, 70, 80 }[(i / 32) % 5];
            int[] sides = { s1, 0, s2 }; // AB와 CA만 주어진다.
            int[] angles = { theta, 0, 0 };
            int scale = 2 + ((i / 80) % 2);
            return Assemble("lace-sas-" + i.ToString("D3"), LaceCondition.Sas, 2, sides, angles,
                (i + 1) % 3, (i * 5 + 2) % 6, scale, i);
        }

        static LaceProblem MakeAa(int i)
        {
            // 모든 원각은 3의 배수, 오답의 바뀐 각은 3의 배수가 아니게 구성한다.
            // 따라서 한 각만 우연히 같을 수 있고 두 각이 동시에 같은 경우는 구조적으로 0건이다.
            int a = 30 + (i % 9) * 3;
            int b = 48 + ((i / 9) % 9) * 3;
            if (a == b) b += 3;
            int c = 180 - a - b;
            if (c < 30 || c == a || c == b) { b = 45; c = 180 - a - b; }
            int[] angles = { a, b, c };
            int[] sides = { 0, 0, 0 };
            return Assemble("lace-aa-" + i.ToString("D3"), LaceCondition.Aa, 3, sides, angles,
                (i + 2) % 3, (i * 5 + 3) % 6, 1, i);
        }

        static LaceProblem Assemble(string id, LaceCondition condition, int band, int[] sourceSides,
            int[] sourceAngles, int correctSlot, int mapPermutation, int scale, int seed)
        {
            int[] map = (int[])Permutations[mapPermutation].Clone();
            var correct = ScaledCandidate(sourceSides, sourceAngles, map, scale, (seed * 5 + correctSlot) % 6, "");
            LaceCandidate wrongA;
            LaceCandidate wrongB;
            int[] wrongMapA = Permutations[(mapPermutation + 2) % 6];
            int[] wrongMapB = Permutations[(mapPermutation + 4) % 6];

            if (condition == LaceCondition.Sss)
            {
                wrongA = AdditiveCandidate(sourceSides, sourceAngles, wrongMapA, 2 + seed % 4, (seed * 3 + 1) % 6);
                wrongB = ScaledCandidate(sourceSides, sourceAngles, wrongMapB, scale, (seed * 3 + 4) % 6, "ratio_mismatch");
                // 정답 제거 → 범위 검사 → 중복 제거 순서의 "한 변 비 깨뜨리기". 세 변은 모두 양의 정수다.
                int broken = EdgeIndex(wrongMapB[1], wrongMapB[2]);
                wrongB.sides[broken] += 1 + seed % 2;
            }
            else if (condition == LaceCondition.Sas)
            {
                wrongA = ScaledCandidate(sourceSides, sourceAngles, wrongMapA, scale, (seed * 3 + 1) % 6, "sas_wrong_included_angle");
                wrongA.angles[wrongMapA[0]] = 180 - sourceAngles[0];
                wrongB = ScaledCandidate(sourceSides, sourceAngles, wrongMapB, scale, (seed * 3 + 4) % 6, "ratio_mismatch");
                int broken = EdgeIndex(wrongMapB[0], wrongMapB[2]);
                wrongB.sides[broken] += 1 + seed % 3;
            }
            else
            {
                wrongA = Candidate((seed * 3 + 1) % 6, "aa_one_angle");
                wrongB = Candidate((seed * 3 + 4) % 6, "aa_one_angle");
                // 정확히 한 각만 공유. +7, +11은 3의 배수가 아니어서 다른 원각과 우연히 같아지지 않는다.
                int wa = sourceAngles[0], wb = sourceAngles[1] + 7, wc = 180 - wa - wb;
                wrongA.angles[wrongMapA[0]] = wa;
                wrongA.angles[wrongMapA[1]] = wb;
                wrongA.angles[wrongMapA[2]] = wc;
                wa = sourceAngles[0] + 11; wb = sourceAngles[1]; wc = 180 - wa - wb;
                wrongB.angles[wrongMapB[0]] = wa;
                wrongB.angles[wrongMapB[1]] = wb;
                wrongB.angles[wrongMapB[2]] = wc;
            }

            var candidates = new LaceCandidate[3];
            int cursor = 0;
            for (int slot = 0; slot < 3; slot++)
            {
                if (slot == correctSlot) candidates[slot] = correct;
                else candidates[slot] = cursor++ == 0 ? wrongA : wrongB;
            }

            string conditionName = condition == LaceCondition.Sss ? "SSS 닮음" : condition == LaceCondition.Sas ? "SAS 닮음" : "AA 닮음";
            string givens;
            if (condition == LaceCondition.Sss)
                givens = "AB=" + sourceSides[0] + " cm, BC=" + sourceSides[1] + " cm, CA=" + sourceSides[2] + " cm";
            else if (condition == LaceCondition.Sas)
                givens = "AB=" + sourceSides[0] + " cm, AC=" + sourceSides[2] + " cm, ∠A=" + sourceAngles[0] + "°";
            else
                givens = "∠A=" + sourceAngles[0] + "°, ∠B=" + sourceAngles[1] + "°";

            var p = new LaceProblem {
                id = id,
                condition = condition,
                band = band,
                sourceSides = sourceSides,
                sourceAngles = sourceAngles,
                sourceLabelToVisual = (int[])Permutations[(seed * 7 + 2) % 6].Clone(),
                candidates = candidates,
                correctCandidate = correctSlot,
                vertexMap = map,
                prompt = "△ABC와 닮은 삼각창을 찾고 대응점을 이으시오. " + givens + " (" + conditionName + ")",
                unitConcept = conditionName + "과 대응점",
            };
            p.answer = MappingText(correctSlot, map);
            p.choices = AllChoices();
            p.reveal = RevealText(p);
            return p;
        }

        static string RevealText(LaceProblem p)
        {
            string sim = "△ABC∽△" + TargetLabels[p.vertexMap[0]] + TargetLabels[p.vertexMap[1]] + TargetLabels[p.vertexMap[2]];
            var c = p.candidates[p.correctCandidate];
            if (p.condition == LaceCondition.Sss)
            {
                int t0 = GetSide(c.sides, p.vertexMap[0], p.vertexMap[1]);
                int t1 = GetSide(c.sides, p.vertexMap[1], p.vertexMap[2]);
                int t2 = GetSide(c.sides, p.vertexMap[2], p.vertexMap[0]);
                return sim + "\n"
                    + p.sourceSides[0] + ":" + t0 + " = " + p.sourceSides[1] + ":" + t1 + " = " + p.sourceSides[2] + ":" + t2 + "\n"
                    + "세 대응변의 비가 같음 · SSS 닮음";
            }
            if (p.condition == LaceCondition.Sas)
            {
                int t0 = GetSide(c.sides, p.vertexMap[0], p.vertexMap[1]);
                int t2 = GetSide(c.sides, p.vertexMap[2], p.vertexMap[0]);
                return sim + "\n"
                    + p.sourceSides[0] + ":" + t0 + " = " + p.sourceSides[2] + ":" + t2 + " · 끼인각 " + p.sourceAngles[0] + "°\n"
                    + "두 변의 비와 끼인각이 같음 · SAS 닮음";
            }
            return sim + "\n"
                + p.sourceAngles[0] + "° = " + c.angles[p.vertexMap[0]] + "° · " + p.sourceAngles[1] + "° = " + c.angles[p.vertexMap[1]] + "°\n"
                + "두 대응각의 크기가 같음 · AA 닮음";
        }

        public static LaceProblem Practice()
        {
            int[] sides = { 6, 10, 8 }; // AB=6, BC=10, CA=8
            int[] angles = { 90, 53, 37 };
            var p = Assemble("lace-practice", LaceCondition.Sss, 0, sides, angles, 1, 0, 1, 997);
            p.candidates[1] = ScaledCandidate(sides, angles, new[] { 0, 1, 2 }, 3, 0, "");
            // 6:9=8:12=10:15=2:3이 되도록 3/2배. 정수 모델을 유지하기 위해 직접 대입한다.
            p.candidates[1].sides = new[] { 9, 15, 12 };
            p.correctCandidate = 1;
            p.vertexMap = new[] { 0, 1, 2 };
            p.sourceLabelToVisual = new[] { 0, 1, 2 };
            p.prompt = "연습: A↔D, B↔E, C↔F를 직접 봉합하고 종줄을 당기시오.";
            p.answer = MappingText(1, p.vertexMap);
            p.choices = AllChoices();
            p.unitConcept = "대응점과 SSS 닮음";
            p.reveal = "△ABC∽△DEF\n6:9 = 8:12 = 10:15 = 2:3\n세 대응변의 비가 같음 · SSS 닮음";
            return p;
        }

        public static List<LaceProblem> BuildProblems()
        {
            var result = new List<LaceProblem>(384);
            for (int i = 0; i < 128; i++) result.Add(MakeSss(i));
            for (int i = 0; i < 128; i++) result.Add(MakeSas(i));
            for (int i = 0; i < 128; i++) result.Add(MakeAa(i));
            return result;
        }

        public static List<LaceProblem> RunDeck(int runSerial)
        {
            var all = BuildProblems();
            var deck = new List<LaceProblem>(Floors);
            int offset = Math.Abs(runSerial * 37 + 11);
            // 난이도 밴드를 순서대로 소진: SSS 3 → SAS 3 → AA 2.
            for (int i = 0; i < 3; i++) deck.Add(all[(offset + i * 17) % 128]);
            for (int i = 0; i < 3; i++) deck.Add(all[128 + (offset * 3 + i * 19) % 128]);
            for (int i = 0; i < 2; i++) deck.Add(all[256 + (offset * 5 + i * 23) % 128]);
            return deck;
        }

        public static List<MgfProblem> BuildBank()
        {
            var source = BuildProblems();
            var bank = new List<MgfProblem>(source.Count);
            for (int i = 0; i < source.Count; i++)
            {
                var p = source[i];
                bank.Add(new MgfProblem {
                    id = p.id,
                    prompt = p.prompt,
                    choices = p.choices,
                    answer = p.answer,
                    answerNumeric = p.correctCandidate * 6 + PermutationIndex(p.vertexMap),
                    unitConcept = p.unitConcept
                });
            }
            return bank;
        }

        public static bool IsCorrect(LaceProblem p, int candidateIndex, int[] map)
        {
            if (p == null || map == null || map.Length != 3 || candidateIndex < 0 || candidateIndex >= 3) return false;
            if (map[0] == map[1] || map[0] == map[2] || map[1] == map[2]) return false;
            for (int i = 0; i < 3; i++) if (map[i] < 0 || map[i] > 2) return false;
            var c = p.candidates[candidateIndex];

            if (p.condition == LaceCondition.Sss)
            {
                int t0 = GetSide(c.sides, map[0], map[1]);
                int t1 = GetSide(c.sides, map[1], map[2]);
                int t2 = GetSide(c.sides, map[2], map[0]);
                if (t0 <= 0 || t1 <= 0 || t2 <= 0) return false;
                return p.sourceSides[0] * t1 == p.sourceSides[1] * t0
                    && p.sourceSides[1] * t2 == p.sourceSides[2] * t1;
            }
            if (p.condition == LaceCondition.Sas)
            {
                int t0 = GetSide(c.sides, map[0], map[1]);
                int t2 = GetSide(c.sides, map[2], map[0]);
                return t0 > 0 && t2 > 0
                    && p.sourceSides[0] * t2 == p.sourceSides[2] * t0
                    && c.angles[map[0]] == p.sourceAngles[0];
            }
            return c.angles[map[0]] == p.sourceAngles[0]
                && c.angles[map[1]] == p.sourceAngles[1];
        }

        public static string MisconceptionId(LaceProblem p, int candidateIndex, int[] map)
        {
            if (p == null || candidateIndex < 0 || candidateIndex >= 3) return "incomplete_lacing";
            if (candidateIndex == p.correctCandidate) return "position_correspondence";
            string id = p.candidates[candidateIndex].misconceptionId;
            return string.IsNullOrEmpty(id) ? "ratio_mismatch" : id;
        }

        public static string MisconceptionMessage(string id)
        {
            switch (id)
            {
                case "additive_difference": return "같은 수를 더한 길이가 아니라 대응변의 비를 비교하시오.";
                case "sas_wrong_included_angle": return "두 변 사이의 끼인각이 서로 다릅니다.";
                case "position_correspondence": return "화면 위치가 아니라 변과 각으로 대응점을 찾으시오.";
                case "aa_one_angle": return "한 각만 같아서는 AA 닮음이 성립하지 않습니다.";
                case "ratio_mismatch": return "대응변 한 쌍의 비가 서로 다릅니다.";
                default: return "세 대응점을 모두 봉합한 뒤 다시 확인하시오.";
            }
        }
    }
}
