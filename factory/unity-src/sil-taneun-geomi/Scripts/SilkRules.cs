// 실 타는 거미 — 중2 평행선과 선분의 길이의 비 정수 생성기.
//
// curriculum/2022-middle-math.json style_guide / expression_traps 반영:
// - 값 발문은 「~의 길이를 구하시오」로 통일하고 수와 단위는 띄어 쓴다(6 cm).
// - 대응 순서를 데이터로 보존한다. 그림을 뒤집어도 선분 이름과 비례식의 대응은 바뀌지 않는다.
// - 중2에는 근호를 쓰지 않으며 모든 길이와 판정은 int 교차곱으로 계산한다.
// - 증명의 「가정」「결론」 용어와 어림 표현을 생성하지 않는다.
//
// 오개념 역산 id:
// - part_part_for_whole: AD:DB=DE:BC로 부분:부분을 부분:전체에 적용.
// - reversed_correspondence: 대응 구간의 순서를 뒤집음.
// - midpoint_same_side / midpoint_double_side: 중점연결선분을 원래 변과 같거나 두 배로 봄.
// - additive_parallel_gap: 평행선 사이의 대응을 곱셈 비례가 아닌 가산 차로 처리.
using System;
using System.Collections.Generic;
using Mgf;

namespace Mgf.SilTaneunGeomi
{
    public enum SilkKind { PartRatio, PartWhole, Midpoint, ThreeParallel, ErrorRepair }

    [Serializable]
    public sealed class SilkProblem
    {
        public string id;
        public SilkKind kind;
        public int band;
        public int answer;
        public int a, b, c, d;
        public int wrongA, wrongB;
        public string target;
        public string prompt;
        public string evidence;
        public string formula;
        public string unitConcept;
        public bool mirrored;
    }

    public static class SilkRules
    {
        public const int MinLength = 1;
        public const int MaxLength = 24;
        public const int TargetRoutes = 9;
        public const int StartLives = 3;
        public const float RunSeconds = 120f;
        public const float QueueSeconds = 28f;

        static readonly int[,] Ratios = {
            {1,1}, {1,2}, {2,1}, {1,3}, {3,1}, {2,3}, {3,2}
        };

        static readonly string[] Zones = {
            "라일락 꽃길", "살구빛 꽃길", "이슬잎 꽃길", "유리천장 꽃길",
            "보랏빛 꽃길", "연두잎 꽃길", "새벽이슬 꽃길", "햇살창 꽃길",
            "꽃가루 문", "진주실 문", "온실 북쪽 길", "온실 남쪽 길",
            "덩굴문 길", "물방울 길", "연꽃 길", "민트잎 길"
        };

        static int Gcd(int x, int y)
        {
            while (y != 0) { int t = x % y; x = y; y = t; }
            return Math.Abs(x);
        }

        static void PickRatio(int k, int variant, bool divisorOnSecond, out int p, out int q)
        {
            // 제약 만족 목록에서 직접 고른다. 실패한 k 재추출이나 사후 필터링은 없다.
            var valid = new List<int>(7);
            for (int i = 0; i < Ratios.GetLength(0); i++)
            {
                int x = Ratios[i,0], y = Ratios[i,1];
                int divisor = divisorOnSecond ? y : x;
                if (k % divisor == 0 && Gcd(x,y) == 1) valid.Add(i);
            }
            int idx = valid[Math.Abs(variant) % valid.Count];
            p = Ratios[idx,0]; q = Ratios[idx,1];
        }

        static string Zone(int variant) => Zones[Math.Abs(variant) % Zones.Length];

        public static SilkProblem Practice()
        {
            return new SilkProblem {
                id="sg-practice", kind=SilkKind.PartRatio, band=0, answer=4,
                a=3, b=2, c=6, d=4, target="EC",
                prompt="△ABC에서 DE∥BC이고 AD=3 cm, DB=2 cm, AE=6 cm일 때, EC의 길이를 구하시오.",
                evidence="AD 3 cm : DB 2 cm = AE 6 cm : EC ? cm",
                formula="AD:DB=AE:EC  →  3:2=6:4",
                unitConcept="삼각형에서 평행선과 선분의 길이의 비"
            };
        }

        public static SilkProblem PartRatio(int k, int variant, int band=1)
        {
            int p,q; PickRatio(k,variant,true,out p,out q);
            int s=1+(Math.Abs(variant)/7)%4, t=k/q;
            int ad=p*s, db=q*s, ae=p*t;
            int reversed=(ae*p)%q==0?(ae*p)/q:0;
            if (p==q || reversed==k || reversed<1 || reversed>MaxLength) reversed=0;
            return new SilkProblem {
                id="sg-pr-"+k+"-"+variant, kind=SilkKind.PartRatio, band=band, answer=k,
                a=ad,b=db,c=ae,d=k,target="EC", mirrored=(variant&1)==1,
                prompt=Zone(variant)+"의 △ABC에서 DE∥BC이고 AD="+ad+" cm, DB="+db+" cm, AE="+ae+" cm일 때, EC의 길이를 구하시오.",
                evidence="AD "+ad+" cm : DB "+db+" cm = AE "+ae+" cm : EC ? cm",
                formula="AD:DB=AE:EC  →  "+ad+":"+db+"="+ae+":"+k,
                unitConcept="삼각형에서 평행선과 부분 선분의 길이의 비",
                wrongA=reversed, wrongB=k==1?2:k-1
            };
        }

        public static SilkProblem PartWhole(int k, int variant, bool repair=false)
        {
            int p,q; PickRatio(k,variant,false,out p,out q);
            int s=1+(Math.Abs(variant)/7)%4, r=k/p;
            int ad=p*s, db=q*s, ab=ad+db, bc=(p+q)*r;
            int wrong=(bc*ad)%db==0?(bc*ad)/db:0;
            if (wrong==k || wrong<1 || wrong>MaxLength) wrong=0;
            string memo=repair?" 느슨한 메모 AD:DB=DE:BC는 잘못되었다.":"";
            return new SilkProblem {
                id=(repair?"sg-er-":"sg-pw-")+k+"-"+variant,
                kind=repair?SilkKind.ErrorRepair:SilkKind.PartWhole, band=repair?3:2, answer=k,
                a=ad,b=db,c=bc,d=ab,target="DE",mirrored=(variant&1)==1,
                prompt=Zone(variant)+"의 △ABC에서 DE∥BC이고 AD="+ad+" cm, DB="+db+" cm, BC="+bc+" cm일 때, DE의 길이를 구하시오."+memo,
                evidence="AD "+ad+" cm · AB "+ab+" cm  /  DE ? cm · BC "+bc+" cm",
                formula="AD:AB=DE:BC  →  "+ad+":"+ab+"="+k+":"+bc,
                unitConcept=repair?"잘못된 부분:전체 대응 고치기":"삼각형에서 평행선과 전체 선분의 길이의 비",
                wrongA=wrong, wrongB=Math.Min(MaxLength,bc)
            };
        }

        public static SilkProblem Midpoint(int k, int variant)
        {
            int bc=2*k;
            return new SilkProblem {
                id="sg-mid-"+k+"-"+variant,kind=SilkKind.Midpoint,band=2,answer=k,
                a=bc,b=k,target="MN",mirrored=(variant&1)==1,
                prompt=Zone(variant)+"의 △ABC에서 M, N은 각각 AB, AC의 중점이고 BC="+bc+" cm일 때, MN의 길이를 구하시오.",
                evidence="M, N은 두 변의 중점  ·  MN∥BC  ·  BC "+bc+" cm",
                formula="MN=BC÷2  →  "+bc+"÷2="+k,
                unitConcept="삼각형의 중점연결정리",
                wrongA=bc<=MaxLength?bc:0, wrongB=2*bc<=MaxLength?2*bc:0
            };
        }

        public static SilkProblem ThreeParallel(int k, int variant, bool reverse)
        {
            int p,q; PickRatio(k,variant,reverse?false:true,out p,out q);
            int s=1+(Math.Abs(variant)/7)%4;
            int t=reverse?k/p:k/q;
            int lu=p*s, ll=q*s, ru=p*t, rl=q*t;
            int answer=reverse?ru:rl;
            int additive=reverse ? rl+(lu-ll) : ru+(ll-lu);
            if(additive==answer || additive<1 || additive>MaxLength) additive=0;
            string known=reverse?"오른쪽 아래 구간="+rl+" cm":"오른쪽 위 구간="+ru+" cm";
            string target=reverse?"오른쪽 위 구간":"오른쪽 아래 구간";
            return new SilkProblem {
                id="sg-tp-"+k+"-"+variant+"-"+(reverse?"r":"d"),kind=SilkKind.ThreeParallel,band=3,answer=answer,
                a=lu,b=ll,c=reverse?rl:ru,d=reverse?ru:rl,target=target,mirrored=(variant&1)==1,
                prompt=Zone(variant)+"에서 l∥m∥n이다. 왼쪽 위 구간="+lu+" cm, 왼쪽 아래 구간="+ll+" cm, "+known+"일 때, "+target+"의 길이를 구하시오.",
                evidence="l∥m∥n  ·  왼쪽 "+lu+":"+ll+" = 오른쪽 "+(reverse?"?":""+ru)+":"+(reverse?""+rl:"?"),
                formula=lu+":"+ll+"="+ru+":"+rl,
                unitConcept=reverse?"평행선 사이 선분의 길이의 비 역추적":"평행선 사이 선분의 길이의 비",
                wrongA=additive, wrongB=answer==1?2:answer-1
            };
        }

        public static SilkProblem ForRun(int routeIndex, int k, int variant)
        {
            if(routeIndex<=2) return PartRatio(k,variant,1);
            if(routeIndex==3) return PartWhole(k,variant,false);
            if(routeIndex==4) return Midpoint(k,variant);
            if(routeIndex==5) return PartWhole(k,variant,true);
            if(routeIndex==6) return ThreeParallel(k,variant,false);
            if(routeIndex==7) return ThreeParallel(k,variant,true);
            return PartWhole(k,variant,true);
        }

        public static List<SilkProblem> RunDeck(Random rng)
        {
            var deck=new List<SilkProblem>(TargetRoutes);
            for(int i=0;i<TargetRoutes;i++)
            {
                int k=1+rng.Next(MaxLength); // 먼저 k를 1..24에서 독립 균등 추출한다.
                deck.Add(ForRun(i,k,rng.Next(100000)));
            }
            return deck;
        }

        public static bool IsCorrect(SilkProblem p,int value)
        {
            if(p==null||value<MinLength||value>MaxLength)return false;
            switch(p.kind)
            {
                case SilkKind.PartRatio: return (long)p.a*value==(long)p.b*p.c;
                case SilkKind.PartWhole:
                case SilkKind.ErrorRepair: return (long)(p.a+p.b)*value==(long)p.a*p.c;
                case SilkKind.Midpoint: return 2L*value==p.a;
                default:
                    // a:b = (reverse이면 value:c, 아니면 c:value)
                    return p.target.Contains("위") ? (long)p.a*p.c==(long)p.b*value : (long)p.a*value==(long)p.b*p.c;
            }
        }

        public static string MisconceptionId(SilkProblem p,int value)
        {
            if(p==null||value==p.answer)return "";
            if(value==p.wrongA && p.wrongA>0)
            {
                if(p.kind==SilkKind.PartWhole||p.kind==SilkKind.ErrorRepair)return "part_part_for_whole";
                if(p.kind==SilkKind.PartRatio)return "reversed_correspondence";
                if(p.kind==SilkKind.Midpoint)return "midpoint_same_side";
                return "additive_parallel_gap";
            }
            if(value==p.wrongB && p.wrongB>0)
            {
                if(p.kind==SilkKind.Midpoint)return "midpoint_double_side";
                return "reversed_correspondence";
            }
            return value<p.answer?"silk_too_short":"silk_too_long";
        }

        public static List<MgfProblem> BuildBank()
        {
            var bank=new List<MgfProblem>(480);
            for(int k=1;k<=MaxLength;k++)
            for(int v=0;v<4;v++)
            {
                Add(bank,PartRatio(k,v+1));
                Add(bank,PartWhole(k,v+17,false));
                Add(bank,Midpoint(k,v+33));
                Add(bank,ThreeParallel(k,v+49,false));
                Add(bank,ThreeParallel(k,v+65,true));
            }
            return bank;
        }

        static void Add(List<MgfProblem> bank,SilkProblem p)
        {
            if(!IsCorrect(p,p.answer)) throw new InvalidOperationException("생성기 교차곱 불일치: "+p.id);
            bank.Add(new MgfProblem {
                id=p.id,prompt=p.prompt,choices=null,answer=p.answer+" cm",
                answerNumeric=p.answer,unitConcept=p.unitConcept
            });
        }
    }
}
