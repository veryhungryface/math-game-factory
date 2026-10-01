// 석회 착지 — 수학 모델·생성기·판정·발문. 판정은 정수 교차곱만(float 비교 없음).
//
// ■ 모델
//   △ABC, 석회 줄 DE는 BC와 평행(칸 줄이 가로라 강제). 칸 위치 ad = A에서 cm(자연수).
//   착지 탭이 「이 비가 맞다」는 제출. 코드는 정답 칸을 칠하거나 스냅하지 않는다.
//
// ■ 판정 Judge.Ok = ad == target. 생성기가 교차곱을 만족하는 내부 칸이 하나뿐임을 구조적으로 보장.
//
// ■ expression_traps (2022-middle-math.json) 을 생성기 규칙으로 옮김
//   · 중2 에 √ 없음 — 길이·중선은 자연수 cm. 피타고라스 수 배수로 중선이 정수인 이등변만 무게중심에 쓴다.
//   · 무게중심은 AG:GD 처럼 선분 이름으로, 「꼭짓점으로부터」 순서를 명시. 중선 AD=3k.
//   · 종결형은 「~을/를 구하시오」. 수와 단위 사이 공백(6 cm, 12 cm²). 「가정」「결론」 금지.
//   · 평행은 「DE는 BC와 평행」으로 써서 ∥ 두부(□)를 피한다.
//   · 닮음비와 넓이의 비를 한 문장에 섞지 않는다.
//
// ■ 오개념 → 코드 (misconceptionId)
//   M1_part_as_similar  AD:DB=DE:BC 로 부분과 전체를 섞음 — 유령 줄
//   M2_mid_double       중점연결 = 밑변 또는 2배
//   M3_cent_mid_or_rev  무게중심을 중점(1:1) 또는 역비(1:2)로
//   M4_any_line_area    무게중심을 지나는 모든 직선이 넓이를 이등분
using System;
using System.Collections.Generic;

namespace Mgf.SeokhoeChakji
{
    public enum Kind { Part, FindAe, FindEc, DeLen, Similar, Three, Mid, Cent, Area }

    public static class Rules
    {
        public const float RunSec = 90f;
        public const int Cards = 3;
        public const int Lands = 10;
        public const float SheetSec = 14f;
        public const int UntimedSheets = 2;
        public const float HopMin = 0.22f;
        public static int Mult(int combo) => combo >= 4 ? 4 : combo < 1 ? 1 : combo;
        public static int Ticks(Sheet s) => Math.Max(1, s.adHi - s.adLo + 1);
        public static double Chance(Sheet s) => 1.0 / Ticks(s);
    }

    public class Sheet
    {
        public int no, stage = 1;
        public Kind kind = Kind.Part;
        public int ab, ac, bc;
        public int ad, ad0 = 1, adLo = 1, adHi = 8, target;
        public int m = 1, n = 1;
        public int de, ae, ec;
        public int fAd;
        public int givenAd;
        public int median, area, areaBot;
        public int misTick, misTick2;
        public int asked;
        public string askedUnit = "cm";
        public string misconceptionId = "";
        public bool hideRatio, showWhole, isPlumb, hopAc;
        // Mid 장은 칸 숫자 = MN(평행 줄 길이).  hop 축을 BC cm 으로 두면 중점(t=0.5)의 칸이 MN=BC/2.
        public int Span => isPlumb ? median : hopAc ? ac : kind == Kind.Mid ? Math.Max(1, bc) : ab;
        public void Reset() { ad = ad0; }
        public Sheet Clone() => (Sheet)MemberwiseClone();
    }

    public static class Judge
    {
        public static int Gcd(int a, int b)
        {
            a = Math.Abs(a); b = Math.Abs(b);
            while (b != 0) { int t = a % b; a = b; b = t; }
            return a == 0 ? 1 : a;
        }
        public static void Ratio(int a, int b, out int m, out int n) { int g = Gcd(a, b); m = a / g; n = b / g; }
        public static bool Prop(int a, int b, int c, int d) => (long)a * d == (long)b * c;

        public static int De(Sheet s, int ad)
        {
            if (s.ab == 0) return 0;
            if ((long)s.bc * ad % s.ab != 0) return -1;
            return s.bc * ad / s.ab;
        }
        public static int Ae(Sheet s, int ad)
        {
            if (s.ab == 0) return 0;
            if ((long)s.ac * ad % s.ab != 0) return -1;
            return s.ac * ad / s.ab;
        }

        public static bool Satisfies(Sheet s, int ad)
        {
            if (ad < 1) return false;
            switch (s.kind)
            {
                case Kind.Part:
                    return Prop(ad, s.ab - ad, s.m, s.n);
                case Kind.FindAe:
                    // hop 축이 AC일 때 칸 숫자 = AE. 이등변(AE=AD) 복사는 OkSheet가 막는다.
                    return s.ae > 0 && ad == s.ae;
                case Kind.FindEc:
                    return Prop(ad, s.ab - ad, s.ae, s.ec);
                case Kind.DeLen:
                case Kind.Similar:
                    return s.de > 0 && (long)ad * s.bc == (long)s.ab * s.de;
                case Kind.Three:
                    return ad == s.target;
                case Kind.Mid:
                    return ad * 2 == s.bc;
                case Kind.Cent:
                case Kind.Area:
                    return s.median > 0 && ad * 3 == s.median * 2;
                default:
                    return ad == s.target;
            }
        }

        public static bool Ok(Sheet s) => s.ad == s.target;

        public static int UniqueHits(Sheet s)
        {
            int n = 0;
            int hi = Math.Max(1, s.Span - 1);
            for (int a = 1; a <= hi; a++) if (Satisfies(s, a)) n++;
            return n;
        }
    }

    public static class Words
    {
        public static string Cm(int n) => n + " cm";
        public static string Cm2(int n) => n + " cm²";

        public static string Prompt(Sheet s)
        {
            switch (s.kind)
            {
                case Kind.Part:
                    return "△ABC에서 DE는 BC와 평행하고\nAB=" + Cm(s.ab) + ", AD:DB = " + s.m + ":" + s.n + "일 때,\nAD의 길이를 구하시오.";
                case Kind.FindAe:
                {
                    int ad0 = s.givenAd > 0 ? s.givenAd : (s.ab * s.ae / Math.Max(1, s.ac));
                    return "△ABC에서 DE는 BC와 평행하고\nAD=" + Cm(ad0) + ", DB=" + Cm(s.ab - ad0) + ", AC=" + Cm(s.ac) + "일 때,\nAE의 길이를 구하시오.";
                }
                case Kind.FindEc:
                    return "△ABC에서 DE는 BC와 평행하고\nAE=" + Cm(s.ae) + ", EC=" + Cm(s.ec) + ", AB=" + Cm(s.ab) + "일 때,\nAD의 길이를 구하시오.";
                case Kind.DeLen:
                    return "△ABC에서 DE는 BC와 평행하고\nDE=" + Cm(s.de) + ", AB=" + Cm(s.ab) + ", BC=" + Cm(s.bc) + "일 때,\nAD의 길이를 구하시오.";
                case Kind.Similar:
                    return "△ABC에서 DE는 BC와 평행하고\nDE=" + Cm(s.de) + ", BC=" + Cm(s.bc) + ", AB=" + Cm(s.ab) + "일 때,\nAD의 길이를 구하시오.";
                case Kind.Three:
                    return "세 평행선 l, m, n이 두 직선과 만난다.\n한 직선 위에서 l과 m 사이가 " + Cm(s.fAd)
                           + "이고,\n다른 직선 위에서 l과 m 사이, m과 n 사이가 각각 "
                           + Cm(s.ae) + ", " + Cm(s.ec)
                           + "일 때,\n한 직선에서 l부터 n까지의 길이를 구하시오.";
                case Kind.Mid:
                    return "△ABC에서 AB, AC의 중점을 각각 M, N이라고 하자.\nAB=" + Cm(s.ab) + ", BC=" + Cm(s.bc) + "일 때,\nMN의 길이를 구하시오.";
                case Kind.Cent:
                    return "점 G가 △ABC의 무게중심이고\n중선 AD의 길이가 " + Cm(s.median) + "일 때,\nAG의 길이를 구하시오.";
                case Kind.Area:
                    return "넓이가 " + Cm2(s.area) + "인 △ABC의 무게중심을 G라고 하고\n중선 AD의 길이가 " + Cm(s.median) + "일 때,\nAG의 길이를 구하시오.";
                default:
                    return "△ABC에서 DE는 BC와 평행하다.\nAD의 길이를 구하시오.";
            }
        }

        public static string Answer(Sheet s)
        {
            if (s.askedUnit == "cm²") return Cm2(s.asked);
            return Cm(s.asked);
        }

        public static string Concept(Sheet s)
        {
            switch (s.kind)
            {
                case Kind.Part:
                case Kind.FindAe:
                case Kind.FindEc: return "삼각형에서 평행선과 선분의 길이의 비";
                case Kind.DeLen:
                case Kind.Similar: return "평행선으로 생기는 닮음(AD:AB = DE:BC)";
                case Kind.Three: return "평행선 사이의 선분의 길이의 비";
                case Kind.Mid: return "삼각형의 중점연결정리";
                case Kind.Cent: return "무게중심은 중선을 꼭짓점으로부터 2:1로 나눈다";
                case Kind.Area: return "무게중심과 삼각형의 넓이";
                default: return "평행선과 선분의 길이의 비";
            }
        }

        public static string Goal(Sheet s)
        {
            if (s == null) return "칸 탭 = hop  ·  쐐기 탭 = 착지";
            if (s.kind == Kind.Mid) return "MN 칸에 착지  ·  쐐기를 눌러 확정";
            if (s.kind == Kind.Area) return "꼭짓점으로부터 2:1 칸  ·  쐐기를 눌러 확정";
            if (s.isPlumb) return "중선 비에 맞춰 착지  ·  쐐기를 눌러 확정";
            return "칸 탭 = hop  ·  쐐기 탭 = 착지";
        }

        public static string RevealRight(Sheet s)
        {
            switch (s.kind)
            {
                case Kind.Part:
                    return "AD:DB = AE:EC = " + s.m + ":" + s.n + " → AD=" + Cm(s.target) + ", DB=" + Cm(s.ab - s.target);
                case Kind.FindAe:
                    return "AD:DB = AE:EC → AE=" + Cm(s.ae) + ", AC=" + Cm(s.ac);
                case Kind.FindEc:
                    return "AD:DB = AE:EC → AD=" + Cm(s.target) + " (" + s.ae + ":" + s.ec + " = " + s.target + ":" + (s.ab - s.target) + ")";
                case Kind.DeLen:
                case Kind.Similar:
                    Judge.Ratio(s.target, s.ab, out int m, out int n);
                    return "AD:AB = DE:BC = " + m + ":" + n + " → AD=" + Cm(s.target);
                case Kind.Three:
                    return "대응하는 선분의 비가 같다 → l부터 n까지 " + Cm(s.target);
                case Kind.Mid:
                    return "MN은 BC와 평행하고 길이는 밑변의 ½ → MN=" + Cm(s.target);
                case Kind.Cent:
                    return "무게중심은 중선을 꼭짓점으로부터 2:1로 나눈다 → AG=" + Cm(s.target);
                case Kind.Area:
                    return "무게중심은 중선을 꼭짓점으로부터 2:1 → AG=" + Cm(s.target) + ", △GBC=" + Cm2(s.areaBot);
                default:
                    return "AD=" + Cm(s.target);
            }
        }

        public static string RevealWrong(Sheet s)
        {
            switch (s.misconceptionId)
            {
                case "M1_part_as_similar":
                    return "AD:DB를 DE:BC에 쓰면 닮음 비와 섞인다. AD:AB = DE:BC.";
                case "M2_mid_double":
                    return "중점을 연결한 선분은 밑변과 평행하고 길이는 밑변의 ½이다.";
                case "M3_cent_mid_or_rev":
                    return "무게중심은 중점이 아니라, 꼭짓점으로부터 2:1이다.";
                case "M4_any_line_area":
                    return "넓이를 이등분하는 것은 중선뿐이다. △GBC는 전체의 ⅓.";
                default:
                    return RevealRight(s);
            }
        }

        public static string Foot(Sheet s)
        {
            if (s == null) return "";
            if (s.hopAc) return "칸 숫자 = AE  ·  AC = " + Cm(s.ac);
            if (s.kind == Kind.Mid) return "칸 숫자 = MN  ·  MN = 밑변의 ½";
            if (s.kind == Kind.Area) return "착지는 중선 2:1  ·  찍힌 뒤 △GBC = 전체의 ⅓";
            if (s.isPlumb) return "중선 AD = " + Cm(s.median);
            return "AB = " + Cm(s.ab) + " · BC = " + Cm(s.bc);
        }
    }

    public class SheetGen
    {
        readonly Random r;
        public SheetGen(Random r) { this.r = r; }

        T Pick<T>(IList<T> l) => l[r.Next(l.Count)];
        int PickI(params int[] a) => a[r.Next(a.Length)];

        public static Sheet Practice()
        {
            // 고정 첫 장: AD=4 cm 칸에 착지. 발문도 AD를 묻는다(포인터와 같은 양).
            var s = new Sheet
            {
                no = 0, stage = 0, kind = Kind.Part,
                ab = 6, ac = 6, bc = 8,
                m = 2, n = 1, target = 4, ad0 = 1, ad = 1,
                adLo = 1, adHi = 5,
                de = 5, ae = 4, ec = 2,
                asked = 4, askedUnit = "cm",
                misconceptionId = "M1_part_as_similar",
                misTick = 3
            };
            return s;
        }

        public List<Sheet> Deck()
        {
            var d = new List<Sheet>();
            // 첫 본판은 복사 불가 유형(FindEc: AE·EC 주고 AD). FindAe 이등변 복사는 3장째.
            d.Add(FindEc(1, 1));
            d.Add(Part(2, 1, 12, 2, 1));
            d.Add(FindAe(3, 1));
            d.Add(Part(4, 1, 9, 1, 2));
            d.Add(DeLen(5, 2));
            d.Add(Mid(6, 2));
            d.Add(Similar(7, 2));
            d.Add(Three(8, 2));
            d.Add(Cent(9, 3));
            d.Add(Area(10, 3));
            return d;
        }

        public Sheet Any()
        {
            int k = r.Next(16);
            switch (k)
            {
                case 0:
                case 1: return PartAny();
                case 2: return FindAeAny();
                case 3:
                case 4: return FindEcAny();
                case 5: return DeLenAny(Kind.DeLen);
                case 6: return DeLenAny(Kind.Similar);
                case 7: return Three(0, 2);
                case 8:
                case 9: return MidAny();
                case 10:
                case 11: return Cent(0, 3);
                case 12:
                case 13: return Area(0, 3);
                default: return PartAny();
            }
        }

        public Sheet NextFor(int no, int stage)
        {
            for (int i = 0; i < 48; i++)
            {
                Sheet s;
                if (stage <= 1) s = i % 3 == 0 ? FindAe(no, 1) : i % 3 == 1 ? FindEc(no, 1) : Part(no, 1, 0, 0, 0);
                else if (stage == 2) s = i % 4 == 0 ? Three(no, 2) : i % 4 == 1 ? Mid(no, 2) : i % 4 == 2 ? Similar(no, 2) : DeLen(no, 2);
                else s = i % 3 == 0 ? Area(no, 3) : i % 3 == 1 ? Cent(no, 3) : Mid(no, 3);
                s.no = no; s.stage = stage;
                if (stage >= 2) s.hideRatio = true;
                if (OkSheet(s)) return s;
            }
            var fb = Part(no, stage, 12, 2, 1);
            fb.hideRatio = stage >= 2;
            return fb;
        }

        static bool OkSheet(Sheet s)
        {
            if (s == null) return false;
            int span = s.Span;
            if (s.stage > 0 && span < 9) return false;
            if (s.target <= s.adLo || s.target >= span) return false;
            if (s.ad0 == s.target) return false;
            if (Judge.UniqueHits(s) != 1) return false;
            if (!Judge.Satisfies(s, s.target)) return false;
            if (s.adHi - s.adLo + 1 < 8 && s.stage > 0) return false;
            if (s.kind == Kind.FindAe && s.ab == s.ac) return false;
            if (s.kind == Kind.FindAe && (s.ae == s.givenAd || !s.hopAc)) return false;
            if (s.asked != s.target) return false;
            if (s.kind == Kind.Mid && s.target * 2 != s.bc) return false;
            if (s.kind == Kind.Area && (s.askedUnit != "cm" || s.target * 3 != s.median * 2)) return false;
            return true;
        }

        Sheet PartAny()
        {
            for (int t = 0; t < 40; t++)
            {
                int m = 1 + r.Next(5), n = 1 + r.Next(5);
                int g = Judge.Gcd(m, n); m /= g; n /= g;
                int k = 2 + r.Next(5);
                int ab = k * (m + n);
                if (ab < 9 || ab > 18) continue;
                int ad = k * m;
                if (ad < 2 || ad > ab - 2) continue;
                int bc = PickBcFor(ab, ad, new[] { ab, ab + 3, ab - 3, 12, 9, 15, 6, 8, 10, 16 });
                if (!BcOk(ab, ad, bc)) continue;
                var s = Base(0, 1, Kind.Part, ab, ab, bc, ad);
                s.m = m; s.n = n; s.ae = ad; s.ec = ab - ad;
                int de = Judge.De(s, ad); s.de = de > 0 ? de : 0;
                s.asked = ad;
                s.misTick = MidMis(ab, ad);
                s.misconceptionId = "M1_part_as_similar";
                if (OkSheet(s)) return s;
            }
            return Part(0, 1, 12, 2, 1);
        }

        Sheet FindEcAny()
        {
            for (int t = 0; t < 50; t++)
            {
                int ab = 9 + r.Next(8);
                int ad = 2 + r.Next(ab - 3);
                int ac = 6 + r.Next(13);
                if ((ac * ad) % ab != 0) continue;
                int ae = ac * ad / ab, ec = ac - ae;
                if (ae < 1 || ec < 1) continue;
                int bc = Math.Max(6, Math.Abs(ab - ac) + 2 + r.Next(5));
                if (ab + ac <= bc || ab + bc <= ac || ac + bc <= ab) continue;
                Judge.Ratio(ad, ab - ad, out int m, out int n);
                var s = Base(0, 1, Kind.FindEc, ab, ac, bc, ad);
                s.m = m; s.n = n; s.ae = ae; s.ec = ec;
                int de = Judge.De(s, ad); s.de = de > 0 ? de : 0;
                s.asked = ad;
                s.misTick = MidMis(ab, ad);
                s.misconceptionId = "M1_part_as_similar";
                if (OkSheet(s)) return s;
            }
            return FindEc(0, 1);
        }

        Sheet DeLenAny(Kind knd)
        {
            for (int t = 0; t < 50; t++)
            {
                int ab = 9 + r.Next(8);
                int ad = 2 + r.Next(ab - 3);
                int bc = 6 + r.Next(13);
                if ((ad * bc) % ab != 0) continue;
                int de = ad * bc / ab;
                if (de < 1 || de >= bc) continue;
                int ac = 6 + r.Next(13);
                if (ab + ac <= bc || ab + bc <= ac || ac + bc <= ab) continue;
                if ((ac * ad) % ab != 0) continue;
                Judge.Ratio(ad, ab, out int m, out int n);
                var s = Base(0, 2, knd, ab, ac, bc, ad);
                s.m = m; s.n = n; s.de = de;
                s.ae = Judge.Ae(s, ad);
                if (s.ae < 0) { s.ae = 0; s.ec = 0; continue; }
                s.ec = s.ac - s.ae;
                s.asked = ad;
                s.showWhole = true;
                s.hideRatio = true;
                s.misconceptionId = "M1_part_as_similar";
                s.misTick = SimilarMis(ab, ad, de, bc);
                if (OkSheet(s)) return s;
            }
            return knd == Kind.Similar ? Similar(0, 2) : DeLen(0, 2);
        }

        Sheet MidAny()
        {
            int ab = 10 + r.Next(5) * 2;
            if (ab > 18) ab = 18;
            int bc = 10 + r.Next(5) * 2;
            if (bc > 18) bc = 18;
            if (bc == ab) bc = ab >= 16 ? ab - 4 : ab + 4;
            int ac = ab;
            if (ab + ac <= bc) { bc = ab - 2; if (bc < 10) bc = 10; }
            if (bc % 2 != 0) bc += 1;
            int mn = bc / 2;
            var s = Base(0, 3, Kind.Mid, ab, ac, bc, mn);
            s.m = 1; s.n = 1; s.hideRatio = true;
            s.de = mn; s.ae = ab / 2; s.ec = ab - s.ae;
            s.asked = mn;
            s.adHi = bc - 1;
            int am = ab / 2;
            s.misTick = am != mn && am >= 1 && am < bc ? am : Math.Max(2, bc - 2);
            s.misTick2 = Math.Min(bc - 1, Math.Max(2, bc - 2));
            if (s.misTick2 == mn) s.misTick2 = mn + 1;
            s.misconceptionId = "M2_mid_double";
            return OkSheet(s) ? s : Mid(0, 3);
        }

        static readonly int[][] PartRatios = {
            new[] { 12, 2, 1 }, new[] { 12, 1, 2 }, new[] { 12, 1, 1 }, new[] { 12, 3, 1 }, new[] { 12, 1, 3 },
            new[] { 9, 2, 1 }, new[] { 9, 1, 2 }, new[] { 15, 2, 1 }, new[] { 15, 1, 2 },
            new[] { 15, 2, 3 }, new[] { 15, 3, 2 }, new[] { 18, 2, 1 }, new[] { 18, 1, 2 }, new[] { 10, 3, 2 }
        };

        Sheet Part(int no, int stage, int ab, int m, int n)
        {
            if (m == 0)
            {
                var p = Pick(PartRatios);
                ab = p[0]; m = p[1]; n = p[2];
            }
            int g = Judge.Gcd(m, n); m /= g; n /= g;
            int sum = m + n;
            if (sum < 1) { m = 1; n = 1; sum = 2; }
            if (ab % sum != 0)
            {
                int k = Math.Max(2, 12 / sum);
                ab = k * sum;
                if (ab < 9) ab = sum * Math.Max(2, (9 + sum - 1) / sum);
                if (ab > 18) ab = sum * Math.Max(1, 18 / sum);
            }
            int t = ab * m / sum;
            int[] prefer = ab <= 12 ? new[] { 12, 9, 15, 6, 8, 10, 16 } : new[] { 15, 12, 18, 10, 9, 16 };
            int bc = PickBcFor(ab, t, prefer);
            var s = Base(no, stage, Kind.Part, ab, ab, bc, t);
            s.m = m; s.n = n;
            s.ae = t; s.ec = ab - t;
            int de = Judge.De(s, t);
            s.de = de > 0 ? de : 0;
            s.asked = t;
            s.misTick = MidMis(ab, t);
            s.misconceptionId = "M1_part_as_similar";
            return s;
        }

        Sheet FindAe(int no, int stage)
        {
            // hop 축 = AC, 칸 숫자 = AE. AB≠AC 이라 AD를 그대로 적어도 통하지 않는다.
            // {AB, AC, BC, givenAD, AE}  AE = AC*AD/AB, AE≠AD, AC≥9.
            int[][] pool = {
                new[] { 12, 9, 12, 8, 6 },
                new[] { 12, 18, 15, 8, 12 },
                new[] { 15, 10, 16, 9, 6 },
                new[] { 15, 12, 18, 10, 8 },
                new[] { 10, 15, 12, 6, 9 },
                new[] { 9, 12, 10, 6, 8 },
                new[] { 18, 12, 16, 9, 6 },
                new[] { 16, 12, 16, 12, 9 },
                new[] { 14, 21, 16, 8, 12 }
            };
            int start = r.Next(pool.Length);
            for (int k = 0; k < pool.Length; k++)
            {
                var s = MakeFindAe(no, stage, pool[(start + k) % pool.Length]);
                if (s != null && OkSheet(s)) return s;
            }
            return MakeFindAe(no, stage, new[] { 12, 18, 15, 8, 12 });
        }

        static Sheet MakeFindAe(int no, int stage, int[] p)
        {
            int ab = p[0], ac = p[1], bc = p[2], givenAd = p[3], ae = p[4];
            if (ab == ac || ae == givenAd || ae < 2 || ac < 9) return null;
            if ((long)ac * givenAd % ab != 0 || ac * givenAd / ab != ae) return null;
            if (ab + ac <= bc || ab + bc <= ac || ac + bc <= ab) return null;
            int db = ab - givenAd;
            if (givenAd < 1 || db < 1 || ac - ae < 1) return null;
            Judge.Ratio(givenAd, db, out int m, out int n);
            var s = Base(no, stage, Kind.FindAe, ab, ac, bc, ae);
            s.hopAc = true;
            s.givenAd = givenAd;
            s.m = m; s.n = n;
            s.ae = ae; s.ec = ac - ae;
            s.adHi = ac - 1;
            int de = Judge.De(s, givenAd); s.de = de > 0 ? de : 0;
            s.asked = ae;
            s.misTick = givenAd != ae && givenAd >= 1 && givenAd < ac ? givenAd : MidMis(ac, ae);
            s.misconceptionId = "M1_part_as_similar";
            return s;
        }

        Sheet FindAeAny()
        {
            for (int t = 0; t < 60; t++)
            {
                int ab = 9 + r.Next(10);
                int givenAd = 2 + r.Next(Math.Max(1, ab - 3));
                int ac = 9 + r.Next(10);
                if (ac == ab) continue;
                if ((long)ac * givenAd % ab != 0) continue;
                int ae = ac * givenAd / ab;
                if (ae < 2 || ae > ac - 2 || ae == givenAd) continue;
                int bc = Math.Max(6, Math.Abs(ab - ac) + 2 + r.Next(6));
                if (ab + ac <= bc || ab + bc <= ac || ac + bc <= ab) continue;
                var s = MakeFindAe(0, 1, new[] { ab, ac, bc, givenAd, ae });
                if (s != null && OkSheet(s)) return s;
            }
            return FindAe(0, 1);
        }

        Sheet FindEc(int no, int stage)
        {
            int[][] pool = {
                new[] { 9, 6, 8, 6, 4, 2 },
                new[] { 9, 12, 10, 6, 8, 4 },
                new[] { 12, 8, 10, 9, 6, 2 },
                new[] { 12, 6, 10, 8, 4, 2 },
                new[] { 12, 9, 10, 8, 6, 3 },
                new[] { 15, 10, 12, 9, 6, 4 },
                new[] { 15, 6, 12, 10, 4, 2 },
                new[] { 12, 18, 16, 8, 12, 6 },
                new[] { 10, 15, 12, 6, 9, 6 }
            };
            var p = Pick(pool);
            int ab = p[0], ac = p[1], bc = p[2], t = p[3], ae = p[4], ec = p[5];
            Judge.Ratio(t, ab - t, out int m, out int n);
            var s = Base(no, stage, Kind.FindEc, ab, ac, bc, t);
            s.m = m; s.n = n; s.ae = ae; s.ec = ec;
            int de = Judge.De(s, t);
            s.de = de > 0 ? de : 0;
            s.asked = t;
            s.misconceptionId = "M1_part_as_similar";
            s.misTick = MidMis(ab, t);
            return s;
        }

        Sheet DeLen(int no, int stage)
        {
            int[][] pool = {
                new[] { 12, 12, 8, 6, 4, 9 },
                new[] { 10, 10, 15, 6, 9, 4 },
                new[] { 12, 12, 9, 8, 6, 4 },
                new[] { 9, 9, 12, 6, 8, 3 },
                new[] { 15, 15, 10, 9, 6, 6 },
                new[] { 12, 16, 12, 8, 8, 4 },
                new[] { 9, 12, 9, 6, 6, 3 },
                new[] { 15, 15, 12, 10, 8, 5 }
            };
            var p = Pick(pool);
            int ab = p[0], ac = p[1], bc = p[2], t = p[3], de = p[4];
            Judge.Ratio(t, ab, out int m, out int n);
            var s = Base(no, stage, Kind.DeLen, ab, ac, bc, t);
            s.m = m; s.n = n; s.de = de;
            s.ae = Judge.Ae(s, t);
            if (s.ae < 0) s.ae = 0;
            s.ec = s.ae > 0 ? s.ac - s.ae : 0;
            s.asked = t;
            s.misTick = SimilarMis(ab, t, de, bc);
            s.showWhole = true;
            s.hideRatio = true;
            s.misconceptionId = "M1_part_as_similar";
            return s;
        }

        Sheet Similar(int no, int stage)
        {
            var s = DeLen(no, stage);
            s.kind = Kind.Similar;
            Judge.Ratio(s.target, s.ab, out s.m, out s.n);
            s.asked = s.target;
            return s;
        }

        Sheet Three(int no, int stage)
        {
            int[][] pool = {
                new[] { 12, 18, 16, 2, 6, 3, 6, 9 },
                new[] { 12, 18, 15, 2, 6, 3, 6, 9 },
                new[] { 10, 15, 12, 2, 6, 3, 6, 6 },
                new[] { 15, 15, 12, 3, 9, 3, 6, 6 },
                new[] { 12, 16, 14, 3, 9, 4, 8, 4 },
                new[] { 9, 18, 15, 2, 5, 4, 6, 8 },
                new[] { 14, 21, 16, 4, 10, 6, 9, 6 }
            };
            int start = r.Next(pool.Length);
            for (int k = 0; k < pool.Length; k++)
            {
                var s = MakeThree(no, stage, pool[(start + k) % pool.Length]);
                if (s != null) return s;
            }
            return MakeThree(no, stage, new[] { 12, 18, 16, 2, 6, 3, 6, 9 });
        }

        static Sheet MakeThree(int no, int stage, int[] p)
        {
            int ab = p[0], ac = p[1], bc = p[2], f = p[3], t = p[4], ae = p[5], ed = p[6], x = p[7];
            int fd = t - f, db = ab - t;
            if (f < 1 || fd < 1 || db < 1 || ae < 1 || ed < 1 || x < 1) return null;
            if (ae + ed + x != ac) return null;
            if (!Judge.Prop(f, fd, ae, ed)) return null;
            if (!Judge.Prop(fd, db, ed, x)) return null;
            if ((long)ac * fd % ab != 0 || ac * fd / ab != ed) return null;
            if ((long)ac * db % ab != 0 || ac * db / ab != x) return null;
            var s = Base(no, stage, Kind.Three, ab, ac, bc, t);
            s.fAd = f;
            s.ae = ae;
            s.ec = ed; s.asked = t; s.askedUnit = "cm";
            s.de = Judge.De(s, t); if (s.de < 0) s.de = 0;
            s.hideRatio = true;
            s.misconceptionId = "M1_part_as_similar";
            s.misTick = f + fd / 2; if (s.misTick == t) s.misTick = t - 1;
            if (s.misTick < 1) s.misTick = 1;
            if (!Judge.Satisfies(s, t) || Judge.UniqueHits(s) != 1) return null;
            return s;
        }

        Sheet Mid(int no, int stage)
        {
            int ab = PickI(10, 12, 14, 16, 18);
            int bc = PickI(10, 12, 14, 16, 18);
            if (bc % 2 != 0) bc += 1;
            if (bc == ab) bc = ab >= 16 ? ab - 4 : ab + 4;
            if (bc % 2 != 0) bc += 1;
            if (bc < 10) bc = 10;
            int mn = bc / 2;
            var s = Base(no, stage, Kind.Mid, ab, ab, bc, mn);
            s.m = 1; s.n = 1; s.hideRatio = true;
            s.de = mn; s.ae = ab / 2; s.ec = ab - s.ae;
            s.asked = mn;
            s.adHi = bc - 1;
            int am = ab / 2;
            s.misTick = am != mn && am >= 1 && am < bc ? am : Math.Max(2, bc - 2);
            s.misconceptionId = "M2_mid_double";
            return s;
        }

        Sheet Cent(int no, int stage)
        {
            // 이등변 + 피타고라스: AB² = median² + (BC/2)². 중선 = 3k.
            // 검증된 두 개만: {13,10,12,8,60} h=12 넓이 60, {17,16,15,10,120} h=15 넓이 120.
            int[][] pool = {
                new[] { 13, 10, 12, 8, 60 },
                new[] { 17, 16, 15, 10, 120 }
            };
            var p = Pick(pool);
            int ab = p[0], bc = p[1], med = p[2], t = p[3], area = p[4];
            var s = Base(no, stage, Kind.Cent, ab, ab, bc, t);
            s.isPlumb = true; s.median = med; s.area = area; s.areaBot = area / 3;
            s.hideRatio = true;
            s.asked = t;
            s.adHi = med - 1;
            s.misTick = med / 2;
            s.misTick2 = med / 3;
            if (s.misTick2 == t) s.misTick2 = 1;
            s.misconceptionId = "M3_cent_mid_or_rev";
            return s;
        }

        Sheet Area(int no, int stage)
        {
            var s = Cent(no, stage);
            s.kind = Kind.Area;
            s.asked = s.target;
            s.askedUnit = "cm";
            s.misconceptionId = "M4_any_line_area";
            return s;
        }

        static Sheet Base(int no, int stage, Kind k, int ab, int ac, int bc, int target)
        {
            int span = ab;
            var s = new Sheet
            {
                no = no, stage = stage, kind = k,
                ab = ab, ac = ac, bc = bc,
                target = target, ad0 = 1, ad = 1,
                adLo = 1, adHi = Math.Max(8, span - 1),
                askedUnit = "cm"
            };
            s.adHi = ab - 1;
            return s;
        }

        static int SimilarMis(int ab, int ad, int de, int bc)
        {
            // M1 AD:DB=DE:BC → AD = AB·DE / (BC+DE). 정수일 때만. 겹치면 DE 복사, 그다음 중점.
            if (bc + de > 0 && (long)ab * de % (bc + de) == 0)
            {
                int m = ab * de / (bc + de);
                if (m >= 1 && m < ab && m != ad) return m;
            }
            if (de >= 1 && de < ab && de != ad) return de;
            return MidMis(ab, ad);
        }

        static int MidMis(int ab, int t)
        {
            int mid = ab / 2;
            if (mid != t && mid >= 1) return mid;
            int rev = ab - t;
            if (rev != t && rev >= 1 && rev < ab) return rev;
            return t > 2 ? t - 1 : t + 1;
        }

        static bool BcOk(int ab, int ad, int bc)
        {
            if (bc < 4 || ab < 3) return false;
            if (ab + ab <= bc) return false;
            if (ad < 1) return true;
            return (long)bc * ad % ab == 0;
        }

        int PickBcFor(int ab, int ad, int[] prefer)
        {
            int n = 0;
            for (int i = 0; i < prefer.Length; i++) if (BcOk(ab, ad, prefer[i])) n++;
            if (n > 0)
            {
                int k = r.Next(n);
                for (int i = 0; i < prefer.Length; i++)
                    if (BcOk(ab, ad, prefer[i]) && k-- == 0) return prefer[i];
            }
            for (int bc = 6; bc <= 20; bc++) if (BcOk(ab, ad, bc)) return bc;
            return ab;
        }
    }
}
