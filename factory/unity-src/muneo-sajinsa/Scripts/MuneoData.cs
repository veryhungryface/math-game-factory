using System;
using System.Collections.Generic;
using System.Linq;
using Mgf;

namespace Mgf.MuneoSajinsa
{
    public enum TaskKind { Sum, Product, NumberCards }

    [Serializable]
    public sealed class PhotoTask
    {
        public string id;
        public TaskKind kind;
        public int band;
        public string prompt;
        public string[] leftLabels;
        public string[] rightLabels;
        public string[] validIds;
        public int answerCount;
        public string reveal;

        public bool IsValid(string photoId)
        {
            for (int i = 0; i < validIds.Length; i++) if (validIds[i] == photoId) return true;
            return false;
        }

        public string AnswerText()
        {
            var sorted = (string[])validIds.Clone();
            Array.Sort(sorted, StringComparer.Ordinal);
            return answerCount + "가지:[" + string.Join(",", sorted) + "]";
        }

        public MgfProblem ToMgf()
        {
            return new MgfProblem
            {
                id = id,
                prompt = prompt,
                choices = null,
                answer = AnswerText(),
                answerNumeric = answerCount,
                unitConcept = kind == TaskKind.Sum ? "경우의 수의 합" : kind == TaskKind.Product ? "경우의 수의 곱" : "카드로 두 자리 자연수 만들기"
            };
        }
    }

    public static class MuneoRules
    {
        public const int Goal = 6;
        public const int StartLives = 3;
        public const int ShotLimit = 14;
        public const int RunSeconds = 150;

        static readonly string[] Hats = { "고깔", "둥근 모자", "챙 모자", "조개 모자", "별 모자", "산호 모자" };
        static readonly string[] Flags = { "물결 깃발", "점 깃발", "줄 깃발", "별 깃발", "달 깃발", "산호 깃발" };

        // 중학교 정본 expression_traps 반영:
        // 1) '또는'은 동시에 일어나지 않는 두 집합으로만 생성한다.
        // 2) 숫자 카드는 십의 자리 0과 같은 카드 재사용을 자리쌍 정수로 판정한다.
        // 3) 확률, 비복원 확률, 순열·조합 공식, 중3 근호를 출제하지 않는다.
        public static PhotoTask Practice()
        {
            return BuildSum("practice-fixed", new[] { "고깔" }, new[] { "물결 깃발" }, 0);
        }

        public static PhotoTask SessionTask(int round, int runSeed)
        {
            int s = 37 + runSeed * 53 + round * 97;
            if (round == 0) return BuildSum("run-sum-2x2-" + s, Pick(Hats, 2, s), Pick(Flags, 2, s + 11), 1);
            if (round == 1) return BuildSum("run-sum-2x3-" + s, Pick(Hats, 2, s + 3), Pick(Flags, 3, s + 17), 1);
            if (round == 2) return BuildProduct("run-product-2x2-" + s, Pick(Hats, 2, s + 5), Pick(Flags, 2, s + 19), 2);
            if (round == 3) return BuildProduct("run-product-2x3-" + s, Pick(Hats, 2 + (runSeed & 1), s + 7), Pick(Flags, 3 - (runSeed & 1), s + 23), 2);
            if (round == 4) return BuildNumbers("run-number-3-" + s, Digits(3, s + 29), 3);
            return BuildNumbers("run-number-4-" + s, Digits(4, s + 31), 3);
        }

        public static List<MgfProblem> BuildBank()
        {
            var bank = new List<MgfProblem>(360);
            for (int i = 0; i < 120; i++)
            {
                int a = 2 + i % 2, b = 2 + (i / 2) % 2;
                bank.Add(BuildSum("sum-" + i, Pick(Hats, a, 101 + i * 7), Pick(Flags, b, 503 + i * 11), 1).ToMgf());
            }
            for (int i = 0; i < 120; i++)
            {
                int a = 2 + i % 2, b = 2 + (i / 3) % 2;
                bank.Add(BuildProduct("product-" + i, Pick(Hats, a, 907 + i * 13), Pick(Flags, b, 1301 + i * 17), 2).ToMgf());
            }
            for (int i = 0; i < 120; i++)
                bank.Add(BuildNumbers("number-" + i, Digits(3 + i % 2, 1709 + i * 19), 3).ToMgf());
            return bank;
        }

        public static bool IsExact(PhotoTask task, IList<string> photos, out string reason, out string misconceptionId)
        {
            reason = ""; misconceptionId = "";
            if (photos.Count == 0) { reason = "필름이 비어 있습니다."; misconceptionId = "empty-film"; return false; }
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < photos.Count; i++)
            {
                string id = photos[i];
                if (!task.IsValid(id))
                {
                    if (task.kind == TaskKind.NumberCards && id.Length >= 2 && id[0] == '0')
                    { reason = id + "은 두 자리 자연수가 아닙니다."; misconceptionId = "zero-in-tens"; }
                    else if (task.kind == TaskKind.NumberCards && id.Length >= 2 && id[0] == id[1])
                    { reason = "같은 숫자 카드는 두 번 쓸 수 없습니다."; misconceptionId = "reuse-one-card"; }
                    else { reason = "조건에 맞지 않는 사진이 있습니다."; misconceptionId = "invalid-composition"; }
                    return false;
                }
                if (!seen.Add(id)) { reason = id + " 사진이 중복되었습니다."; misconceptionId = "duplicate-case"; return false; }
            }
            if (photos.Count != task.answerCount)
            {
                reason = photos.Count < task.answerCount ? "아직 찍지 않은 모습이 있습니다." : "필름에 필요 없는 사진이 있습니다.";
                misconceptionId = task.kind == TaskKind.Sum ? "multiply-when-or" : task.kind == TaskKind.Product ? "add-when-and" : "miss-ordered-pair";
                return false;
            }
            for (int i = 0; i < task.validIds.Length; i++)
                if (!seen.Contains(task.validIds[i])) { reason = "아직 찍지 않은 모습이 있습니다."; misconceptionId = "missing-case"; return false; }
            return true;
        }

        public static List<string> DeliberateWrong(PhotoTask task, out string misconceptionId)
        {
            // distractor_rules를 실제 오개념에서 역산한다. 숫자 오답은 모든 숫자 풀에서 항상 조건 위반이다.
            var result = new List<string>();
            if (task.kind == TaskKind.Sum)
            {
                misconceptionId = "multiply-when-or";
                for (int a = 0; a < task.leftLabels.Length; a++)
                    for (int b = 0; b < task.rightLabels.Length; b++) result.Add("H" + a + "|F" + b);
                return result;
            }
            if (task.kind == TaskKind.Product)
            {
                misconceptionId = "add-when-and";
                for (int a = 0; a < task.leftLabels.Length; a++) result.Add("H" + a);
                for (int b = 0; b < task.rightLabels.Length; b++) result.Add("F" + b);
                return result;
            }
            misconceptionId = "zero-in-tens";
            result.Add("0" + task.rightLabels[1]);
            result.Add(task.leftLabels[1] + task.leftLabels[1]); // misconceptionId reuse-one-card도 판정 가능
            return result;
        }

        static PhotoTask BuildSum(string id, string[] hats, string[] flags, int band)
        {
            var valid = new List<string>();
            for (int i = 0; i < hats.Length; i++) valid.Add("H" + i);
            for (int i = 0; i < flags.Length; i++) valid.Add("F" + i);
            return new PhotoTask
            {
                id = id, kind = TaskKind.Sum, band = band, leftLabels = hats, rightLabels = flags,
                validIds = valid.ToArray(), answerCount = valid.Count,
                prompt = "모자 " + string.Join("·", hats) + " 또는 깃발 " + string.Join("·", flags) + " 중 하나만 착용한 모습을 모두 찍어 경우의 수를 나타내시오.",
                reveal = hats.Length + "+" + flags.Length + "=" + valid.Count + "가지"
            };
        }

        static PhotoTask BuildProduct(string id, string[] hats, string[] flags, int band)
        {
            var valid = new List<string>();
            for (int i = 0; i < hats.Length; i++) for (int j = 0; j < flags.Length; j++) valid.Add("H" + i + "|F" + j);
            return new PhotoTask
            {
                id = id, kind = TaskKind.Product, band = band, leftLabels = hats, rightLabels = flags,
                validIds = valid.ToArray(), answerCount = valid.Count,
                prompt = "모자 " + JoinWithParticle(hats, "과", "와") + " 깃발 " + JoinWithParticle(flags, "을", "를") + " 각각 하나씩 착용한 모습을 모두 찍어 경우의 수를 나타내시오.",
                reveal = hats.Length + "×" + flags.Length + "=" + valid.Count + "가지"
            };
        }

        static PhotoTask BuildNumbers(string id, string[] digits, int band)
        {
            var valid = new List<string>();
            for (int i = 0; i < digits.Length; i++)
                for (int j = 0; j < digits.Length; j++)
                    if (digits[i] != "0" && i != j) valid.Add(digits[i] + digits[j]);
            return new PhotoTask
            {
                id = id, kind = TaskKind.NumberCards, band = band, leftLabels = digits, rightLabels = (string[])digits.Clone(),
                validIds = valid.ToArray(), answerCount = valid.Count,
                prompt = JoinWithParticle(digits, "이", "가") + " 각각 하나씩 적힌 카드 중 2장으로 만들 수 있는 두 자리 자연수를 모두 찍어 경우의 수를 나타내시오. (단, 같은 카드는 두 번 쓰지 않는다.)",
                reveal = "십의 자리 0과 같은 카드 재사용을 빼면 " + valid.Count + "가지"
            };
        }

        static string JoinWithParticle(string[] words, string withBatchim, string withoutBatchim)
        {
            string joined = string.Join(words.Length > 1 && char.IsDigit(words[0][0]) ? ", " : "·", words);
            return joined + (HasBatchim(words[words.Length - 1]) ? withBatchim : withoutBatchim);
        }

        static bool HasBatchim(string word)
        {
            if (string.IsNullOrEmpty(word)) return false;
            char last = word[word.Length - 1];
            if (last >= '\uAC00' && last <= '\uD7A3') return (last - '\uAC00') % 28 != 0;
            if (last >= '0' && last <= '9')
            {
                // 한국어 수 읽기: 영/일/삼/육/칠/팔은 받침이 있다.
                return last == '0' || last == '1' || last == '3' || last == '6' || last == '7' || last == '8';
            }
            return false;
        }

        static string[] Pick(string[] source, int count, int seed)
        {
            var list = new List<string>(source);
            var rng = new Random(seed);
            for (int i = list.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); string t = list[i]; list[i] = list[j]; list[j] = t; }
            return list.Take(count).ToArray();
        }

        static string[] Digits(int count, int seed)
        {
            var values = new List<int>();
            for (int i = 1; i <= 9; i++) values.Add(i);
            var rng = new Random(seed);
            for (int i = values.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); int t = values[i]; values[i] = values[j]; values[j] = t; }
            var result = new string[count]; result[0] = "0";
            for (int i = 1; i < count; i++) result[i] = values[i - 1].ToString();
            return result;
        }
    }
}
