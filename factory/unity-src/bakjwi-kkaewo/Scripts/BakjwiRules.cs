// 박쥐 깨워 — 닮은 삼각형 집합을 한 획으로 고르는 정수 생성기와 판정기.
//
// curriculum/2022-middle-math.json style_guide / expression_traps 를 생성기에 옮겨 적는다:
// - 발문은 중학교 교과서 지시형 「~하시오」로 통일한다(청유형과 섞지 않는다).
// - 길이는 `6 cm`, 각은 `60°`처럼 쓴다. 단위를 빠뜨리지 않는다.
// - △ABC∽△DEF 는 대응 꼭짓점 순서로 쓴다. 이 순서는 화면 위치가 아니라 도형 자료(변의 제곱·각)로
//   계산한 대응에서 만든다. 기호 순서만으로 실제 닮은 도형을 오답 처리하지 않는다.
// - 중2 범위: √·무리수·삼각비를 생성하지 않는다. SAS 셋째 변은 내부 판정용 제곱값으로만 쓰고 보여 주지 않는다.
// - 닮음비는 「가장 간단한 자연수의 비」. 넓이의 비·부피의 비는 이 게임에서 다루지 않는다.
//
// 오개념 역산(misconceptionId) — 오답 날개는 모두 아래 중 하나를 역산한 값이다:
// - sss_two_sides            : 두 대응변의 비만 같으면 SSS 닮음이라고 판단한다.   (qa, qb, qc±1)
// - additive_lengths         : 대응변에 같은 수를 더해도 닮음이 유지된다고 판단한다. (a+d, b+d, c+d)
// - sas_ignore_included_angle: 두 변의 비만 같으면 끼인각을 보지 않아도 된다.     (qa, qb, θ′≠θ)
// - aa_one_angle             : 각 하나만 같으면 AA 닮음이라고 판단한다.          (α, β±3~5, 나머지)
// - sas_one_ratio            : (보조) 끼인각은 같지만 한 쌍의 대응변 비만 맞춘다.   (qa, qb±1, θ)
// 닮지 않은 날개는 그림의 세 각이 기준과 3.5° 이내인 근접 반례만 쓴다(끼인각 반례 제외) — 눈대중이 아니라 수치로 가르게.
//
// 판정은 전부 정수: SSS·SAS 는 세 변의 제곱을 정렬해 교차곱, AA 는 정렬한 세 각의 일치.
using System;
using System.Collections.Generic;
using System.Text;
using Mgf;

namespace Mgf.BakjwiKkaewo
{
    public enum SimilarityBand { SSS = 1, SAS = 2, AA = 3 }

    [Serializable]
    public sealed class WingData
    {
        // SSS: v = {DE, EF, FD} (cm) · SAS: v = {DE, DF, ∠D} · AA: v = {∠D, ∠E, ∠F(=180−∠D−∠E, 숨김)}
        public int[] v = new int[3];
        public string[] letters = { "D", "E", "F" };
        public string misconceptionId = "";
        public int rotDeg;      // 화면 회전(정답과 독립)
        public bool flip;       // 뒤집힌 도형(AA 단계, 정답과 독립)
        public int sizePct = 100; // 표시 크기(정답과 독립)
    }

    [Serializable]
    public sealed class BatScene
    {
        public string id = "";
        public SimilarityBand band;
        public int serial;
        public int[] target = new int[3];
        public WingData[] wings = new WingData[4];
        public int correctMask;
        public string prompt = "";
        public string answer = "";
        public string reveal = "";
        public string concept = "";
    }

    public static class BakjwiRules
    {
        public const int CandidateCount = 4;
        public const int Goal = 10;
        public const int StartLives = 3;
        public const float RunSeconds = 120f;
        public const int MasteryFirst = 8;
        public const int MasteryAa = 3;
        public static readonly int[] BandCounts = { 3, 3, 4 };

        static readonly int[][] SssBases = {
            new[]{3,4,5}, new[]{4,5,6}, new[]{4,6,7}, new[]{5,6,8}, new[]{5,7,9}, new[]{3,5,7}, new[]{4,7,9}
        };
        // 두 변 a,b(서로 다름)와 끼인각. 배수는 언제나 자연수다.
        static readonly int[][] SasBases = {
            new[]{3,5,60}, new[]{4,6,90}, new[]{5,7,120}, new[]{4,7,60},
            new[]{5,8,90}, new[]{6,9,120}, new[]{3,8,60}, new[]{2,5,90}, new[]{3,4,120}
        };
        static readonly int[][] AaBases = {
            new[]{40,60,80}, new[]{45,60,75}, new[]{30,70,80}, new[]{35,65,80},
            new[]{50,55,75}, new[]{45,55,80}, new[]{40,65,75}, new[]{35,60,85}
        };
        public static readonly string[] WingIds = { "1", "2", "3", "4" };
        static readonly string[][] WingLetters = {
            new[]{"D","E","F"}, new[]{"G","H","I"}, new[]{"J","K","L"}, new[]{"P","Q","R"}
        };
        static readonly int[][] Perms = {
            new[]{0,1,2}, new[]{1,2,0}, new[]{2,0,1}, new[]{0,2,1}, new[]{2,1,0}, new[]{1,0,2}
        };

        static int Pos(int x) { return x & 0x7fffffff; }
        static int Hash(int a, int b) { unchecked { int h = a * 73856093 ^ b * 19349663; h ^= h >> 13; h *= 0x5bd1e995; h ^= h >> 15; return Pos(h); } }

        // ───────────── 정수 판정 ─────────────
        static void Sort3(long[] a)
        {
            if (a[0] > a[1]) { long t = a[0]; a[0] = a[1]; a[1] = t; }
            if (a[1] > a[2]) { long t = a[1]; a[1] = a[2]; a[2] = t; }
            if (a[0] > a[1]) { long t = a[0]; a[0] = a[1]; a[1] = t; }
        }

        static bool ProportionalSorted(long[] a, long[] b)
        {
            Sort3(a); Sort3(b);
            return a[0] * b[1] == a[1] * b[0] && a[1] * b[2] == a[2] * b[1];
        }

        public static bool IsTriangle(int a, int b, int c)
        {
            return a > 0 && b > 0 && c > 0 && a + b > c && a + c > b && b + c > a;
        }

        public static bool IsSimilarSss(int[] t, int[] c)
        {
            if (t == null || c == null || t.Length < 3 || c.Length < 3) return false;
            if (!IsTriangle(t[0], t[1], t[2]) || !IsTriangle(c[0], c[1], c[2])) return false;
            return ProportionalSorted(new[] { (long)t[0] * t[0], (long)t[1] * t[1], (long)t[2] * t[2] },
                                      new[] { (long)c[0] * c[0], (long)c[1] * c[1], (long)c[2] * c[2] });
        }

        // 끼인각 θ 의 2cosθ 를 정수로: 60°→1, 90°→0, 120°→−1. 셋째 변² = a²+b²−k·ab.
        public static int CosCoefficient(int angle) { return angle == 60 ? 1 : angle == 90 ? 0 : angle == 120 ? -1 : 99; }

        public static long ThirdSideSquared(int a, int b, int angle)
        {
            int k = CosCoefficient(angle);
            if (k == 99 || a <= 0 || b <= 0) return -1;
            return (long)a * a + (long)b * b - (long)k * a * b;
        }

        public static bool IsSimilarSas(int[] t, int[] c)
        {
            if (t == null || c == null || t.Length < 3 || c.Length < 3) return false;
            long tc = ThirdSideSquared(t[0], t[1], t[2]), cc = ThirdSideSquared(c[0], c[1], c[2]);
            if (tc <= 0 || cc <= 0) return false;
            return ProportionalSorted(new[] { (long)t[0] * t[0], (long)t[1] * t[1], tc },
                                      new[] { (long)c[0] * c[0], (long)c[1] * c[1], cc });
        }

        public static bool IsSimilarAa(int[] t, int[] c)
        {
            if (t == null || c == null || t.Length < 2 || c.Length < 2) return false;
            int tc = 180 - t[0] - t[1], cc = 180 - c[0] - c[1];
            if (t[0] <= 0 || t[1] <= 0 || tc <= 0 || c[0] <= 0 || c[1] <= 0 || cc <= 0) return false;
            long[] a = { t[0], t[1], tc }, b = { c[0], c[1], cc };
            Sort3(a); Sort3(b);
            return a[0] == b[0] && a[1] == b[1] && a[2] == b[2];
        }

        public static bool IsSimilar(BatScene s, WingData w)
        {
            if (s == null || w == null) return false;
            if (s.band == SimilarityBand.SSS) return IsSimilarSss(s.target, w.v);
            if (s.band == SimilarityBand.SAS) return IsSimilarSas(s.target, w.v);
            return IsSimilarAa(s.target, w.v);
        }

        public static int ComputeMask(BatScene s)
        {
            int m = 0;
            for (int i = 0; i < CandidateCount; i++) if (IsSimilar(s, s.wings[i])) m |= 1 << i;
            return m;
        }

        // ───────────── 날개 생성(오개념 역산) ─────────────
        static WingData Wing(int a, int b, int c, string misconception, int serial, int slot, SimilarityBand band)
        {
            int h = Hash(serial, slot + 11);
            return new WingData {
                v = new[] { a, b, c }, letters = WingLetters[slot], misconceptionId = misconception,
                rotDeg = h % 360, flip = ((h >> 9) & 1) == 1,
                sizePct = 80 + (h >> 4) % 21
            };
        }

        static int[] Permute(int[] x, int perm) { int[] p = Perms[perm % 6]; return new[] { x[p[0]], x[p[1]], x[p[2]] }; }

        // ───────────── 근접 반례(실루엣 차단) ─────────────
        // 그림은 실제 자료대로 그린다. 그래서 닮지 않은 날개는 기준과 모양이 거의 같은 「근접 반례」여야 한다 —
        // 눈대중(실루엣)으로는 못 가르고 수치를 읽어야만 가를 수 있게. 아래 값은 그림용 실수 계산이며 판정에는 쓰지 않는다.
        public const double NearMissMaxDeg = 3.5, NearMissPreferDeg = 2.0, AaNearMissMaxDeg = 5.0;
        public static double NearMissLimit(SimilarityBand band) { return band == SimilarityBand.AA ? AaNearMissMaxDeg : NearMissMaxDeg; }

        public static double[] SortedAngles(SimilarityBand band, int[] v)
        {
            double a, b, c;
            double[] r;
            if (band == SimilarityBand.AA) r = new double[] { v[0], v[1], 180 - v[0] - v[1] };
            else
            {
                if (band == SimilarityBand.SSS) { a = v[0]; b = v[1]; c = v[2]; }
                else { a = v[0]; b = v[1]; c = Math.Sqrt(a * a + b * b - 2 * a * b * Math.Cos(v[2] * Math.PI / 180)); }
                double A = Math.Acos(Math.Max(-1, Math.Min(1, (b * b + c * c - a * a) / (2 * b * c)))) * 180 / Math.PI;
                double B = Math.Acos(Math.Max(-1, Math.Min(1, (a * a + c * c - b * b) / (2 * a * c)))) * 180 / Math.PI;
                r = new[] { A, B, 180 - A - B };
            }
            Array.Sort(r); return r;
        }

        public static double ShapeDeviation(SimilarityBand band, int[] target, int[] v)
        {
            double[] t = SortedAngles(band, target), c = SortedAngles(band, v);
            return Math.Max(Math.Abs(t[0] - c[0]), Math.Max(Math.Abs(t[1] - c[1]), Math.Abs(t[2] - c[2])));
        }

        // 제약 만족 풀: 조건을 만족하는 후보를 전부 나열한 뒤 해시로 하나를 고른다(사후 필터 무한 루프 없음).
        sealed class Cand { public int[] v; public string mis; }

        static List<Cand> SssPool(int[] b, int m, int M, int[] target, bool correct)
        {
            var pool = new List<Cand>();
            if (correct)
            {
                for (int q = 1; q <= M; q++) if (q != m) pool.Add(new Cand { v = new[] { b[0] * q, b[1] * q, b[2] * q }, mis = "" });
                return pool;
            }
            // 두 대응변의 비만 같다: 기본형의 q배에서 한 변만 ±1
            for (int q = 2; q <= M + 1; q++) for (int i = 0; i < 3; i++) for (int d = -1; d <= 1; d += 2)
            {
                int[] v = { b[0] * q, b[1] * q, b[2] * q }; v[i] += d;
                pool.Add(new Cand { v = v, mis = "sss_two_sides" });
            }
            // 대응변에 같은 수를 더했다: 기준 + d
            for (int d = 1; d <= 3; d++) pool.Add(new Cand { v = new[] { target[0] + d, target[1] + d, target[2] + d }, mis = "additive_lengths" });
            for (int d = 1; d <= 2; d++) if (target[0] - d >= 2) pool.Add(new Cand { v = new[] { target[0] - d, target[1] - d, target[2] - d }, mis = "additive_lengths" });
            return pool;
        }

        static List<Cand> SasPool(int[] b, int m, int M, int[] target, bool correct)
        {
            var pool = new List<Cand>();
            if (correct)
            {
                for (int q = 1; q <= M; q++) if (q != m) pool.Add(new Cand { v = new[] { b[0] * q, b[1] * q, b[2] }, mis = "" });
                return pool;
            }
            // 두 변의 비가 같지만 끼인각이 다르다(눈에 띄는 반례 — 오개념 확인용, 장면당 많아야 하나)
            for (int q = 1; q <= M; q++) foreach (int ang in new[] { 60, 90, 120 }) if (ang != b[2])
                pool.Add(new Cand { v = new[] { b[0] * q, b[1] * q, ang }, mis = "sas_ignore_included_angle" });
            // 끼인각은 같지만 한 쌍의 대응변 비가 어긋난다(근접 반례)
            for (int q = 2; q <= M + 1; q++) for (int i = 0; i < 2; i++) for (int d = -1; d <= 1; d += 2)
            {
                int[] v = { b[0] * q, b[1] * q, b[2] }; v[i] += d;
                pool.Add(new Cand { v = v, mis = "sas_one_ratio" });
            }
            // 대응변에 같은 수를 더했다
            for (int d = 1; d <= 3; d++) pool.Add(new Cand { v = new[] { target[0] + d, target[1] + d, b[2] }, mis = "additive_lengths" });
            return pool;
        }

        static List<Cand> AaPool(int[] b, bool correct)
        {
            var pool = new List<Cand>();
            for (int s = 0; s < 3; s++) for (int o = 0; o < 2; o++)
            {
                int x = b[s], y = b[(s + 1 + o) % 3];
                if (correct) { pool.Add(new Cand { v = new[] { x, y, 180 - x - y }, mis = "" }); continue; }
                // 각 하나만 같고 다른 하나는 δ° 어긋난다
                foreach (int d in new[] { -5, -4, -3, 3, 4, 5 })
                    pool.Add(new Cand { v = new[] { x, y + d, 180 - x - y - d }, mis = "aa_one_angle" });
            }
            return pool;
        }

        static bool CandOk(SimilarityBand band, int[] target, Cand c, bool correct)
        {
            int[] v = c.v;
            if (band == SimilarityBand.AA)
            {
                if (v[0] <= 0 || v[1] <= 0 || v[2] <= 0) return false;
                return IsSimilarAa(target, v) == correct;
            }
            if (band == SimilarityBand.SSS)
            {
                if (!IsTriangle(v[0], v[1], v[2]) || Math.Max(v[0], Math.Max(v[1], v[2])) > 36 || Math.Min(v[0], Math.Min(v[1], v[2])) < 2) return false;
                if (IsSimilarSss(target, v) != correct) return false;
            }
            else
            {
                if (v[0] < 2 || v[1] < 2 || v[0] > 36 || v[1] > 36 || v[0] == v[1]) return false;
                if (IsSimilarSas(target, v) != correct) return false;
                if (correct && v[2] != target[2]) return false;
            }
            if (!correct && c.mis != "sas_ignore_included_angle" && ShapeDeviation(band, target, v) > NearMissLimit(band)) return false;
            return true;
        }

        static bool SameNumbers(SimilarityBand band, WingData a, WingData b)
        {
            if (band == SimilarityBand.AA)
            {
                int a0 = Math.Min(a.v[0], a.v[1]), a1 = Math.Max(a.v[0], a.v[1]), b0 = Math.Min(b.v[0], b.v[1]), b1 = Math.Max(b.v[0], b.v[1]);
                return a0 == b0 && a1 == b1;
            }
            if (band == SimilarityBand.SAS)
                return a.v[2] == b.v[2] && Math.Min(a.v[0], a.v[1]) == Math.Min(b.v[0], b.v[1]) && Math.Max(a.v[0], a.v[1]) == Math.Max(b.v[0], b.v[1]);
            int[] x = { a.v[0], a.v[1], a.v[2] }, y = { b.v[0], b.v[1], b.v[2] };
            Array.Sort(x); Array.Sort(y);
            return x[0] == y[0] && x[1] == y[1] && x[2] == y[2];
        }

        public static BatScene BuildScene(SimilarityBand band, int mask, int serial)
        {
            mask = Math.Max(1, Math.Min(14, mask));
            var p = new BatScene { band = band, serial = serial, correctMask = mask };
            int[][] bases = band == SimilarityBand.SSS ? SssBases : band == SimilarityBand.SAS ? SasBases : AaBases;
            int[] b = bases[Hash(serial, mask) % bases.Length];
            int m = 1, M = 1;
            if (band == SimilarityBand.AA)
            {
                // 기준은 세 각 중 두 각을 보여 준다(어느 둘인지는 장면마다 다르다)
                int s = Hash(serial, 3) % 3;
                p.target = new[] { b[s], b[(s + 1) % 3], b[(s + 2) % 3] };
            }
            else
            {
                int big = band == SimilarityBand.SSS ? Math.Max(b[0], Math.Max(b[1], b[2])) : Math.Max(b[0], b[1]);
                M = Math.Min(6, 36 / big);
                m = M >= 2 ? 2 + Hash(serial, 7) % (M - 1) : 1;   // 기준 = 기본형의 m배(2배 이상 → 2:3·3:4 같은 닮음비)
                p.target = band == SimilarityBand.SSS ? new[] { b[0] * m, b[1] * m, b[2] * m } : new[] { b[0] * m, b[1] * m, b[2] };
            }
            p.concept = band == SimilarityBand.SSS ? "SSS 닮음과 대응변의 길이의 비" : band == SimilarityBand.SAS ? "SAS 닮음과 끼인각" : "AA 닮음과 대응각";
            p.id = band.ToString().ToLowerInvariant() + "-" + serial.ToString("D3") + "-m" + mask.ToString("D2");
            int angleChanges = 0;
            for (int i = 0; i < CandidateCount; i++)
            {
                bool correct = (mask & (1 << i)) != 0;
                List<Cand> pool = band == SimilarityBand.SSS ? SssPool(b, m, M, p.target, correct)
                    : band == SimilarityBand.SAS ? SasPool(b, m, M, p.target, correct) : AaPool(b, correct);
                var ok = new List<Cand>(pool.Count);
                // SAS 끼인각 반례는 3장면 중 1장면꼴로만 허용(실루엣으로 바로 보이는 반례가 덱을 지배하지 않게)
                bool allowAngle = band == SimilarityBand.SAS && angleChanges == 0 && Hash(serial, 40 + i) % 3 == 0;
                foreach (Cand c in pool)
                {
                    if (!CandOk(band, p.target, c, correct)) continue;
                    if (c.mis == "sas_ignore_included_angle" && !allowAngle) continue;
                    bool dup = false;
                    var probe = new WingData { v = c.v };
                    for (int j = 0; j < i; j++) if (SameNumbers(band, p.wings[j], probe)) dup = true;
                    if (!dup) ok.Add(c);
                }
                if (ok.Count == 0) return null;
                if (!correct && band != SimilarityBand.AA)
                {
                    // 더 가까운 근접 반례(그림의 세 각 차 2° 이내)가 있으면 그쪽에서만 고른다
                    var close = ok.FindAll(c => c.mis != "sas_ignore_included_angle" && ShapeDeviation(band, p.target, c.v) <= NearMissPreferDeg);
                    var angleOnes = ok.FindAll(c => c.mis == "sas_ignore_included_angle");
                    if (close.Count > 0) { if (angleOnes.Count > 0 && Hash(serial, 50 + i) % 2 == 0) close = angleOnes; ok = close; }
                }
                Cand pick = ok[Hash(serial * 13 + i, mask) % ok.Count];
                if (pick.mis == "sas_ignore_included_angle") angleChanges++;
                int[] v = pick.v;
                if (band == SimilarityBand.SSS) v = Permute(v, Hash(serial, 20 + i) % 6);
                else if (band == SimilarityBand.SAS && Hash(serial, 30 + i) % 2 == 1) v = new[] { v[1], v[0], v[2] };
                else if (band == SimilarityBand.AA && Hash(serial, 30 + i) % 2 == 1) v = new[] { v[1], v[0], v[2] };
                p.wings[i] = Wing(v[0], v[1], v[2], pick.mis, serial, i, band);
            }
            p.answer = MaskAnswer(mask);
            p.prompt = BuildPrompt(p, "구역 " + serial.ToString("D3"));
            p.reveal = band == SimilarityBand.SSS ? "세 대응변의 길이의 비가 같다 → SSS 닮음"
                : band == SimilarityBand.SAS ? "두 대응변의 길이의 비가 같고 그 끼인각이 같다 → SAS 닮음"
                : "두 대응각의 크기가 각각 같다 → AA 닮음";
            return p;
        }

        public static string TargetText(BatScene p)
        {
            if (p.band == SimilarityBand.SSS)
                return "기준 △ABC: AB=" + p.target[0] + " cm, BC=" + p.target[1] + " cm, CA=" + p.target[2] + " cm";
            if (p.band == SimilarityBand.SAS)
                return "기준 △ABC: AB=" + p.target[0] + " cm, AC=" + p.target[1] + " cm, ∠A=" + p.target[2] + "°";
            return "기준 △ABC: ∠A=" + p.target[0] + "°, ∠B=" + p.target[1] + "°";
        }

        public static string WingText(BatScene p, int i)
        {
            WingData w = p.wings[i]; string[] L = w.letters;
            string head = WingIds[i] + "번 △" + L[0] + L[1] + L[2] + " ";
            if (p.band == SimilarityBand.SSS)
                return head + L[0] + L[1] + "=" + w.v[0] + " cm, " + L[1] + L[2] + "=" + w.v[1] + " cm, " + L[2] + L[0] + "=" + w.v[2] + " cm";
            if (p.band == SimilarityBand.SAS)
                return head + L[0] + L[1] + "=" + w.v[0] + " cm, " + L[0] + L[2] + "=" + w.v[1] + " cm, ∠" + L[0] + "=" + w.v[2] + "°";
            return head + "∠" + L[0] + "=" + w.v[0] + "°, ∠" + L[1] + "=" + w.v[1] + "°";
        }

        static string BuildPrompt(BatScene p, string head)
        {
            var sb = new StringBuilder();
            sb.Append(head).Append(": 기준 삼각형과 닮은 날개를 모두 찾아 한 획으로 쓸어 손을 떼시오. ");
            sb.Append(TargetText(p));
            for (int i = 0; i < CandidateCount; i++) sb.Append(" / ").Append(WingText(p, i));
            return sb.ToString();
        }

        public static string MaskAnswer(int mask)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < CandidateCount; i++) if ((mask & (1 << i)) != 0)
            {
                if (sb.Length > 0) sb.Append(",");
                sb.Append(WingIds[i]);
            }
            return sb.ToString();
        }

        // 고정 연습(기획서 onboarding.first_problem_fixed). 앞의 두 날개(1·2번)가 정답.
        public static BatScene Practice()
        {
            var p = new BatScene {
                id = "practice-fixed", band = SimilarityBand.SSS, serial = 0,
                target = new[] { 3, 4, 5 }, correctMask = 3, concept = "SSS 닮음과 대응변의 길이의 비",
                answer = "1,2", reveal = "세 대응변의 길이의 비가 같다 → SSS 닮음"
            };
            p.wings[0] = Wing(6, 8, 10, "", 0, 0, SimilarityBand.SSS); p.wings[0].rotDeg = 0; p.wings[0].sizePct = 92;
            p.wings[1] = Wing(9, 12, 15, "", 0, 1, SimilarityBand.SSS); p.wings[1].rotDeg = 180; p.wings[1].sizePct = 100;
            p.wings[2] = Wing(5, 6, 7, "additive_lengths", 0, 2, SimilarityBand.SSS); p.wings[2].rotDeg = 90; p.wings[2].sizePct = 90;
            p.wings[3] = Wing(6, 8, 11, "sss_two_sides", 0, 3, SimilarityBand.SSS); p.wings[3].rotDeg = 270; p.wings[3].sizePct = 96;
            p.prompt = BuildPrompt(p, "연습");
            return p;
        }

        public static List<BatScene> BuildBank()
        {
            var list = new List<BatScene>(420);
            int serial = 1;
            for (int band = 1; band <= 3; band++)
                for (int cycle = 0; cycle < 10; cycle++)
                    for (int mask = 1; mask <= 14; mask++)
                    {
                        BatScene s = null;
                        // 같은 마스크로 다음 일련번호의 자료를 쓴다(마스크 분포 불변)
                        for (int k = 0; k < 8 && s == null; k++) { s = BuildScene((SimilarityBand)band, mask, serial); serial++; }
                        if (s != null) list.Add(s);
                    }
            return list;
        }

        // 한 판의 덱: 단계별 은행에서 비복원 무작위 추출. 마스크는 단계 풀(마스크별 동일 개수)에서 균등하다.
        public static List<BatScene> RunDeck(List<BatScene> bank, Random rng)
        {
            var deck = new List<BatScene>(Goal);
            for (int band = 1; band <= 3; band++)
            {
                var pool = new List<BatScene>(140);
                for (int i = 0; i < bank.Count; i++) if ((int)bank[i].band == band) pool.Add(bank[i]);
                for (int j = 0; j < BandCounts[band - 1]; j++)
                {
                    int k = rng.Next(j, pool.Count);
                    BatScene t = pool[j]; pool[j] = pool[k]; pool[k] = t;
                    deck.Add(pool[j]);
                }
            }
            return deck;
        }

        public static List<MgfProblem> BuildMgfBank(List<BatScene> scenes)
        {
            var list = new List<MgfProblem>(scenes.Count);
            for (int i = 0; i < scenes.Count; i++)
            {
                BatScene p = scenes[i];
                list.Add(new MgfProblem {
                    id = p.id, prompt = p.prompt, choices = Array.Empty<string>(), answer = p.answer,
                    answerNumeric = p.correctMask, unitConcept = p.concept
                });
            }
            return list;
        }

        // ───────────── 대응과 근거 ─────────────
        // 꼭짓점 i 의 대변의 제곱(SSS·SAS). 꼭짓점 순서 0,1,2 = A,B,C 또는 날개 글자 순서.
        static long[] OppositeSquares(SimilarityBand band, int[] v)
        {
            if (band == SimilarityBand.SSS)  // v = {AB, BC, CA}: A의 대변 BC, B의 대변 CA, C의 대변 AB
                return new[] { (long)v[1] * v[1], (long)v[2] * v[2], (long)v[0] * v[0] };
            // SAS v = {AB, AC, ∠A}: A의 대변 BC, B의 대변 AC, C의 대변 AB
            return new[] { ThirdSideSquared(v[0], v[1], v[2]), (long)v[1] * v[1], (long)v[0] * v[0] };
        }

        static int[] Angles3(int[] v) { return new[] { v[0], v[1], 180 - v[0] - v[1] }; }

        /// 닮은 날개의 대응: 기준 꼭짓점 A,B,C 가 날개의 몇 번째 꼭짓점에 대응하는지. 닮지 않았으면 null.
        public static int[] Correspondence(BatScene p, WingData w)
        {
            if (!IsSimilar(p, w)) return null;
            for (int k = 0; k < 6; k++)
            {
                int[] pm = Perms[k];
                if (p.band == SimilarityBand.AA)
                {
                    int[] ta = Angles3(p.target), wa = Angles3(w.v);
                    if (ta[0] == wa[pm[0]] && ta[1] == wa[pm[1]] && ta[2] == wa[pm[2]]) return pm;
                }
                else
                {
                    long[] t = OppositeSquares(p.band, p.target), c = OppositeSquares(p.band, w.v);
                    if (t[0] * c[pm[1]] == t[1] * c[pm[0]] && t[1] * c[pm[2]] == t[2] * c[pm[1]]) return pm;
                }
            }
            return null;
        }

        public static string SimilarNotation(BatScene p, WingData w)
        {
            int[] m = Correspondence(p, w);
            if (m == null) return "";
            return "△ABC∽△" + w.letters[m[0]] + w.letters[m[1]] + w.letters[m[2]];
        }

        static int Gcd(int a, int b) { while (b != 0) { int t = a % b; a = b; b = t; } return Math.Abs(a); }

        public static string SimpleRatio(int a, int b) { int g = Gcd(a, b); return (a / g) + ":" + (b / g); }

        static string RatioChain(int[] t, int[] c)
        {
            int[] a = (int[])t.Clone(), b = (int[])c.Clone(); Array.Sort(a); Array.Sort(b);
            var sb = new StringBuilder();
            for (int i = 0; i < a.Length; i++)
            {
                if (i > 0) sb.Append((long)a[i - 1] * b[i] == (long)a[i] * b[i - 1] ? "=" : "≠");
                sb.Append(a[i]).Append(':').Append(b[i]);
            }
            return sb.ToString();
        }

        /// 제출 뒤 날개 자리에 남는 근거 두 줄(수치는 실제 자료에서 계산).
        public static string Evidence(BatScene p, int i)
        {
            WingData w = p.wings[i];
            bool sim = IsSimilar(p, w);
            string note = sim ? SimilarNotation(p, w) : "";
            if (p.band == SimilarityBand.SSS)
            {
                string chain = RatioChain(p.target, w.v);
                if (sim) { int[] a = (int[])p.target.Clone(), b = (int[])w.v.Clone(); Array.Sort(a); Array.Sort(b); return chain + "\n" + note + " · " + SimpleRatio(a[0], b[0]); }
                return chain + "\n세 비가 같지 않다";
            }
            if (p.band == SimilarityBand.SAS)
            {
                string chain = RatioChain(new[] { p.target[0], p.target[1] }, new[] { w.v[0], w.v[1] });
                if (sim) return chain + " · ∠" + w.letters[0] + "=" + w.v[2] + "°\n" + note;
                if (w.v[2] != p.target[2]) return chain + "\n끼인각 " + w.v[2] + "°≠" + p.target[2] + "°";
                return chain + "\n두 변의 비가 다르다";
            }
            int third = 180 - w.v[0] - w.v[1];
            int[] ws = { w.v[0], w.v[1], third }; Array.Sort(ws);
            string line = "∠" + w.letters[2] + "=180−" + w.v[0] + "−" + w.v[1] + "=" + third + "°";
            return line + (sim ? "\n" + note : "\n세 각 " + ws[0] + "°·" + ws[1] + "°·" + ws[2] + "°");
        }

        public static string WrongReason(BatScene p, int selectedMask)
        {
            if (p == null) return "기준 삼각형의 조건을 다시 확인하시오";
            int extra = selectedMask & ~p.correctMask;
            for (int i = 0; i < CandidateCount; i++) if ((extra & (1 << i)) != 0)
            {
                string id = p.wings[i].misconceptionId;
                if (id == "sss_two_sides") return WingIds[i] + "번: 두 비만 같다 · 세 대응변의 비를 모두 확인하시오";
                if (id == "additive_lengths") return WingIds[i] + "번: 같은 수를 더한 길이 · 같은 배인지 확인하시오";
                if (id == "sas_ignore_included_angle") return WingIds[i] + "번: 두 변 사이의 끼인각이 다르다";
                if (id == "sas_one_ratio") return WingIds[i] + "번: 한 쌍의 변만 비가 맞는다 · 두 대응변의 비를 모두 확인하시오";
                if (id == "aa_one_angle") return WingIds[i] + "번: 각 하나만 같다 · 남은 각까지 구해 비교하시오";
            }
            int miss = p.correctMask & ~selectedMask;
            for (int i = 0; i < CandidateCount; i++) if ((miss & (1 << i)) != 0)
                return WingIds[i] + "번도 닮았다 · " + (p.band == SimilarityBand.SSS ? "작은 변끼리 비를 맞춰 보시오"
                    : p.band == SimilarityBand.SAS ? "두 변의 비와 끼인각이 같다" : "남은 각을 구하면 세 각이 같다");
            return "기준 삼각형의 조건을 다시 확인하시오";
        }

        // ───────────── 화면용 도형(실수는 그림에만) ─────────────
        /// 날개(또는 기준) 삼각형의 꼭짓점 좌표. 크기는 정규화 전(변 길이 단위 또는 임의 단위).
        public static void ShapeOf(SimilarityBand band, int[] v, float[] xs, float[] ys)
        {
            if (band == SimilarityBand.SSS)
            {
                float a = v[0], b = v[1], c = v[2];   // 0-1 = a, 1-2 = b, 2-0 = c
                float cos0 = (a * a + c * c - b * b) / (2f * a * c);
                float ang = (float)Math.Acos(Math.Max(-1f, Math.Min(1f, cos0)));
                xs[0] = 0; ys[0] = 0; xs[1] = a; ys[1] = 0; xs[2] = c * (float)Math.Cos(ang); ys[2] = c * (float)Math.Sin(ang);
            }
            else if (band == SimilarityBand.SAS)
            {
                float ang = v[2] * (float)Math.PI / 180f;   // 0-1 = v0, 0-2 = v1, ∠0 = v2
                xs[0] = 0; ys[0] = 0; xs[1] = v[0]; ys[1] = 0; xs[2] = v[1] * (float)Math.Cos(ang); ys[2] = v[1] * (float)Math.Sin(ang);
            }
            else
            {
                float al = v[0] * (float)Math.PI / 180f, be = v[1] * (float)Math.PI / 180f;
                float side02 = (float)(Math.Sin(be) / Math.Sin(Math.PI - al - be));   // 0-1 = 1
                xs[0] = 0; ys[0] = 0; xs[1] = 1; ys[1] = 0; xs[2] = side02 * (float)Math.Cos(al); ys[2] = side02 * (float)Math.Sin(al);
            }
        }

        // ───────────── 은행 검산(부팅 때 1회) ─────────────
        public static int AuditBank(List<BatScene> scenes, StringBuilder log = null)
        {
            int errors = 0;
            void Err(string m) { errors++; if (log != null && errors < 30) log.AppendLine(m); }
            if (scenes == null || scenes.Count < 336) Err("bank size " + (scenes == null ? 0 : scenes.Count));
            var ids = new HashSet<string>(); var prompts = new HashSet<string>();
            int[,] maskCount = new int[3, 15];
            var mis = new HashSet<string>();
            for (int s = 0; s < scenes.Count; s++)
            {
                BatScene p = scenes[s];
                if (!ids.Add(p.id)) Err("dup id " + p.id);
                if (!prompts.Add(p.prompt)) Err("dup prompt " + p.id);
                int computed = ComputeMask(p);
                if (computed != p.correctMask || p.correctMask < 1 || p.correctMask > 14) Err("mask " + p.id + " " + computed);
                if (p.answer != MaskAnswer(p.correctMask)) Err("answer " + p.id);
                maskCount[(int)p.band - 1, p.correctMask]++;
                if (p.prompt.IndexOf('√') >= 0) Err("root " + p.id);
                for (int i = 0; i < CandidateCount; i++)
                {
                    WingData w = p.wings[i];
                    bool sim = (computed & (1 << i)) != 0;
                    if (sim)
                    {
                        if (Correspondence(p, w) == null) Err("corr " + p.id + "#" + i);
                        if (p.band == SimilarityBand.SAS && w.v[2] != p.target[2]) Err("sas angle " + p.id);
                        if (w.misconceptionId != "") Err("mis on correct " + p.id);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(w.misconceptionId)) Err("no mis " + p.id + "#" + i);
                        mis.Add(w.misconceptionId);
                    }
                    for (int j = 0; j < i; j++) if (SameNumbers(p.band, p.wings[j], w)) Err("dup wing " + p.id);
                    if (!sim && w.misconceptionId != "sas_ignore_included_angle" && ShapeDeviation(p.band, p.target, w.v) > NearMissLimit(p.band) + 1e-9) Err("not near-miss " + p.id);
                }
            }
            for (int b = 0; b < 3; b++) for (int m = 1; m <= 14; m++) if (maskCount[b, m] != 10) Err("mask count b" + b + " m" + m + " " + maskCount[b, m]);
            if (mis.Count < 4) Err("misconceptions " + mis.Count);
            return errors;
        }
    }
}
