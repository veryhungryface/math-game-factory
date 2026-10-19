// 불꽃 점검 — 경우의 수 명단을 만드는 정수 ID 모델.
// curriculum/2022-middle-math.json style_guide / expression_traps:
// - 발문은 중학교 지시형 「조건을 만족하는 모든 경우를 빠짐없이 한 번씩 남기시오.」로 통일한다.
// - 합은 동시에 일어나지 않는 두 통 중 한 통에서 하나, 곱은 두 역할에서 각각 하나를 고르는 상황만 만든다.
// - 순열·조합 공식, 확률, 포함배제, 제곱근을 쓰지 않는다.
// - 학생에게 보이는 명단과 판정은 같은 문자열 정수 ID를 사용하며 전체 개수만 맞아도 통과시키지 않는다.
//
// 오개념 역산(misconceptionId):
// - firstAttributeOnly: 첫 항목이 같으면 완전한 순서쌍도 같다고 본다.
// - orTimesAndPlus: 「또는」를 곱으로, 「각각 하나」를 합으로 센다.
// - countOnlyIgnoresDuplicates: 명단 수만 맞으면 중복·누락을 무시한다.
// - arbitraryReplacement: 복제된 경우를 빠진 경우가 아닌 임의의 경우로 바꾼다.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;
using JsonUtility = UnityEngine.JsonUtility;

namespace Mgf.BulkkotJeomgeom
{
    [Serializable]
    public sealed class CaseProblem
    {
        public string id;
        public bool product;
        public int band;
        public int corruptionCount;
        public int[] aItems;
        public int[] bItems;
        public string[] canonicalCaseIds;
        public string[] initialCaseIds;
        public int[] correctionSlots;
        public string[] correctionValues;
        public string prompt;
        public string unitConcept;

        public int Count => canonicalCaseIds == null ? 0 : canonicalCaseIds.Length;
    }

    public static class BulkkotRules
    {
        [Serializable]
        sealed class RepairExport
        {
            public int slot;
            public string from;
            public string to;
        }

        [Serializable]
        sealed class ProblemExport
        {
            public string id;
            public string prompt;
            public string[] choices;
            public string answer;
            public double answerNumeric;
            public string unitConcept;
            public string[] canonicalCaseIds;
            public string[] initialCaseIds;
            public RepairExport[] validCorrectionExample;
            public string[] misconceptionIds;
        }

        [Serializable]
        sealed class BankExport { public ProblemExport[] items; }

        public const int StartLives = 3;
        public const int RoundCount = 6;
        public const int RequiredFirstCorrect = 5;
        public const float RunSeconds = 105f;

        public static readonly string[] Shapes = { "별", "원", "세모", "꽃", "달", "마름모" };
        public static readonly string[] Tails = { "직선", "물결", "점선", "갈래", "고리", "지그재그" };

        public static string CaseId(bool product, int a, int b)
        {
            return product ? "P:" + a + ":" + b : (a == 0 ? "A:" : "B:") + b;
        }

        public static bool TryDecode(string id, out bool product, out int a, out int b)
        {
            product = false; a = 0; b = 0;
            if (string.IsNullOrEmpty(id)) return false;
            string[] p = id.Split(':');
            if (p.Length == 3 && p[0] == "P")
            {
                product = true;
                return int.TryParse(p[1], out a) && int.TryParse(p[2], out b);
            }
            if (p.Length == 2 && (p[0] == "A" || p[0] == "B"))
            {
                a = p[0] == "A" ? 0 : 1;
                return int.TryParse(p[1], out b);
            }
            return false;
        }

        static int[] PickItems(int seed, int count, int salt)
        {
            var pool = new List<int> { 0, 1, 2, 3, 4, 5 };
            var r = new Random(unchecked(seed * 7919 + salt * 104729));
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = r.Next(i + 1); int t = pool[i]; pool[i] = pool[j]; pool[j] = t;
            }
            return pool.GetRange(0, count).ToArray();
        }

        static string JoinNames(int[] values, string[] names)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0) sb.Append('·');
                sb.Append(names[values[i]]);
            }
            return sb.ToString();
        }

        public static CaseProblem Make(int seed, bool product, int m, int n, int corruptions, int band)
        {
            var p = new CaseProblem
            {
                id = "case-" + seed + "-" + (product ? "p" : "s"),
                product = product,
                band = band,
                corruptionCount = corruptions,
                aItems = PickItems(seed, m, 17),
                bItems = PickItems(seed, n, 43),
                unitConcept = product ? "곱의 법칙과 완전한 순서쌍" : "합의 법칙과 중복 없는 나열"
            };
            var canonical = new List<string>();
            if (product)
            {
                for (int i = 0; i < p.aItems.Length; i++)
                    for (int j = 0; j < p.bItems.Length; j++)
                        canonical.Add(CaseId(true, p.aItems[i], p.bItems[j]));
                // 조사 결합을 피하면서 두 역할에서 하나씩 고른다는 조건을 그대로 드러낸다.
                p.prompt = "모양에서는 " + JoinNames(p.aItems, Shapes) + ", 꼬리에서는 " + JoinNames(p.bItems, Tails)
                    + " 중 각각 하나씩 골라 모든 경우를 빠짐없이 한 번씩 남기시오.";
            }
            else
            {
                for (int i = 0; i < p.aItems.Length; i++) canonical.Add(CaseId(false, 0, p.aItems[i]));
                for (int j = 0; j < p.bItems.Length; j++) canonical.Add(CaseId(false, 1, p.bItems[j]));
                p.prompt = "A통 " + JoinNames(p.aItems, Shapes) + " 또는 B통 " + JoinNames(p.bItems, Tails)
                    + " 중 한 통에서 하나를 고르는 모든 경우를 빠짐없이 한 번씩 남기시오.";
            }
            p.canonicalCaseIds = canonical.ToArray();
            p.initialCaseIds = (string[])p.canonicalCaseIds.Clone();

            var missing = new List<int>();
            var duplicate = new List<int>();
            var rng = new Random(unchecked(seed * 3571 + 97));
            int guard = 0;
            while (missing.Count < corruptions && guard++ < 200)
            {
                int mi = rng.Next(p.Count);
                if (missing.Contains(mi)) continue;
                bool mp; int ma, mb; TryDecode(p.canonicalCaseIds[mi], out mp, out ma, out mb);
                var candidates = new List<int>();
                for (int k = 0; k < p.Count; k++)
                {
                    if (k == mi || missing.Contains(k) || duplicate.Contains(k)) continue;
                    bool kp; int ka, kb; TryDecode(p.canonicalCaseIds[k], out kp, out ka, out kb);
                    if (!product)
                    {
                        if (ka == ma) candidates.Add(k); // 같은 통 안에서 한 고리 수정
                    }
                    else if ((missing.Count & 1) == 0 ? ka == ma && kb != mb : kb == mb && ka != ma)
                    {
                        candidates.Add(k); // 위/아래 고리를 번갈아 고치게 한다
                    }
                }
                if (candidates.Count == 0) continue;
                int di = candidates[rng.Next(candidates.Count)];
                missing.Add(mi); duplicate.Add(di);
            }
            if (missing.Count != corruptions) throw new InvalidOperationException("결함 생성 실패 " + p.id);
            p.correctionSlots = new int[corruptions];
            p.correctionValues = new string[corruptions];
            for (int i = 0; i < corruptions; i++)
            {
                p.initialCaseIds[missing[i]] = p.canonicalCaseIds[duplicate[i]];
                p.correctionSlots[i] = missing[i];
                p.correctionValues[i] = p.canonicalCaseIds[missing[i]];
            }
            return p;
        }

        public static CaseProblem Practice()
        {
            var p = new CaseProblem
            {
                id = "practice-sum-2-3", product = false, band = 0, corruptionCount = 1,
                aItems = new[] { 0, 1 }, bItems = new[] { 0, 1, 2 },
                prompt = "A통 별·원 또는 B통 직선·물결·점선 중 한 통에서 하나를 고르는 모든 경우를 빠짐없이 한 번씩 남기시오.",
                unitConcept = "합의 법칙과 중복 없는 나열"
            };
            p.canonicalCaseIds = new[] { "A:0", "A:1", "B:0", "B:1", "B:2" };
            p.initialCaseIds = new[] { "A:0", "A:0", "B:0", "B:1", "B:2" };
            p.correctionSlots = new[] { 1 }; p.correctionValues = new[] { "A:1" };
            return p;
        }

        public static List<CaseProblem> BuildBank()
        {
            var all = new List<CaseProblem>();
            var seen = new HashSet<string>();
            for (int seed = 1; seed <= 6000 && all.Count < 420; seed++)
            {
                bool product = (seed & 1) == 0;
                int m = 2 + (seed % 3), n = 2 + ((seed / 3) % 3);
                int count = product ? m * n : m + n;
                if ((!product && (count < 5 || count > 8)) || (product && (count < 6 || count > 12))) continue;
                int band = all.Count < 140 ? 1 : all.Count < 300 ? 2 : 3;
                int corruptions = band == 3 && count >= 8 ? 2 : 1;
                CaseProblem p;
                try { p = Make(seed, product, m, n, corruptions, band); }
                catch { continue; }
                if (seen.Add(p.prompt)) all.Add(p);
            }
            if (all.Count < 300) throw new InvalidOperationException("문제 은행 300개 미만: " + all.Count);
            for (int i = 0; i < all.Count; i++) ValidateGeneratedProblem(all[i]);
            return all;
        }

        static void ValidateGeneratedProblem(CaseProblem p)
        {
            if (p == null || p.Count < 5 || p.initialCaseIds == null || p.initialCaseIds.Length != p.Count)
                throw new InvalidOperationException("문항 크기 오류");
            if (new HashSet<string>(p.canonicalCaseIds).Count != p.Count)
                throw new InvalidOperationException("정본 중복 " + p.id);
            if (ValidateRoster(p, p.initialCaseIds))
                throw new InvalidOperationException("초기 명단이 정상 " + p.id);
            int expected = p.product ? p.aItems.Length * p.bItems.Length : p.aItems.Length + p.bItems.Length;
            int swappedRule = p.product ? p.aItems.Length + p.bItems.Length : p.aItems.Length * p.bItems.Length;
            if (p.Count != expected || swappedRule == expected)
                throw new InvalidOperationException("합·곱 역산 오답 우연 일치 " + p.id);
            if (p.correctionSlots == null || p.correctionValues == null ||
                p.correctionSlots.Length != p.corruptionCount || p.correctionValues.Length != p.corruptionCount)
                throw new InvalidOperationException("수정 예시 누락 " + p.id);
            var repaired = new List<string>(p.initialCaseIds);
            for (int i = 0; i < p.correctionSlots.Length; i++)
            {
                int slot = p.correctionSlots[i];
                if (slot < 0 || slot >= repaired.Count) throw new InvalidOperationException("수정 슬롯 범위 " + p.id);
                repaired[slot] = p.correctionValues[i];
            }
            if (!ValidateRoster(p, repaired)) throw new InvalidOperationException("수정 예시 실패 " + p.id);
        }

        public static string ExportBankJson(List<CaseProblem> problems)
        {
            var items = new ProblemExport[problems.Count];
            for (int i = 0; i < problems.Count; i++)
            {
                var p = problems[i];
                var repairs = new RepairExport[p.correctionSlots.Length];
                for (int k = 0; k < repairs.Length; k++) repairs[k] = new RepairExport
                {
                    slot = p.correctionSlots[k],
                    from = p.initialCaseIds[p.correctionSlots[k]],
                    to = p.correctionValues[k]
                };
                items[i] = new ProblemExport
                {
                    id = p.id, prompt = p.prompt, choices = null,
                    answer = p.Count.ToString(), answerNumeric = p.Count, unitConcept = p.unitConcept,
                    canonicalCaseIds = (string[])p.canonicalCaseIds.Clone(),
                    initialCaseIds = (string[])p.initialCaseIds.Clone(), validCorrectionExample = repairs,
                    misconceptionIds = new[] { "firstAttributeOnly", "orTimesAndPlus", "countOnlyIgnoresDuplicates", "arbitraryReplacement" }
                };
            }
            return JsonUtility.ToJson(new BankExport { items = items });
        }

        public static List<MgfProblem> ToBank(List<CaseProblem> problems)
        {
            var result = new List<MgfProblem>(problems.Count);
            foreach (var p in problems)
            {
                result.Add(new MgfProblem
                {
                    id = p.id,
                    prompt = p.prompt,
                    choices = null,
                    answer = p.Count.ToString(),
                    answerNumeric = p.Count,
                    unitConcept = p.unitConcept
                });
            }
            return result;
        }

        public static bool ValidateRoster(CaseProblem p, IList<string> roster)
        {
            if (p == null || roster == null || roster.Count != p.Count) return false;
            var want = new Dictionary<string, int>();
            var got = new Dictionary<string, int>();
            for (int i = 0; i < p.canonicalCaseIds.Length; i++)
            {
                string id = p.canonicalCaseIds[i];
                want[id] = want.TryGetValue(id, out int v) ? v + 1 : 1;
            }
            for (int i = 0; i < roster.Count; i++)
            {
                string id = roster[i];
                got[id] = got.TryGetValue(id, out int v) ? v + 1 : 1;
            }
            if (want.Count != got.Count) return false;
            foreach (var kv in want) if (!got.TryGetValue(kv.Key, out int v) || v != 1 || kv.Value != 1) return false;
            return true;
        }

        public static string Misconception(CaseProblem p, IList<string> roster)
        {
            if (p == null || roster == null) return "arbitraryReplacement";
            var unique = new HashSet<string>();
            bool sameFirstPair = false;
            for (int i = 0; i < roster.Count; i++)
            {
                if (!unique.Add(roster[i])) continue;
                bool pi; int ai, bi; TryDecode(roster[i], out pi, out ai, out bi);
                for (int j = i + 1; j < roster.Count; j++)
                {
                    bool pj; int aj, bj; TryDecode(roster[j], out pj, out aj, out bj);
                    if (pi && pj && ai == aj && bi != bj) sameFirstPair = true;
                }
            }
            if (p.product && sameFirstPair && unique.Count < roster.Count) return "firstAttributeOnly";
            if (roster.Count == p.Count && unique.Count < roster.Count) return "countOnlyIgnoresDuplicates";
            return "arbitraryReplacement";
        }

        public static int MissingCount(CaseProblem p, IList<string> roster)
        {
            var got = new HashSet<string>(roster);
            int n = 0; for (int i = 0; i < p.canonicalCaseIds.Length; i++) if (!got.Contains(p.canonicalCaseIds[i])) n++;
            return n;
        }

        public static int DuplicateCount(IList<string> roster)
        {
            return roster.Count - new HashSet<string>(roster).Count;
        }
    }
}
