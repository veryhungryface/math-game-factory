// 깃 나눠 — 수학 모델·생성기·판정·발문. 판정은 정수 교차곱만(float 비교 없음).
//
// ■ 모델
//   △ABC, 밀대가 떨어뜨린 줄 DE는 BC와 평행(기구가 강제). 칸 위치 ad = 꼭짓점 A에서 cm(자연수).
//   탭 한 번이 「이 비가 맞다」는 제출. 코드는 밀대를 정답 칸에 붙이지 않는다.
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

namespace Mgf.GitNanwo
{
    public enum Kind { Part, FindAe, FindEc, DeLen, Similar, Three, Mid, Cent, Area }

    public static class Rules
    {
        public const float RunSec = 90f;
        public const int Flags = 3;
        public const int Cuts = 10;
        public const float SheetSec = 8f;
        public const int UntimedSheets = 2;
        public const float TickSecSlow = 0.70f;
        public const float TickSecFast = 0.55f;
        public static int Mult(int combo) => combo >= 4 ? 4 : combo < 1 ? 1 : combo;
        public static int Ticks(Sheet s) => Math.Max(1, s.adHi - s.adLo + 1);
        public static double Chance(Sheet s) => 1.0 / Ticks(s);
        public static float TickSec(int stage) => stage >= 2 ? TickSecFast : TickSecSlow;
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
        public bool hideRatio, showWhole, isPlumb, hopAc, ghostOn;
        public int Span => isPlumb ? median : hopAc ? ac : (kind == Kind.Similar || kind == Kind.Mid) ? Math.Max(1, bc) : ab;
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
                    return s.ae > 0 && ad == s.ae;
                case Kind.FindEc:
                    return Prop(ad, s.ab - ad, s.ae, s.ec);
                case Kind.DeLen:
                    return s.de > 0 && (long)ad * s.bc == (long)s.ab * s.de;
                case Kind.Similar:
                case Kind.Three:
                    return ad == s.target;
                case Kind.Mid:
                    return s.bc > 0 && ad * 2 == s.bc;
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
                    return "△ABC에서 DE는 BC와 평행하고 AB=" + Cm(s.ab) + ", AD:DB = " + s.m + ":" + s.n + "일 때, AD의 길이를 구하시오.";
                case Kind.FindAe:
                {
                    int ad0 = s.givenAd > 0 ? s.givenAd : (s.ab * s.ae / Math.Max(1, s.ac));
                    return "△ABC에서 DE는 BC와 평행하고 AD=" + Cm(ad0) + ", DB=" + Cm(s.ab - ad0) + ", AC=" + Cm(s.ac) + "일 때, AE의 길이를 구하시오.";
                }
                case Kind.FindEc:
                    return "△ABC에서 DE는 BC와 평행하고 AE=" + Cm(s.ae) + ", EC=" + Cm(s.ec) + ", AB=" + Cm(s.ab) + "일 때, AD의 길이를 구하시오.";
                case Kind.DeLen:
                    return "△ABC에서 DE는 BC와 평행하고 DE=" + Cm(s.de) + ", AB=" + Cm(s.ab) + ", BC=" + Cm(s.bc) + "일 때, AD의 길이를 구하시오.";
                case Kind.Similar:
                    return "△ABC에서 DE는 BC와 평행하고 AD:AB = " + s.m + ":" + s.n + ", AB=" + Cm(s.ab) + ", BC=" + Cm(s.bc) + "일 때, DE의 길이를 구하시오.";
                case Kind.Three:
                    return "세 평행선 l, m, n이 두 직선과 만난다. 한 직선 위에서 l과 m 사이가 " + Cm(s.fAd)
                           + ", m과 n 사이가 " + Cm(s.givenAd)
                           + "이고, 다른 직선 위에서 l과 m 사이가 " + Cm(s.ae)
                           + "일 때, 그 직선에서 m과 n 사이의 길이를 구하시오.";
                case Kind.Mid:
                    return "△ABC에서 AB, AC의 중점을 각각 M, N이라고 하자. AB=" + Cm(s.ab) + ", BC=" + Cm(s.bc) + "일 때, MN의 길이를 구하시오.";
                case Kind.Cent:
                    return "점 G가 △ABC의 무게중심이고 중선 AD의 길이가 " + Cm(s.median) + "일 때, AG의 길이를 구하시오.";
                case Kind.Area:
                    return "넓이가 " + Cm2(s.area) + "인 △ABC의 무게중심을 G라고 하고 중선 AD의 길이가 " + Cm(s.median) + "일 때, AG의 길이를 구하시오.";
                default:
                    return "△ABC에서 DE는 BC와 평행하다. AD의 길이를 구하시오.";
            }
        }

        public static string Answer(Sheet s)
        {
            if (s.kind == Kind.Similar) return Cm(s.de);
            if (s.kind == Kind.Mid) return Cm(s.asked);
            if (s.kind == Kind.Area) return Cm(s.asked);
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
            if (s == null) return "밀대를 눌러 길이의 비를 맞춰라";
            if (s.kind == Kind.Mid) return "MN의 길이에 맞춰 밀대를 눌러라";
            if (s.kind == Kind.Cent || s.kind == Kind.Area) return "AG의 길이에 맞춰 밀대를 눌러라";
            if (s.isPlumb) return "중선 비에 맞춰 밀대를 눌러라";
            return "밀대를 눌러 길이의 비를 맞춰라";
        }

        public static string RevealRight(Sheet s)
        {
            switch (s.kind)
            {
                case Kind.Part:
                    return "AD:DB = AE:EC = " + s.m + ":" + s.n + "  →  AD=" + Cm(s.target) + ", DB=" + Cm(s.ab - s.target);
                case Kind.FindAe:
                    return "AD:DB = AE:EC  →  AE=" + Cm(s.ae) + ", AC=" + Cm(s.ac);
                case Kind.FindEc:
                    return "AD:DB = AE:EC  →  AD=" + Cm(s.target);
                case Kind.DeLen:
                    Judge.Ratio(s.target, s.ab, out int m, out int n);
                    return "AD:AB = DE:BC = " + m + ":" + n + "  →  AD=" + Cm(s.target);
                case Kind.Similar:
                {
                    // target 은 DE cm. AD:AB 를 DE:AB 로 바꾸면 틀린 닮음비를 가르친다.
                    int mShow = s.m, nShow = s.n;
                    if (mShow < 1 || nShow < 1 || (long)mShow * s.bc != (long)nShow * s.de)
                        Judge.Ratio(s.de, s.bc, out mShow, out nShow);
                    return "AD:AB = DE:BC = " + mShow + ":" + nShow + "  →  DE=" + Cm(s.de);
                }
                case Kind.Three:
                    return "대응하는 선분의 비가 같다  →  m과 n 사이 " + Cm(s.target);
                case Kind.Mid:
                    return "MN은 BC와 평행하고 길이는 밑변의 1/2  →  MN=" + Cm(s.asked);
                case Kind.Cent:
                    return "무게중심은 중선을 꼭짓점으로부터 2:1로 나눈다  →  AG=" + Cm(s.target);
                case Kind.Area:
                    return "중선은 넓이를 이등분하고 △GBC는 전체의 1/3  →  " + Cm2(s.areaBot);
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
                    return "중점을 연결한 선분은 밑변과 평행하고 길이는 밑변의 1/2이다.";
                case "M3_cent_mid_or_rev":
                    return "무게중심은 중점이 아니라, 꼭짓점으로부터 2:1이다.";
                case "M4_any_line_area":
                    return "넓이를 이등분하는 것은 중선뿐이다. △GBC는 전체의 1/3.";
                default:
                    return RevealRight(s);
            }
        }

        public static string Foot(Sheet s)
        {
            if (s == null) return "";
            if (s.hopAc) return "칸 숫자 = AE   ·   AC = " + Cm(s.ac);
            if (s.kind == Kind.Mid) return "구한 MN cm 칸이 중점 · MN = 밑변의 1/2";
            if (s.kind == Kind.Area) return "중선 AD = " + Cm(s.median) + "  ·  △ABC = " + Cm2(s.area);
            if (s.isPlumb) return "중선 AD = " + Cm(s.median);
            return "AB = " + Cm(s.ab) + "  ·  BC = " + Cm(s.bc);
        }

        public static int CounterGoal(Sheet s)
        {
            if (s == null) return 0;
            if (s.kind == Kind.Similar) return s.de;
            if (s.kind == Kind.Mid) return s.asked;
            if (s.kind == Kind.Area) return s.areaBot;
            return s.asked;
        }

        public static string CounterUnit(Sheet s)
        {
            if (s != null && s.kind == Kind.Area) return "cm²";
            return "cm";
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
            // 고정 첫 장: AB 높이 칸만 왕복, 4 cm 칸에 드롭. hopAc 끄면 밀대가 BC와 평행을 유지한다.
            var s = new Sheet
            {
                no = 0, stage = 0, kind = Kind.FindAe,
                ab = 6, ac = 6, bc = 9,
                m = 2, n = 1, target = 4, ad0 = 1, ad = 1,
                adLo = 1, adHi = 5,
                givenAd = 4, de = 6, ae = 4, ec = 2,
                asked = 4, askedUnit = "cm",
                hopAc = false,
                misconceptionId = "M1_part_as_similar",
                misTick = 3
            };
            return s;
        }

        public List<Sheet> Deck()
        {
            var d = new List<Sheet>();
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
            foreach (var s in d) StampStart(s);
            return d;
        }

        void StampStart(Sheet s)
        {
            if (s == null) return;
            int ticks = Math.Max(1, s.adHi - s.adLo + 1);
            s.ad0 = s.adLo + r.Next(ticks);
            if (s.ad0 == s.target)
                s.ad0 = s.target > s.adLo ? s.target - 1 : Math.Min(s.adHi, s.target + 1);
            s.ad = s.ad0;
        }

        public Sheet Any()
        {
            Sheet s;
            int k = r.Next(16);
            switch (k)
            {
                case 0:
                case 1: s = PartAny(); break;
                case 2: s = FindAeAny(); break;
                case 3: s = FindEcAny(); break;
                case 4: s = DeLenAny(Kind.DeLen); break;
                case 5: s = DeLenAny(Kind.Similar); break;
                case 6:
                case 7:
                case 14: s = Three(0, 2); break;
                case 8:
                case 9: s = MidAny(); break;
                case 10:
                case 11: s = Cent(0, 3); break;
                case 12:
                case 13: s = Area(0, 3); break;
                default: s = Similar(0, 2); break;
            }
            StampStart(s);
            return s;
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
                if (OkSheet(s)) { StampStart(s); return s; }
            }
            var fb = Part(no, stage, 12, 2, 1);
            fb.hideRatio = stage >= 2;
            StampStart(fb);
            return fb;
        }

        public static bool OkSheet(Sheet s)
        {
            if (s == null) return false;
            int span = s.Span;
            if (s.stage > 0 && span < 9) return false;
            if (s.target <= s.adLo || s.target >= span) return false;
            if (s.ad0 == s.target) return false;
            if (s.target == 1) return false;
            if (Judge.UniqueHits(s) != 1) return false;
            if (!Judge.Satisfies(s, s.target)) return false;
            if (s.adHi - s.adLo + 1 < 8 && s.stage > 0) return false;
            if (s.kind == Kind.FindAe && s.ab == s.ac && s.stage > 0) return false;
            if (s.kind == Kind.FindAe && (s.ae == s.givenAd || !s.hopAc) && s.stage > 0) return false;
            if (s.kind == Kind.FindEc && s.stage > 0 && (s.ab == s.ac || s.ae == s.target)) return false;
            if (s.kind == Kind.Similar)
            {
                if (s.de < 1 || s.asked != s.de || s.target != s.de) return false;
                if (s.ab == s.de) return false;
            }
            else if (s.kind == Kind.Mid)
            {
                if (s.asked * 2 != s.bc || s.target != s.asked) return false;
            }
            else if (s.kind == Kind.Three)
            {
                if (s.asked != s.target || s.givenAd < 1 || s.fAd < 1 || s.ae < 1) return false;
                if (!Judge.Prop(s.fAd, s.givenAd, s.ae, s.asked)) return false;
                if (s.target == s.fAd || s.target == s.givenAd || s.target == s.ae) return false;
            }
            else if (s.kind == Kind.Area)
            {
                if (s.asked != s.target || s.askedUnit != "cm") return false;
                if (s.target * 3 != s.median * 2) return false;
                if (s.median < 1 || s.area * 2 != s.bc * s.median) return false;
                if (s.areaBot * 3 != s.area) return false;
            }
            else if (s.asked != s.target) return false;
            if (s.misTick == s.target) return false;
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
                if (ac == ab) continue;
                if ((ac * ad) % ab != 0) continue;
                int ae = ac * ad / ab, ec = ac - ae;
                if (ae < 1 || ec < 1 || ae == ad) continue;
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
                s.givenAd = ad;
                if (knd == Kind.Similar)
                {
                    s.asked = de;
                    s.target = de;
                    s.adHi = Math.Max(8, bc - 1);
                    if (de < 2 || de >= s.Span) continue;
                    if (bc < 9) continue;
                }
                else s.asked = ad;
                s.showWhole = true;
                s.hideRatio = true;
                s.ghostOn = true;
                s.misconceptionId = "M1_part_as_similar";
                s.misTick = knd == Kind.Similar
                    ? (ad != de && ad >= 1 && ad < s.Span ? ad : MidMis(s.Span, de))
                    : SimilarMis(ab, ad, de, bc);
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
            int mid = ab / 2;
            int mn = bc / 2;
            var s = Base(0, 2, Kind.Mid, ab, ac, bc, mn);
            s.m = 1; s.n = 1; s.hideRatio = true;
            s.de = mn; s.ae = mid; s.ec = ab - s.ae;
            s.asked = mn;
            s.adHi = Math.Max(8, bc - 1);
            s.misTick = mid != mn && mid >= 1 && mid < bc ? mid : Math.Max(2, bc - 2);
            s.misTick2 = Math.Min(bc - 1, Math.Max(2, ab));
            if (s.misTick2 == mn) s.misTick2 = mn + 1;
            s.misconceptionId = "M2_mid_double";
            return OkSheet(s) ? s : Mid(0, 2);
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
            if (stage > 0 && (ab == ac || ae == givenAd || ae < 2 || ac < 9)) return null;
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
                new[] { 10, 15, 12, 6, 9, 6 },
                new[] { 14, 21, 16, 8, 12, 9 },
                new[] { 16, 12, 18, 8, 6, 6 },
                new[] { 18, 12, 16, 12, 8, 4 }
            };
            int start = r.Next(pool.Length);
            for (int k = 0; k < pool.Length; k++)
            {
                var p = pool[(start + k) % pool.Length];
                int ab = p[0], ac = p[1], bc = p[2], t = p[3], ae = p[4], ec = p[5];
                if (ab == ac || ae == t) continue;
                if (ab + ac <= bc || ab + bc <= ac || ac + bc <= ab) continue;
                if ((long)ac * t % ab != 0 || ac * t / ab != ae) continue;
                if (ac - ae != ec) continue;
                Judge.Ratio(t, ab - t, out int m, out int n);
                var s = Base(no, stage, Kind.FindEc, ab, ac, bc, t);
                s.m = m; s.n = n; s.ae = ae; s.ec = ec;
                int de = Judge.De(s, t);
                s.de = de > 0 ? de : 0;
                s.asked = t;
                s.misconceptionId = "M1_part_as_similar";
                s.misTick = MidMis(ab, t);
                if (stage == 0 || OkSheet(s)) return s;
            }
            var fb = Base(no, stage, Kind.FindEc, 12, 8, 10, 9);
            fb.m = 3; fb.n = 1; fb.ae = 6; fb.ec = 2; fb.de = 0; fb.asked = 9;
            fb.misconceptionId = "M1_part_as_similar";
            fb.misTick = MidMis(12, 9);
            return fb;
        }

        Sheet DeLen(int no, int stage)
        {
            int[][] pool = {
                new[] { 12, 12, 8, 6, 4 },
                new[] { 10, 10, 15, 6, 9 },
                new[] { 12, 12, 9, 8, 6 },
                new[] { 9, 9, 12, 6, 8 },
                new[] { 15, 15, 10, 9, 6 },
                new[] { 12, 16, 12, 8, 8 },
                new[] { 9, 12, 9, 6, 6 },
                new[] { 15, 15, 12, 10, 8 }
            };
            var p = Pick(pool);
            int ab = p[0], ac = p[1], bc = p[2], t = p[3], de = p[4];
            Judge.Ratio(t, ab, out int m, out int n);
            var s = Base(no, stage, Kind.DeLen, ab, ac, bc, t);
            s.m = m; s.n = n; s.de = de; s.givenAd = t;
            s.ae = Judge.Ae(s, t);
            if (s.ae < 0) s.ae = 0;
            s.ec = s.ae > 0 ? s.ac - s.ae : 0;
            s.asked = t;
            s.misTick = SimilarMis(ab, t, de, bc);
            s.showWhole = true;
            s.hideRatio = true;
            s.ghostOn = true;
            s.misconceptionId = "M1_part_as_similar";
            return s;
        }

        Sheet Similar(int no, int stage)
        {
            for (int i = 0; i < 12; i++)
            {
                var s = DeLen(no, stage);
                int adKept = s.target;
                s.kind = Kind.Similar;
                s.givenAd = adKept;
                Judge.Ratio(adKept, s.ab, out s.m, out s.n);
                s.asked = s.de;
                s.target = s.de;
                s.adHi = Math.Max(8, s.bc - 1);
                s.ghostOn = true;
                s.misTick = adKept != s.de && adKept >= 1 && adKept < s.Span ? adKept : MidMis(s.Span, s.de);
                if (OkSheet(s)) return s;
            }
            // 알려진 정수 교차곱: AD:AB=2:3, DE=6, 드롭 칸=DE
            var fb = Base(no, stage, Kind.Similar, 12, 12, 9, 6);
            fb.givenAd = 8; fb.de = 6; fb.ae = 8; fb.ec = 4;
            fb.m = 2; fb.n = 3; fb.asked = 6; fb.target = 6;
            fb.adHi = 8; fb.hideRatio = true; fb.ghostOn = true;
            fb.misconceptionId = "M1_part_as_similar";
            fb.misTick = 8;
            return fb;
        }

        Sheet Three(int no, int stage)
        {
            // {ab, ac, bc, f=l-m 첫째, fd=m-n 첫째, ae=l-m 둘째, ed=m-n 둘째=답}
            int[][] pool = {
                new[] { 12, 18, 16, 2, 3, 4, 6 },
                new[] { 12, 18, 15, 2, 4, 3, 6 },
                new[] { 15, 18, 16, 4, 6, 6, 9 },
                new[] { 12, 16, 14, 3, 6, 4, 8 },
                new[] { 14, 21, 16, 4, 6, 6, 9 },
                new[] { 10, 15, 12, 2, 4, 3, 6 },
                new[] { 15, 20, 16, 3, 6, 5, 10 },
                new[] { 16, 20, 18, 4, 8, 5, 10 },
                new[] { 18, 24, 20, 3, 6, 4, 8 },
                new[] { 18, 24, 20, 4, 8, 6, 12 }
            };
            int start = r.Next(pool.Length);
            for (int k = 0; k < pool.Length; k++)
            {
                var s = MakeThree(no, stage, pool[(start + k) % pool.Length]);
                if (s != null) return s;
            }
            return MakeThree(no, stage, new[] { 12, 18, 16, 2, 3, 4, 6 });
        }

        static Sheet MakeThree(int no, int stage, int[] p)
        {
            int ab = p[0], ac = p[1], bc = p[2], f = p[3], fd = p[4], ae = p[5], ed = p[6];
            if (f < 1 || fd < 1 || ae < 1 || ed < 1) return null;
            if (!Judge.Prop(f, fd, ae, ed)) return null;
            if (ed == f || ed == fd || ed == ae) return null;
            if (ed <= 1 || ed >= ab - 1) return null;
            if (ab + ac <= bc || ab + bc <= ac || ac + bc <= ab) return null;
            var s = Base(no, stage, Kind.Three, ab, ac, bc, ed);
            s.fAd = f;
            s.givenAd = fd;
            s.ae = ae;
            s.ec = ed;
            s.asked = ed;
            s.askedUnit = "cm";
            s.de = ed;
            s.hideRatio = true;
            s.misconceptionId = "M1_part_as_similar";
            s.misTick = ae != ed && ae >= 1 && ae < ab ? ae : MidMis(ab, ed);
            if (s.misTick == ed) s.misTick = ed > 2 ? ed - 1 : ed + 1;
            if (!Judge.Satisfies(s, ed) || Judge.UniqueHits(s) != 1) return null;
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
            int mid = ab / 2;
            int mn = bc / 2;
            var s = Base(no, stage, Kind.Mid, ab, ab, bc, mn);
            s.m = 1; s.n = 1; s.hideRatio = true;
            s.de = mn; s.ae = mid; s.ec = ab - s.ae;
            s.asked = mn;
            s.adHi = Math.Max(8, bc - 1);
            s.misTick = mid != mn && mid >= 1 && mid < bc ? mid : Math.Max(2, bc - 2);
            s.misconceptionId = "M2_mid_double";
            return s;
        }

        Sheet Cent(int no, int stage)
        {
            // 이등변 + 피타고라스: AB² = median² + (BC/2)². 중선 = 3k.
            int[][] pool = {
                new[] { 13, 10, 12, 8, 60 },
                new[] { 15, 18, 12, 8, 108 },
                new[] { 17, 16, 15, 10, 120 },
                new[] { 15, 24, 9, 6, 108 },
                new[] { 20, 32, 12, 8, 192 }
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
            // 발문 넓이 = 그린 이등변(BC×중선/2). 드롭 칸은 AG(cm) — 물은 값과 칸을 같게.
            s.areaBot = s.area / 3;
            s.asked = s.target;
            s.askedUnit = "cm";
            s.misconceptionId = "M4_any_line_area";
            s.ghostOn = true;
            return s;
        }

        static Sheet Base(int no, int stage, Kind k, int ab, int ac, int bc, int target)
        {
            var s = new Sheet
            {
                no = no, stage = stage, kind = k,
                ab = ab, ac = ac, bc = bc,
                target = target, ad0 = 1, ad = 1,
                adLo = 1, adHi = Math.Max(8, ab - 1),
                askedUnit = "cm"
            };
            int hi = k == Kind.FindAe ? ac : (k == Kind.Similar || k == Kind.Mid) ? bc : k == Kind.Cent || k == Kind.Area ? Math.Max(ab, 9) : ab;
            s.adHi = hi - 1;
            if (s.adHi < 8 && k != Kind.Similar) s.adHi = 8;
            return s;
        }

        static int SimilarMis(int ab, int ad, int de, int bc)
        {
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
