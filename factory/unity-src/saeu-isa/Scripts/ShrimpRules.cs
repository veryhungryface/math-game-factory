// 새우 이사 — 중1 「자료의 정리와 해석」 정수 구간 생성기.
// curriculum/2022-middle-math.json style_guide / expression_traps:
// - 중학교 지시형 「~하시오」를 사용한다.
// - 수와 단위는 `23 mm`처럼 띄어 쓴다.
// - 계급은 하한 이상·상한 미만이며, 경계값은 오른쪽 계급에 포함한다.
// - 한 히스토그램의 네 계급 크기는 모두 같고 직사각형 사이에 틈을 두지 않는다.
// - 변량(mm)과 도수(마리)를 혼동하지 않는다. 표본 하나는 도수 1만 증가시킨다.
//
// 오개념 역산(misconceptionId): upper_inclusive, stale_interval, trust_origin,
// shift_lower_N, shift_upper_N. 모든 740개 원자 문항은 ValidateBank에서
// 정수 포함 관계·고유성·오배치 조건과 모든 오답 목적지의 구체 진단을 전수 검사한다.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;

namespace Mgf.SaeuIsa
{
    [Serializable]
    public sealed class ShrimpProblem
    {
        public string id = "";
        public int band;
        public int lower;
        public int width;
        public int valueMm;
        public int originClass = -1;
        public int correctClass;
        public string misconceptionId = "";

        // 런타임 상태. 문제 은행의 수학 데이터와 같은 객체를 복제해 사용한다.
        [NonSerialized] public bool firstSubmitted;
        [NonSerialized] public bool sealedSample;
        [NonSerialized] public int placedClass = -1;
        [NonSerialized] public float waitingAge;
    }

    public static class ShrimpRules
    {
        public const int ClassCount = 4;
        public const int SamplesPerBand = 8;
        public const int Goal = 24;
        public const int StartLives = 3;
        public const int MasteryTarget = 22;
        public const int RepairTarget = 7;
        public const float RunSeconds = 95f;
        public const float WaitingLimit = 22f;

        static readonly int[] LowerBand1 = { 0, 10, 20, 30, 40 };
        static readonly int[] LowerBand2 = { 0, 5, 10, 15, 20, 25, 30, 35, 40, 45 };
        static readonly int[] LowerBand3 = { 0, 10, 20, 30 };

        static int Positive(int x) { return x & 0x7fffffff; }

        public static int ClassOf(int valueMm, int lower, int width)
        {
            if (width <= 0 || valueMm < lower || valueMm >= lower + ClassCount * width) return -1;
            return (valueMm - lower) / width;
        }

        public static bool Belongs(ShrimpProblem p, int classIndex)
        {
            if (p == null || classIndex < 0 || classIndex >= ClassCount) return false;
            int lo = p.lower + classIndex * p.width;
            int hi = lo + p.width;
            return lo <= p.valueMm && p.valueMm < hi;
        }

        public static string Interval(int lower, int width, int classIndex)
        {
            int lo = lower + classIndex * width;
            return lo + " 이상 " + (lo + width) + " 미만";
        }

        public static string AxisIntervals(int lower, int width)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < ClassCount; i++)
            {
                if (i > 0) sb.Append(" / ");
                sb.Append(Interval(lower, width, i));
            }
            return sb.ToString();
        }

        static ShrimpProblem Make(int band, int lower, int width, int correctClass, int offset, int originClass, int serial)
        {
            int value = lower + correctClass * width + offset;
            return new ShrimpProblem
            {
                id = "hist-b" + band + "-" + serial.ToString("D3"),
                band = band,
                lower = lower,
                width = width,
                valueMm = value,
                originClass = originClass,
                correctClass = correctClass,
                placedClass = originClass,
                misconceptionId = originClass >= 0 ? "trust_origin" : ""
            };
        }

        public static ShrimpProblem FixedPractice()
        {
            var p = Make(0, 10, 10, 1, 3, -1, 0); // 20 ≤ 23 < 30
            p.id = "practice-23mm";
            return p;
        }

        public static ShrimpProblem FixedRepairPractice()
        {
            var p = Make(3, 10, 10, 3, 1, 2, 0); // 41 mm: [30,40)에 잘못 배치 → [40,50)
            p.id = "repair-practice-41mm";
            return p;
        }

        public static List<ShrimpProblem> BuildBank()
        {
            var bank = new List<ShrimpProblem>(740);
            int serial = 1;

            // band 1: 5 lower × 4 class × 9 interior offsets = 180.
            for (int a = 0; a < LowerBand1.Length; a++)
                for (int c = 0; c < ClassCount; c++)
                    for (int off = 1; off <= 9; off++)
                        bank.Add(Make(1, LowerBand1[a], 10, c, off, -1, serial++));

            // band 2: 10 lower × 4 class × {하한, 상한 바로 전} = 80.
            for (int a = 0; a < LowerBand2.Length; a++)
                for (int c = 0; c < ClassCount; c++)
                    for (int k = 0; k < 2; k++)
                        bank.Add(Make(2, LowerBand2[a], 5, c, k == 0 ? 0 : 4, -1, serial++));

            // band 3: 4 lower × 4 correct × 10 offsets × 3 wrong origins = 480.
            for (int a = 0; a < LowerBand3.Length; a++)
                for (int c = 0; c < ClassCount; c++)
                    for (int off = 0; off <= 9; off++)
                        for (int step = 1; step <= 3; step++)
                            bank.Add(Make(3, LowerBand3[a], 10, c, off, (c + step) % ClassCount, serial++));

            ValidateBank(bank);
            return bank;
        }

        public static List<ShrimpProblem> RunBand(int band, int runSerial)
        {
            var list = new List<ShrimpProblem>(SamplesPerBand);
            int lower;
            int width = band == 2 ? 5 : 10;
            if (band == 1) lower = LowerBand1[Positive(runSerial * 7 + 1) % LowerBand1.Length];
            else if (band == 2) lower = LowerBand2[Positive(runSerial * 11 + 3) % LowerBand2.Length];
            else lower = LowerBand3[Positive(runSerial * 13 + 2) % LowerBand3.Length];

            for (int i = 0; i < SamplesPerBand; i++)
            {
                int h = Positive(runSerial * 97 + band * 53 + i * 29 + i * i * 7);
                int c = h % ClassCount;
                int off;
                if (band == 1) off = 1 + Positive(h / 7 + i * 3) % 9;
                else if (band == 2) off = (i % 2 == 0) ? 0 : 4;
                else off = Positive(h / 5 + i * 7) % 10;
                int origin = band == 3 ? (c + 1 + Positive(h / 11) % 3) % ClassCount : -1;
                list.Add(Make(band, lower, width, c, off, origin, runSerial * 100 + band * 10 + i));
            }
            return list;
        }

        public static ShrimpProblem CloneForRun(ShrimpProblem p)
        {
            return new ShrimpProblem
            {
                id = p.id,
                band = p.band,
                lower = p.lower,
                width = p.width,
                valueMm = p.valueMm,
                originClass = p.originClass,
                correctClass = p.correctClass,
                misconceptionId = p.misconceptionId,
                placedClass = p.originClass
            };
        }

        static void DiagnoseWrong(ShrimpProblem p, int submittedClass, int previousLower, int previousWidth,
            out string misconceptionId, out string explanation)
        {
            misconceptionId = "";
            explanation = "";
            if (p == null || submittedClass < 0 || submittedClass >= ClassCount || submittedClass == p.correctClass) return;

            int selectedLo = p.lower + submittedClass * p.width;
            int selectedHi = selectedLo + p.width;
            int correctLo = p.lower + p.correctClass * p.width;
            int correctHi = correctLo + p.width;
            string correct = correctLo + " 이상 " + correctHi + " 미만";

            if (p.valueMm == correctLo && submittedClass == p.correctClass - 1)
            {
                misconceptionId = "upper_inclusive";
                explanation = p.valueMm + " mm는 " + selectedLo + " 이상 " + selectedHi + " 미만에 포함되지 않습니다";
                return;
            }
            int stale = ClassOf(p.valueMm, previousLower, previousWidth);
            if (p.band == 2 && stale >= 0 && stale != p.correctClass && submittedClass == stale)
            {
                misconceptionId = "stale_interval";
                explanation = "이전 계급의 크기가 아니라 현재 구간을 읽으시오 · " + p.valueMm + " mm는 " + correct + "입니다";
                return;
            }
            if (p.band == 3 && submittedClass == p.originClass)
            {
                misconceptionId = "trust_origin";
                explanation = "현재 위치를 믿지 말고 꼬리표를 확인하시오 · " + p.valueMm + " mm는 " + correct + "입니다";
                return;
            }

            int delta = submittedClass - p.correctClass;
            int distance = Math.Abs(delta);
            misconceptionId = (delta < 0 ? "shift_lower_" : "shift_upper_") + distance;
            explanation = p.valueMm + " mm를 " + distance + "계급 " + (delta < 0 ? "왼쪽" : "오른쪽") + "으로 잘못 옮겼습니다 · " + correct + "입니다";
        }

        public static string WrongMisconception(ShrimpProblem p, int submittedClass, int previousLower, int previousWidth)
        {
            DiagnoseWrong(p, submittedClass, previousLower, previousWidth, out string id, out _);
            return id;
        }

        public static string WrongExplanation(ShrimpProblem p, int submittedClass, int previousLower, int previousWidth)
        {
            DiagnoseWrong(p, submittedClass, previousLower, previousWidth, out _, out string explanation);
            return explanation;
        }

        public static MgfProblem ToMgf(ShrimpProblem p)
        {
            string action = p.band == 3
                ? "길이가 " + p.valueMm + " mm인 표본의 분류를 고치시오."
                : "길이가 " + p.valueMm + " mm인 표본을 알맞은 계급으로 옮기시오.";
            string origin = p.band == 3 ? " 현재 구간: " + Interval(p.lower, p.width, p.originClass) + "." : "";
            return new MgfProblem
            {
                id = p.id,
                prompt = action + origin + " 계급: " + AxisIntervals(p.lower, p.width),
                choices = null,
                answer = Interval(p.lower, p.width, p.correctClass),
                answerNumeric = p.lower + p.correctClass * p.width,
                unitConcept = p.band == 3 ? "오분류 수정과 히스토그램의 도수 변화" : "계급의 하한 포함·상한 제외와 도수"
            };
        }

        public static void ValidateBank(List<ShrimpProblem> bank)
        {
            if (bank == null || bank.Count != 740) throw new InvalidOperationException("문제 은행은 740개여야 한다.");
            var ids = new HashSet<string>();
            var prompts = new HashSet<string>();
            int b1 = 0, b2 = 0, b3 = 0;
            for (int i = 0; i < bank.Count; i++)
            {
                var p = bank[i];
                if (!ids.Add(p.id)) throw new InvalidOperationException("중복 id " + p.id);
                var mp = ToMgf(p);
                if (!prompts.Add(mp.prompt)) throw new InvalidOperationException("중복 prompt " + p.id);
                if (p.width <= 0) throw new InvalidOperationException("계급 크기 위반 " + p.id);
                int hit = 0;
                for (int c = 0; c < ClassCount; c++) if (Belongs(p, c)) hit++;
                if (hit != 1 || ClassOf(p.valueMm, p.lower, p.width) != p.correctClass)
                    throw new InvalidOperationException("유일 계급 위반 " + p.id);
                if (p.band == 3 && (p.originClass < 0 || p.originClass == p.correctClass || Belongs(p, p.originClass)))
                    throw new InvalidOperationException("수정 구역 오배치 위반 " + p.id);

                var diagnoses = new HashSet<string>();
                int priorLower = p.band == 2 ? p.lower : 10;
                int priorWidth = p.band == 2 ? 10 : p.width;
                for (int submitted = 0; submitted < ClassCount; submitted++)
                {
                    if (submitted == p.correctClass) continue;
                    DiagnoseWrong(p, submitted, priorLower, priorWidth, out string id, out string explanation);
                    if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(explanation) || id == "value_frequency_confusion")
                        throw new InvalidOperationException("오답 진단 누락 " + p.id + " class=" + submitted);
                    if (!explanation.Contains(p.valueMm + " mm"))
                        throw new InvalidOperationException("오답 진단 단위 누락 " + p.id + " class=" + submitted);
                    diagnoses.Add(id);
                }
                if (diagnoses.Count < 2) throw new InvalidOperationException("오답 진단 변별 부족 " + p.id);
                if (p.band == 1) b1++; else if (p.band == 2) b2++; else if (p.band == 3) b3++;
            }
            if (b1 != 180 || b2 != 80 || b3 != 480) throw new InvalidOperationException("밴드 개수 위반");
        }
    }
}
