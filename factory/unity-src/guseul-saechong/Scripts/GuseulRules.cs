// 구슬 새총 — 중2 확률 정수 모델과 제약 만족 문제 은행.
// curriculum/2022-middle-math.json 표현 함정:
// - 공은 반드시 "임의로" 꺼내며 "모양과 크기는 모두 같다"를 밝힌다.
// - 연속 추첨은 "확인한 후 다시 넣고"라고 써 복원 추출임을 명시한다.
// - 확률은 기약분수로 쓰고, 0과 1만 정수로 표시한다.
// - 중2 범위이므로 제곱근·조건부 확률·비복원 추출을 쓰지 않는다.
using System;
using System.Collections.Generic;
using Mgf;

namespace Mgf.GuseulSaechong
{
    [Serializable]
    public struct IntFraction
    {
        public int n;
        public int d;

        public IntFraction(int numerator, int denominator)
        {
            if (denominator == 0) throw new ArgumentException("분모는 0일 수 없습니다.");
            if (denominator < 0) { numerator = -numerator; denominator = -denominator; }
            int g = Gcd(Math.Abs(numerator), Math.Abs(denominator));
            n = numerator / g;
            d = denominator / g;
        }

        public static int Gcd(int a, int b)
        {
            while (b != 0) { int t = a % b; a = b; b = t; }
            return Math.Max(1, a);
        }

        public bool Same(IntFraction other) => n * other.d == other.n * d;
        public string Display => d == 1 ? n.ToString() : n + "/" + d;
    }

    public enum EventType { One, Not, Both, AtLeastOne }

    [Serializable]
    public sealed class MarbleProblem
    {
        public string id;
        public int band;
        public int N;
        public int K; // 목표색의 정답 개수. 0..N에서 조건 없이 균등 추출한다.
        public bool targetPurple;
        public EventType eventType;
        public int initialPurple;
        public int targetN;
        public int targetD;
        public string prompt;
        public string answer;
        public string concept;

        public int CorrectPurple => targetPurple ? K : N - K;
        public string TargetColor => targetPurple ? "보라" : "상아";
    }

    [Serializable]
    public struct DiagnosticCandidate
    {
        public string misconceptionId;
        public int purpleCount;
        public DiagnosticCandidate(string id, int count) { misconceptionId = id; purpleCount = count; }
    }

    public static class GuseulRules
    {
        public const int MaxMarbles = 12;
        public const int StartLives = 3;
        public const int ProblemCount = 6;
        public const int RequiredFirstCorrect = 4;

        static readonly int[] EarlyN = { 6, 8, 10, 12 };
        static readonly int[] LateN = { 4, 6, 8 };

        public static IntFraction EventProbability(EventType type, int eventColorCount, int total)
        {
            eventColorCount = Math.Max(0, Math.Min(total, eventColorCount));
            switch (type)
            {
                case EventType.Not:
                    return new IntFraction(total - eventColorCount, total);
                case EventType.Both:
                    return new IntFraction(eventColorCount * eventColorCount, total * total);
                case EventType.AtLeastOne:
                    return new IntFraction(total * total - (total - eventColorCount) * (total - eventColorCount), total * total);
                default:
                    return new IntFraction(eventColorCount, total);
            }
        }

        public static IntFraction MadeProbability(MarbleProblem p, int purpleCount)
        {
            int eventColorCount = p.targetPurple ? purpleCount : p.N - purpleCount;
            return EventProbability(p.eventType, eventColorCount, p.N);
        }

        // 실제 채점은 정수쌍의 교차곱만 사용한다. float/double 비교는 없다.
        public static bool IsCorrect(MarbleProblem p, int purpleCount)
        {
            IntFraction made = MadeProbability(p, purpleCount);
            return made.n * p.targetD == p.targetN * made.d;
        }

        public static MarbleProblem Make(int band, int total, int targetCount, bool targetPurple,
            EventType type, int initialPurple, string id)
        {
            total = Math.Max(1, Math.Min(MaxMarbles, total));
            targetCount = Math.Max(0, Math.Min(total, targetCount));
            initialPurple = Math.Max(0, Math.Min(total, initialPurple));
            IntFraction target = EventProbability(type, targetCount, total);
            var p = new MarbleProblem
            {
                id = id,
                band = band,
                N = total,
                K = targetCount,
                targetPurple = targetPurple,
                eventType = type,
                initialPurple = initialPurple,
                targetN = target.n,
                targetD = target.d
            };

            string color = p.TargetColor;
            string condition = "(단, 구슬의 모양과 크기는 모두 같다.)";
            if (type == EventType.One)
            {
                p.concept = "경우의 수의 비율로서의 확률";
                p.prompt = "구슬 " + total + "개 중 한 개를 임의로 꺼낼 때, " + color
                    + " 구슬이 나올 확률이 " + target.Display + "이 되도록 배합하시오. " + condition;
            }
            else if (type == EventType.Not)
            {
                p.concept = "어떤 사건이 일어나지 않을 확률";
                p.prompt = "구슬 " + total + "개 중 한 개를 임의로 꺼낼 때, " + color
                    + " 구슬이 나오지 않을 확률이 " + target.Display + "이 되도록 배합하시오. " + condition;
            }
            else if (type == EventType.Both)
            {
                p.concept = "서로 영향을 끼치지 않는 두 사건이 동시에 일어날 확률";
                p.prompt = "구슬 " + total + "개 중 한 개를 임의로 꺼내 확인한 후 다시 넣고 또 한 개를 임의로 꺼낸다. "
                    + "두 번 모두 " + color + " 구슬이 나올 확률이 " + target.Display
                    + "이 되도록 배합하시오. " + condition;
            }
            else
            {
                p.concept = "적어도 하나의 확률";
                p.prompt = "구슬 " + total + "개 중 한 개를 임의로 꺼내 확인한 후 다시 넣고 또 한 개를 임의로 꺼낸다. "
                    + "적어도 한 번 " + color + " 구슬이 나올 확률이 " + target.Display
                    + "이 되도록 배합하시오. " + condition;
            }
            p.answer = "보라 " + p.CorrectPurple + "개·상아 " + (total - p.CorrectPurple)
                + "개, 확률 " + target.Display;
            return p;
        }

        public static MarbleProblem Practice()
        {
            return Make(0, 6, 2, true, EventType.One, 0, "practice-fixed-2-of-6");
        }

        public static List<MarbleProblem> BuildRunDeck(Random targetRng, Random initialRng, bool[] previousColors, int previousN)
        {
            var deck = new List<MarbleProblem>(ProblemCount);
            for (int i = 0; i < ProblemCount; i++)
            {
                int band = i < 2 ? 1 : i < 4 ? 2 : 3;
                int[] pool = band < 3 ? EarlyN : LateN;
                int total = pool[targetRng.Next(pool.Length)];
                int k = targetRng.Next(total + 1); // 0..N 조건 없는 균등 분포.
                bool purple = targetRng.Next(2) == 0;
                EventType type = band == 1 ? EventType.One : band == 2 ? EventType.Not
                    : (i == 4 ? EventType.Both : EventType.AtLeastOne);

                // 초기 배합은 목표 K와 독립이다. 공통 번호의 색만 보존하고 추가 구슬은 별도 난수 스트림을 쓴다.
                int initialPurple = 0;
                for (int j = 0; j < total; j++)
                {
                    bool value = j < previousN && previousColors != null && j < previousColors.Length
                        ? previousColors[j]
                        : initialRng.Next(2) == 0;
                    if (value) initialPurple++;
                }
                deck.Add(Make(band, total, k, purple, type, initialPurple,
                    "run-b" + band + "-o" + (i + 1) + "-n" + total + "-k" + k + "-c" + (purple ? "p" : "i")));
            }
            return deck;
        }

        // 실제 게임의 Make/EventProbability/IsCorrect 경로와 같은 은행이다.
        // 초기 배합 수를 본문 뒤의 실제 자료 문장으로 바꾸어 핵심 발문 다양성도 확보한다.
        public static List<MgfProblem> BuildBank()
        {
            var bank = new List<MgfProblem>(520);
            int serial = 1;
            EventType[] types = { EventType.One, EventType.Not, EventType.Both, EventType.AtLeastOne };
            for (int ti = 0; ti < types.Length; ti++)
            {
                EventType type = types[ti];
                int band = type == EventType.One ? 1 : type == EventType.Not ? 2 : 3;
                int[] pool = band < 3 ? EarlyN : LateN;
                for (int ni = 0; ni < pool.Length; ni++)
                for (int k = 0; k <= pool[ni]; k++)
                for (int color = 0; color < 2; color++)
                for (int variant = 0; variant < 2; variant++)
                {
                    int total = pool[ni];
                    int initial = (k * 3 + variant * 5 + color + ni) % (total + 1);
                    MarbleProblem p = Make(band, total, k, color == 0, type, initial, "bank-" + serial);
                    string prompt = p.prompt + " 현재 배합판에는 보라 " + initial + "개와 상아 "
                        + (total - initial) + "개가 놓여 있다.";
                    bank.Add(new MgfProblem
                    {
                        id = p.id,
                        prompt = prompt,
                        choices = null,
                        answer = p.answer,
                        answerNumeric = p.targetN / (double)p.targetD,
                        unitConcept = p.concept
                    });
                    serial++;
                }
            }
            return bank;
        }

        static int TargetColorToPurple(MarbleProblem p, int targetColorCount)
            => p.targetPurple ? targetColorCount : p.N - targetColorCount;

        // 오답 선택 버튼은 없지만, 오답 배합을 오개념에서 역산해 피드백과 검산에 사용한다.
        // 후처리 순서: 정답 제거 → 범위 검사 → 중복 제거 → 부족분 보충.
        public static List<DiagnosticCandidate> DiagnosticCandidates(MarbleProblem p)
        {
            var raw = new List<DiagnosticCandidate>();
            raw.Add(new DiagnosticCandidate("equiprobable_half", TargetColorToPurple(p, p.N / 2)));

            // 기약분수의 분자를 실제 개수로 복사.
            raw.Add(new DiagnosticCandidate("numerator_copy", TargetColorToPurple(p, p.targetN)));

            if (p.eventType == EventType.Not)
            {
                // 나오지 않을 확률에도 목표색의 한 번 확률을 그대로 쓴다.
                if ((p.N * p.targetN) % p.targetD == 0)
                    raw.Add(new DiagnosticCandidate("complement_ignored",
                        TargetColorToPurple(p, p.N * p.targetN / p.targetD)));
            }
            if (p.eventType == EventType.Both || p.eventType == EventType.AtLeastOne)
            {
                // 두 번 사건을 한 번의 확률로 복사.
                if ((p.N * p.targetN) % p.targetD == 0)
                    raw.Add(new DiagnosticCandidate("single_trial_copy",
                        TargetColorToPurple(p, p.N * p.targetN / p.targetD)));
            }

            var result = new List<DiagnosticCandidate>();
            var used = new HashSet<int>();
            for (int i = 0; i < raw.Count; i++)
            {
                int v = raw[i].purpleCount;
                if (v < 0 || v > p.N || IsCorrect(p, v) || !used.Add(v)) continue;
                result.Add(raw[i]);
            }
            for (int delta = 1; result.Count < 3 && delta <= p.N; delta++)
            {
                int[] values = { p.CorrectPurple - delta, p.CorrectPurple + delta };
                for (int i = 0; i < values.Length && result.Count < 3; i++)
                {
                    int v = values[i];
                    if (v < 0 || v > p.N || IsCorrect(p, v) || !used.Add(v)) continue;
                    result.Add(new DiagnosticCandidate("near_count_" + delta, v));
                }
            }
            return result;
        }

        public static string ClassifyWrong(MarbleProblem p, int purpleCount)
        {
            List<DiagnosticCandidate> list = DiagnosticCandidates(p);
            for (int i = 0; i < list.Count; i++)
                if (list[i].purpleCount == purpleCount) return list[i].misconceptionId;
            return "event_count_mismatch";
        }

        public static string EventCode(EventType type)
        {
            switch (type)
            {
                case EventType.Not: return "not";
                case EventType.Both: return "both";
                case EventType.AtLeastOne: return "atLeastOne";
                default: return "one";
            }
        }

        public static void ValidateAll()
        {
            EventType[] types = { EventType.One, EventType.Not, EventType.Both, EventType.AtLeastOne };
            for (int ti = 0; ti < types.Length; ti++)
            {
                EventType type = types[ti];
                int band = type == EventType.One ? 1 : type == EventType.Not ? 2 : 3;
                int[] pool = band < 3 ? EarlyN : LateN;
                for (int ni = 0; ni < pool.Length; ni++)
                for (int k = 0; k <= pool[ni]; k++)
                for (int color = 0; color < 2; color++)
                {
                    MarbleProblem p = Make(band, pool[ni], k, color == 0, type, 0, "audit");
                    int solutions = 0;
                    for (int purple = 0; purple <= p.N; purple++) if (IsCorrect(p, purple)) solutions++;
                    if (solutions != 1 || !IsCorrect(p, p.CorrectPurple))
                        throw new InvalidOperationException("정답 배합 비유일/누락: " + type + " N=" + p.N + " K=" + k);
                    List<DiagnosticCandidate> d = DiagnosticCandidates(p);
                    if (d.Count < 2)
                        throw new InvalidOperationException("오개념 역산 후보 부족: " + type + " N=" + p.N + " K=" + k);
                    for (int i = 0; i < d.Count; i++)
                        if (IsCorrect(p, d[i].purpleCount))
                            throw new InvalidOperationException("오개념 후보 우연 정답: " + d[i].misconceptionId);
                }
            }
            if (BuildBank().Count < 300) throw new InvalidOperationException("문제 은행 300개 미만");
        }
    }
}
