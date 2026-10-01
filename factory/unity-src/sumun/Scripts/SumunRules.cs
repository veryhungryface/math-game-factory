// 수문 — 수학 모델·생성기·판정·발문. 판정은 정수 교차곱만(float 비교 없음).
//
// ■ 모델
//   △ABC, 수문 DE∥BC(기구가 강제). 칸 위치 ad = AD(cm, 자연수). DE = BC·AD/AB (나눌 때만 표시).
//   무게중심 장: 중선 AD 위 추 위치 ag. 코드는 정답 칸으로 끌어 주지 않는다.
//
// ■ 판정 Judge.Ok = ad == target. 생성기가 교차곱을 만족하는 칸이 하나뿐임을 구조적으로 보장.
//
// ■ expression_traps (2022-middle-math.json) 을 생성기 규칙으로 옮김
//   · 중2 에 √ 없음 — 길이·중선은 자연수 cm. 피타고라스 수(3·4·5 배수)로 중선이 정수인 이등변만 무게중심에 쓴다.
//   · 무게중심은 AG:GD 처럼 선분 이름으로, 「꼭짓점으로부터」 순서를 명시. 중선 AD=3k.
//   · 종결형은 「~을/를 구하시오」. 수와 단위 사이 공백(6 cm, 12 cm²). 「가정」「결론」 금지.
//   · 닮음 기호 순서는 대응 꼭짓점. 평행은 「DE는 BC와 평행」으로 써서 ∥ 두부(□)를 피한다.
//
// ■ 오개념 → 코드 (misconceptionId)
//   M1_part_as_similar  AD:DB=DE:BC 로 닮음 비를 세움 — 오답 칸 DE=AD 또는 부분:부분을 DE에 적용
//   M2_mid_double       중점연결 = 밑변 또는 2배 — 밑변 겹침 잠금은 핀 1개
//   M3_cent_mid_or_rev  무게중심을 중점(1:1) 또는 역비(1:2)로
//   M4_any_line_area    무게중심을 지나는 모든 직선이 넓이를 이등분 — 중선 밖 드래그는 스냅백
using System;
using System.Collections.Generic;
using System.Text;

namespace Mgf.Sumun
{
    public enum Kind { Part, FindEc, DeLen, Similar, Three, Mid, Cent, Area, Fix }

    public static class Rules
    {
        public const float RunSec = 90f;
        public const int Pins = 3;
        public const int Locks = 10;
        public const int TickPerSec = 8;
        public const float SheetSec = 12f;
        public static int Mult(int combo) => combo >= 4 ? 4 : combo < 1 ? 1 : combo;
        public static int Ticks(Sheet s) => s.adHi - s.adLo + 1;
        public static double Chance(Sheet s) => Ticks(s) <= 0 ? 0 : 1.0 / Ticks(s);
    }

    public class Sheet
    {
        public int no, stage = 1;
        public Kind kind = Kind.Part;
        public int ab, ac, bc;
        public int ad, ad0 = 1, adLo = 1, adHi = 8, target;
        public int m = 1, n = 1;
        public int de, ae, ec;
        public int fAd;                 // 세 평행선: 고정 수문
        public int median, area, areaBot;
        public int misTick;
        public int asked;
        public string askedUnit = "cm";
        public string misconceptionId = "";
        public bool hideRatio, showWhole, isPlumb, refuseBase;
        public int Span => isPlumb ? median : ab;
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

        /// <summary>칸 ad 가 이 장의 수학 조건을 만족하는가(교차곱). 생성기 전수 검사용.</summary>
        public static bool Satisfies(Sheet s, int ad)
        {
            if (ad < 1) return false;
            switch (s.kind)
            {
                case Kind.Part:
                    return Prop(ad, s.ab - ad, s.m, s.n);
                case Kind.FindEc:
                    return Prop(ad, s.ab - ad, s.ae, s.ec);
                case Kind.DeLen:
                case Kind.Similar:
                    return s.de > 0 && (long)ad * s.bc == (long)s.ab * s.de;
                case Kind.Three:
                {
                    // 세 평행선 분점: (l–m):(m–n) = ae:asked. ad==target 항등식은 틀린 x 를 통과시킨다.
                    int fd = ad - s.fAd, db = s.ab - ad;
                    if (fd < 1 || db < 1 || s.ae < 1 || s.asked < 1) return false;
                    return Prop(fd, db, s.ae, s.asked);
                }
                case Kind.Mid:
                    return ad * 2 == s.ab;
                case Kind.Cent:
                case Kind.Area:
                    return s.median > 0 && ad * 3 == s.median * 2;
                case Kind.Fix:
                    return s.showWhole
                        ? (s.de > 0 && (long)ad * s.bc == (long)s.ab * s.de)
                        : Prop(ad, s.ab - ad, s.m, s.n);
                default:
                    return ad == s.target;
            }
        }

        public static bool Ok(Sheet s) => s.ad == s.target;

        public static int UniqueHits(Sheet s)
        {
            int n = 0;
            int hi = s.isPlumb ? Math.Max(1, s.median - 1) : Math.Max(1, s.ab - 1);
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
                case Kind.FindEc:
                    return "△ABC에서 DE는 BC와 평행하고\nAD=" + Cm(s.target) + ", DB=" + Cm(s.ab - s.target) + ", AE=" + Cm(s.ae) + "일 때,\nEC의 길이를 구하시오.";
                case Kind.DeLen:
                    return "△ABC에서 DE는 BC와 평행하고\nAD=" + Cm(s.target) + ", AB=" + Cm(s.ab) + ", BC=" + Cm(s.bc) + "일 때,\nDE의 길이를 구하시오.";
                case Kind.Similar:
                    return "△ABC에서 DE는 BC와 평행하고\nAB=" + Cm(s.ab) + ", AD:AB = " + s.m + ":" + s.n + ", BC=" + Cm(s.bc) + "일 때,\nDE의 길이를 구하시오.";
                case Kind.Three:
                    return "세 평행선 l, m, n이 두 직선과 만난다.\n한 직선 위에서 l과 m 사이, m과 n 사이가 각각 "
                           + Cm(s.target - s.fAd) + ", " + Cm(s.ab - s.target)
                           + "이고,\n다른 직선 위에서는 각각 " + Cm(s.ae) + ", x cm일 때,\nx의 값을 구하시오.";
                case Kind.Mid:
                    return "△ABC에서 AB, AC의 중점을 각각 M, N이라고 하자.\nBC=" + Cm(s.bc) + "일 때, MN의 길이를 구하시오.";
                case Kind.Cent:
                    return "점 G가 △ABC의 무게중심이고\n중선 AD의 길이가 " + Cm(s.median) + "일 때,\nAG의 길이를 구하시오.";
                case Kind.Area:
                    return "넓이가 " + Cm2(s.area) + "인 △ABC의 무게중심을 G라고 할 때,\n△GBC의 넓이를 구하시오.";
                case Kind.Fix:
                    if (s.showWhole)
                        return "△ABC에서 DE는 BC와 평행하고\nAB=" + Cm(s.ab) + ", BC=" + Cm(s.bc) + "일 때,\nDE의 길이가 " + Cm(s.de) + "가 되게 하는 AD의 길이를 구하시오.";
                    return "△ABC에서 DE는 BC와 평행하고\nAB=" + Cm(s.ab) + ", AD:DB = " + s.m + ":" + s.n + "이어야 한다.\n잘못 걸린 수문을 옮겨 AD의 길이를 구하시오.";
                default:
                    return "△ABC에서 DE는 BC와 평행하다.\nAD의 길이를 구하시오.";
            }
        }

        public static string Answer(Sheet s)
        {
            if (s.askedUnit == "cm²") return Cm2(s.asked);
            if (s.kind == Kind.Three) return s.asked.ToString();
            return Cm(s.asked);
        }

        public static string Concept(Sheet s)
        {
            switch (s.kind)
            {
                case Kind.Part:
                case Kind.FindEc: return "삼각형에서 평행선과 선분의 길이의 비";
                case Kind.DeLen:
                case Kind.Similar: return "평행선으로 생기는 닮음(AD:AB = DE:BC)";
                case Kind.Fix: return s.showWhole
                    ? "평행선으로 생기는 닮음(AD:AB = DE:BC)"
                    : "삼각형에서 평행선과 선분의 길이의 비";
                case Kind.Three: return "평행선 사이의 선분의 길이의 비";
                case Kind.Mid: return "삼각형의 중점연결정리";
                case Kind.Cent: return "무게중심은 중선을 꼭짓점으로부터 2:1로 나눈다";
                case Kind.Area: return "무게중심과 삼각형의 넓이";
                default: return "평행선과 선분의 길이의 비";
            }
        }

        public static string Goal(Sheet s)
        {
            if (s.isPlumb) return "추를 중선 비에 맞춰 잠가라";
            return "수문을 비에 맞춰 잠가라";
        }

        public static string RevealRight(Sheet s)
        {
            switch (s.kind)
            {
                case Kind.Part:
                    return "AD:DB = AE:EC = " + s.m + ":" + s.n + " → AD=" + Cm(s.target) + ", DB=" + Cm(s.ab - s.target);
                case Kind.FindEc:
                    return "AD:DB = AE:EC → " + s.target + ":" + (s.ab - s.target) + " = " + s.ae + ":" + s.ec + " → EC=" + Cm(s.ec);
                case Kind.DeLen:
                case Kind.Similar:
                    Judge.Ratio(s.target, s.ab, out int m, out int n);
                    return "AD:AB = DE:BC = " + m + ":" + n + " → DE=" + Cm(s.de);
                case Kind.Fix:
                    if (s.showWhole)
                    {
                        Judge.Ratio(s.target, s.ab, out int mw, out int nw);
                        return "AD:AB = DE:BC = " + mw + ":" + nw + " → DE=" + Cm(s.de);
                    }
                    return "AD:DB = " + s.m + ":" + s.n + " → AD=" + Cm(s.target) + ", DB=" + Cm(s.ab - s.target);
                case Kind.Three:
                    return "대응하는 선분의 비가 같다 → x=" + s.asked;
                case Kind.Mid:
                    return "중점을 연결한 선분은 밑변의 ½ → MN=" + Cm(s.bc / 2);
                case Kind.Cent:
                    return "무게중심은 중선을 꼭짓점으로부터 2:1로 나눈다 → AG=" + Cm(s.target);
                case Kind.Area:
                    return "무게중심에서 △GBC의 넓이는 전체의 ⅓ → " + Cm2(s.areaBot);
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
                    return "무게중심은 중점을 1:1이 아니라, 꼭짓점으로부터 2:1이다.";
                case "M4_any_line_area":
                    return "넓이를 이등분하는 것은 중선뿐이다.";
                default:
                    return RevealRight(s);
            }
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
            var s = new Sheet
            {
                no = 0, stage = 0, kind = Kind.Part,
                ab = 6, ac = 6, bc = 8,
                m = 1, n = 1, target = 3, ad0 = 1, ad = 1,
                adLo = 1, adHi = 5,
                de = 4, ae = 3, ec = 3,
                asked = 3, askedUnit = "cm"
            };
            return s;
        }

        public List<Sheet> Deck()
        {
            var d = new List<Sheet>();
            d.Add(Part(1, 1, 9, 2, 1));
            d.Add(FindEc(2, 1));
            d.Add(FixPart(3, 1));
            d.Add(DeLen(4, 2));
            d.Add(Similar(5, 2));
            d.Add(Three(6, 2));
            d.Add(FixDe(7, 2));
            d.Add(Mid(8, 3));
            d.Add(Cent(9, 3));
            d.Add(Area(10, 3));
            return d;
        }

        public Sheet Any()
        {
            int k = r.Next(14);
            switch (k)
            {
                case 0:
                case 1: return PartAny();
                case 2: return FindEcAny();
                case 3: return DeLenAny(Kind.DeLen);
                case 4: return DeLenAny(Kind.Similar);
                case 5: return Three(0, 2);
                case 6:
                case 7: return MidAny();
                case 8:
                case 9: return Cent(0, 3);
                case 10:
                case 11: return Area(0, 3);
                case 12: return FixPart(0, 1);
                default: return FixDe(0, 2);
            }
        }

        Sheet PartAny()
        {
            for (int t = 0; t < 40; t++)
            {
                int m = 1 + r.Next(5), n = 1 + r.Next(5);
                int g = Judge.Gcd(m, n); m /= g; n /= g;
                int k = 1 + r.Next(5);
                int ab = k * (m + n);
                if (ab < 8 || ab > 18) continue;
                int ad = k * m;
                if (ad < 2 || ad > ab - 2) continue;
                int bc = PickBcFor(ab, ad, new[] { ab, ab + 3, ab - 3, 12, 9, 15, 6, 8, 10 });
                if (!BcOk(ab, ad, bc)) continue;
                var s = Base(0, 1, Kind.Part, ab, ab, bc, ad);
                s.m = m; s.n = n; s.ae = ad; s.ec = ab - ad;
                int de = Judge.De(s, ad); s.de = de > 0 ? de : 0;
                s.asked = ad;
                s.misTick = MidMis(ab, ad);
                s.misconceptionId = "M1_part_as_similar";
                return s;
            }
            return Part(0, 1, 12, 2, 1);
        }

        Sheet FindEcAny()
        {
            for (int t = 0; t < 50; t++)
            {
                int ab = 8 + r.Next(9);
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
                s.asked = ec;
                s.misTick = MidMis(ab, ad);
                s.misconceptionId = "M1_part_as_similar";
                return s;
            }
            return FindEc(0, 1);
        }

        Sheet DeLenAny(Kind knd)
        {
            for (int t = 0; t < 50; t++)
            {
                int ab = 8 + r.Next(9);
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
                s.asked = de;
                s.showWhole = true;
                s.misconceptionId = "M1_part_as_similar";
                if ((ab * ad) % bc == 0)
                {
                    int mis = ab * ad / bc;
                    if (mis >= 1 && mis < ab && mis != ad) s.misTick = mis;
                }
                if (s.misTick == 0) s.misTick = MidMis(ab, ad);
                return s;
            }
            return knd == Kind.Similar ? Similar(0, 2) : DeLen(0, 2);
        }

        Sheet MidAny()
        {
            int ab = 8 + r.Next(6) * 2;
            int bc = 8 + r.Next(7) * 2;
            int ac = ab;
            if (ab + ac <= bc) bc = ab;
            int t = ab / 2;
            var s = Base(0, 3, Kind.Mid, ab, ac, bc, t);
            s.m = 1; s.n = 1; s.hideRatio = true;
            s.de = bc / 2; s.ae = t; s.ec = ab - t;
            s.asked = bc / 2;
            s.misTick = ab - 1;
            s.refuseBase = true;
            s.misconceptionId = "M2_mid_double";
            return s;
        }

        static readonly int[][] PartRatios = {
            new[] { 9, 2, 1 }, new[] { 9, 1, 2 }, new[] { 9, 4, 5 }, new[] { 9, 5, 4 },
            new[] { 12, 1, 1 }, new[] { 12, 2, 1 }, new[] { 12, 1, 2 }, new[] { 12, 3, 1 }, new[] { 12, 1, 3 }, new[] { 12, 5, 1 },
            new[] { 15, 2, 1 }, new[] { 15, 1, 2 }, new[] { 15, 2, 3 }, new[] { 15, 3, 2 }, new[] { 15, 4, 1 }
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
                if (ab < 8) ab = sum * 3;
                if (ab > 18) ab = sum * Math.Max(1, 18 / sum);
            }
            int t = ab * m / sum;
            int[] prefer = ab <= 9 ? new[] { 12, 9, 6, 8, 10 } : ab <= 12 ? new[] { 12, 9, 15, 6, 8, 10 } : new[] { 15, 12, 18, 10, 9 };
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

        Sheet FindEc(int no, int stage)
        {
            // 제약 풀: AB | AC·AD 가 되게 역으로 뽑음
            int[][] pool = {
                new[] { 9, 6, 8, 6, 4, 2 },  // 교과서형 AD=6 DB=3 AE=4 EC=2
                new[] { 9, 12, 10, 6, 8, 4 },
                new[] { 12, 8, 10, 9, 6, 2 },
                new[] { 12, 6, 10, 8, 4, 2 },
                new[] { 12, 9, 10, 8, 6, 3 },
                new[] { 15, 10, 12, 9, 6, 4 },
                new[] { 15, 6, 12, 10, 4, 2 },
                new[] { 9, 15, 12, 6, 10, 5 },
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
            s.asked = ec;
            s.misconceptionId = "M1_part_as_similar";
            s.misTick = MidMis(ab, t);
            return s;
        }

        Sheet DeLen(int no, int stage)
        {
            // (ab,ac,bc,target,de,misTick) — 교차곱·오개념 칸이 모두 정수
            int[][] pool = {
                new[] { 12, 12, 8, 6, 4, 9 },   // 정답 DE=4 at AD=6, 오개념 DE=AD=6 at AD=9
                new[] { 10, 10, 15, 6, 9, 4 },
                new[] { 12, 12, 9, 8, 6, 4 },   // 8*9=72, 12*6=72; mis AD for DE=8 → 8*12/9 not int; use 4 as 1:2
                new[] { 9, 9, 12, 6, 8, 4 },    // 6*12=72, 9*8=72; mis≈4.5 → 4
                new[] { 15, 15, 10, 9, 6, 6 },
                new[] { 12, 16, 12, 8, 8, 4 },
                new[] { 9, 12, 9, 6, 6, 3 },
                new[] { 15, 15, 12, 10, 8, 5 }
            };
            var p = Pick(pool);
            int ab = p[0], ac = p[1], bc = p[2], t = p[3], de = p[4], mis = p[5];
            Judge.Ratio(t, ab, out int m, out int n);
            var s = Base(no, stage, Kind.DeLen, ab, ac, bc, t);
            s.m = m; s.n = n; s.de = de;
            s.ae = Judge.Ae(s, t);
            if (s.ae < 0) s.ae = 0;
            s.ec = s.ae > 0 ? s.ac - s.ae : 0;
            s.asked = de;
            s.misTick = mis;
            s.showWhole = true;
            s.misconceptionId = "M1_part_as_similar";
            return s;
        }

        Sheet Similar(int no, int stage)
        {
            var s = DeLen(no, stage);
            s.kind = Kind.Similar;
            Judge.Ratio(s.target, s.ab, out s.m, out s.n);
            s.asked = s.de;
            return s;
        }

        Sheet Three(int no, int stage)
        {
            // {ab,ac,bc, AF, AD, AE, ED, EC=x} — AF:FD:DB = AE:ED:EC, 발문 교차곱 (AD-AF):(AB-AD) = ED:x
            int[][] pool = {
                new[] { 12, 18, 16, 2, 6, 3, 6, 9 }, // 4:6 = 6:9
                new[] { 12, 18, 15, 2, 6, 3, 6, 9 },
                new[] { 10, 15, 12, 2, 6, 3, 6, 6 },
                new[] { 15, 15, 12, 3, 9, 3, 6, 6 }, // was x=3 (6:6=6:x 를 3으로 저장하던 행)
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
            if (!Judge.Prop(f, fd, ae, ed)) return null;          // AF:FD = AE:ED
            if (!Judge.Prop(fd, db, ed, x)) return null;          // FD:DB = ED:EC  (= 발문 6:6=6:x)
            if ((long)ac * fd % ab != 0 || ac * fd / ab != ed) return null;
            if ((long)ac * db % ab != 0 || ac * db / ab != x) return null;
            var s = Base(no, stage, Kind.Three, ab, ac, bc, t);
            s.fAd = f;
            s.ae = ed; // 발문의 「다른 직선 위 l–m 구간」
            s.ec = x; s.asked = x; s.askedUnit = "cm";
            s.de = Judge.De(s, t); if (s.de < 0) s.de = 0;
            s.misconceptionId = "M1_part_as_similar";
            s.misTick = f + fd / 2; if (s.misTick == t) s.misTick = t - 1;
            if (s.misTick < 1) s.misTick = 1;
            if (!Judge.Satisfies(s, t) || Judge.UniqueHits(s) != 1) return null;
            return s;
        }

        Sheet Mid(int no, int stage)
        {
            int ab = PickI(10, 12, 14, 16);
            int bc = PickI(8, 10, 12, 14, 16, 18);
            if (bc % 2 != 0) bc += 1;
            int t = ab / 2;
            var s = Base(no, stage, Kind.Mid, ab, ab, bc, t);
            s.m = 1; s.n = 1; s.hideRatio = true;
            s.de = bc / 2; s.ae = t; s.ec = ab - t;
            s.asked = bc / 2;
            s.misTick = ab - 1; // 밑변에 겹침
            s.refuseBase = true;
            s.misconceptionId = "M2_mid_double";
            return s;
        }

        Sheet Cent(int no, int stage)
        {
            // 이등변 + 피타고라스: AB² = median² + (BC/2)²
            int[][] pool = {
                new[] { 13, 10, 12, 8, 60 },
                new[] { 15, 18, 9, 6, 81 },
                new[] { 10, 16, 6, 4, 48 },
                new[] { 17, 16, 15, 10, 120 }
            };
            var p = Pick(pool);
            int ab = p[0], bc = p[1], med = p[2], t = p[3], area = p[4];
            var s = Base(no, stage, Kind.Cent, ab, ab, bc, t);
            s.isPlumb = true; s.median = med; s.area = area; s.areaBot = area / 3;
            s.hideRatio = true;
            s.asked = t;
            s.adHi = med - 1;
            s.misTick = med / 2; // 1:1 중점
            s.misconceptionId = "M3_cent_mid_or_rev";
            return s;
        }

        Sheet Area(int no, int stage)
        {
            // 밑변 8 · 높이(중선) 9. 넓이는 3의 배수만(⅓이 자연수). AB 길이는 화면에 안 씀.
            int[] areas = { 24, 30, 36, 42, 48, 54, 60 };
            int area = areas[r.Next(areas.Length)];
            var s = Base(no, stage, Kind.Area, 9, 9, 8, 6);
            s.isPlumb = true; s.median = 9; s.area = area; s.areaBot = area / 3;
            s.hideRatio = true;
            s.asked = area / 3; s.askedUnit = "cm²";
            s.adHi = 8;
            s.misTick = 4; // 역비 1:2 (변 쪽이 2)
            s.misconceptionId = "M4_any_line_area";
            return s;
        }

        Sheet FixPart(int no, int stage)
        {
            var s = Part(no, stage, 12, 2, 1); // AD=8, BC는 DE가 정수인 것만
            s.kind = Kind.Fix;
            s.showWhole = false;
            s.misTick = 6; // 1:1
            s.ad0 = s.misTick; s.ad = s.ad0;
            s.misconceptionId = "M1_part_as_similar";
            s.asked = s.target;
            return s;
        }

        Sheet FixDe(int no, int stage)
        {
            var s = DeLen(no, stage);
            s.kind = Kind.Fix;
            s.showWhole = true;
            if (s.misTick == s.target || s.misTick < 1) s.misTick = Math.Max(1, s.target - 2);
            s.ad0 = s.misTick; s.ad = s.ad0;
            s.asked = s.target;
            s.misconceptionId = "M1_part_as_similar";
            return s;
        }

        static Sheet Base(int no, int stage, Kind k, int ab, int ac, int bc, int target)
        {
            var s = new Sheet
            {
                no = no, stage = stage, kind = k,
                ab = ab, ac = ac, bc = bc,
                target = target, ad0 = 1, ad = 1,
                adLo = 1, adHi = (k == Kind.Cent || k == Kind.Area) ? 8 : ab - 1,
                askedUnit = "cm"
            };
            return s;
        }

        static int MidMis(int ab, int t)
        {
            int mid = ab / 2;
            if (mid != t && mid >= 1) return mid;
            int rev = ab - t;
            if (rev != t && rev >= 1 && rev < ab) return rev;
            return t > 2 ? t - 1 : t + 1;
        }

        public bool FitBroken(Sheet s, int broken)
        {
            int span = s.isPlumb ? s.median : s.ab;
            s.adLo = 1;
            s.adHi = Math.Max(1, span - 1 - broken);
            if (s.target < s.adLo || s.target > s.adHi) return false;
            if (s.ad0 < s.adLo || s.ad0 > s.adHi) s.ad0 = s.adLo;
            if (s.ad0 == s.target && s.kind != Kind.Fix) { s.ad0 = s.adLo; if (s.ad0 == s.target) return false; }
            s.ad = s.ad0;
            return true;
        }

        public Sheet NextFitted(int no, int stage, int broken)
        {
            for (int i = 0; i < 48; i++)
            {
                Sheet s;
                if (stage <= 1) s = i % 3 == 0 ? FindEc(no, 1) : i % 3 == 1 ? FixPart(no, 1) : Part(no, 1, 0, 0, 0);
                else if (stage == 2) s = i % 4 == 0 ? Three(no, 2) : i % 4 == 1 ? FixDe(no, 2) : i % 4 == 2 ? Similar(no, 2) : DeLen(no, 2);
                else s = i % 3 == 0 ? Area(no, 3) : i % 3 == 1 ? Cent(no, 3) : Mid(no, 3);
                s.no = no; s.stage = stage;
                if (FitBroken(s, broken)) return s;
            }
            int[][] fb = {
                new[] { 12, 1, 1 }, new[] { 12, 2, 1 }, new[] { 12, 1, 2 },
                new[] { 9, 2, 1 }, new[] { 9, 1, 2 }, new[] { 15, 1, 2 }, new[] { 15, 2, 1 }
            };
            for (int i = 0; i < fb.Length; i++)
            {
                var f = Part(no, stage, fb[i][0], fb[i][1], fb[i][2]);
                if (FitBroken(f, broken)) return f;
            }
            var last = Part(no, stage, 12, 1, 1);
            FitBroken(last, broken);
            return last;
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
