// 실 타는 거미 — 중2 평행선과 선분의 길이의 비 정수 생성기.
//
// curriculum/2022-middle-math.json style_guide / expression_traps 반영:
// - 값 발문은 「~의 길이를 구하시오」로 통일하고 수와 단위는 띄어 쓴다(6 cm).
// - 대응 순서를 데이터로 보존한다. 그림을 뒤집어도 선분 이름과 비례식의 대응은 바뀌지 않는다.
// - evidence(풀기 전 화면)에는 비례식을 쓰지 않는다. 대응(부분:부분/부분:전체)은 학생이 고르고,
//   완성된 비례식(formula)은 정답 판정 카드에서만 보인다.
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

        // 한 브라우저 세션에서 첫 길의 답은 24판마다 정확히 한 번씩 나온다.
        // 1→2→3→4 순환 입력과 같은 위상에 답이 몰리지 않도록 1~4를 서로 다른
        // 위상에 배치했다. 네 칸 단위 회전만 허용하므로 세션마다 시작점은 달라도
        // 고정/순환 무뇌 입력의 원시 성공률은 1/24를 넘지 않는다.
        static readonly int[] BalancedFirstAnswers = {
            5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,2,3,4,1
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
                prompt="△ABC에서 선분 DE와 선분 BC가 평행하다. 선분 AD, DB, AE의 길이가 각각 3 cm, 2 cm, 6 cm일 때, 선분 EC의 길이를 구하시오.",
                evidence="구할 선분 EC · 모식도의 실을 당겨 길이를 맞추시오",
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
                prompt=Zone(variant)+"의 △ABC에서 선분 DE와 선분 BC가 평행하다. 선분 AD, DB, AE의 길이가 각각 "+ad+" cm, "+db+" cm, "+ae+" cm일 때, 선분 EC의 길이를 구하시오.",
                evidence="구할 선분 EC",
                formula="AD:DB=AE:EC  →  "+ad+":"+db+"="+ae+":"+k,
                unitConcept="삼각형에서 평행선과 부분 선분의 길이의 비",
                wrongA=reversed, wrongB=0
            };
        }

        public static SilkProblem PartWhole(int k, int variant, bool repair=false)
        {
            int p,q; PickRatio(k,variant,false,out p,out q);
            int s=1+(Math.Abs(variant)/7)%4, r=k/p;
            int ad=p*s, db=q*s, ab=ad+db, bc=(p+q)*r;
            int wrong=(bc*ad)%db==0?(bc*ad)/db:0;
            if (wrong==k || wrong<1 || wrong>MaxLength) wrong=0;
            string memo=repair?" 잘못 세운 비례식 AD:DB=DE:BC를 고쳐 계산하시오.":"";
            return new SilkProblem {
                id=(repair?"sg-er-":"sg-pw-")+k+"-"+variant,
                kind=repair?SilkKind.ErrorRepair:SilkKind.PartWhole, band=repair?3:2, answer=k,
                a=ad,b=db,c=bc,d=ab,target="DE",mirrored=(variant&1)==1,
                prompt=Zone(variant)+"의 △ABC에서 선분 DE와 선분 BC가 평행하다. 선분 AD, DB, BC의 길이가 각각 "+ad+" cm, "+db+" cm, "+bc+" cm일 때, 선분 DE의 길이를 구하시오."+memo,
                evidence=repair?"AD:DB=DE:BC 는 잘못 세운 비례식 · 구할 선분 DE":"구할 선분 DE",
                formula="AD:AB=DE:BC  →  "+ad+":"+ab+"="+k+":"+bc,
                unitConcept=repair?"잘못된 부분:전체 대응 고치기":"삼각형에서 평행선과 전체 선분의 길이의 비",
                wrongA=wrong, wrongB=0
            };
        }

        public static SilkProblem Midpoint(int k, int variant)
        {
            int bc=2*k;
            return new SilkProblem {
                id="sg-mid-"+k+"-"+variant,kind=SilkKind.Midpoint,band=2,answer=k,
                a=bc,b=k,target="MN",mirrored=(variant&1)==1,
                prompt=Zone(variant)+"의 △ABC에서 M, N은 각각 선분 AB, AC의 중점이다. 선분 BC의 길이가 "+bc+" cm일 때, 선분 MN의 길이를 구하시오.",
                evidence="M, N은 두 변의 중점 · 구할 선분 MN",
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
            string known=reverse?"오른쪽의 m-n 구간은 "+rl+" cm":"오른쪽의 l-m 구간은 "+ru+" cm";
            string target=reverse?"오른쪽 l-m 구간":"오른쪽 m-n 구간";
            return new SilkProblem {
                id="sg-tp-"+k+"-"+variant+"-"+(reverse?"r":"d"),kind=SilkKind.ThreeParallel,band=3,answer=answer,
                a=lu,b=ll,c=reverse?rl:ru,d=reverse?ru:rl,target=target,mirrored=(variant&1)==1,
                prompt=Zone(variant)+"에서 서로 다른 두 직선이 서로 평행한 세 직선 l, m, n과 만난다. 왼쪽의 l-m 구간은 "+lu+" cm, m-n 구간은 "+ll+" cm이고, "+known+"일 때, "+target+"의 길이를 구하시오.",
                evidence="l∥m∥n · 구할 구간 "+target,
                formula=lu+":"+ll+"="+ru+":"+rl,
                unitConcept=reverse?"평행선 사이 선분의 길이의 비 역추적":"평행선 사이 선분의 길이의 비",
                wrongA=additive, wrongB=0
            };
        }

        public static SilkProblem ForRun(int routeIndex, int k, int variant)
        {
            SilkProblem p;
            if(routeIndex<=2)p=PartRatio(k,variant,1);
            else if(routeIndex==3)p=PartWhole(k,variant,false);
            else if(routeIndex==4)p=Midpoint(k,variant);
            else if(routeIndex==5)p=PartWhole(k,variant,true);
            else if(routeIndex==6)p=ThreeParallel(k,variant,false);
            else if(routeIndex==7)p=ThreeParallel(k,variant,true);
            else p=PartWhole(k,variant,true);
            p.id+="-route"+routeIndex;
            return p;
        }

        public static int BalancedFirstAnswer(int runSerial,int fourStepOffset)
        {
            int offset=((fourStepOffset%24)+24)%24;
            offset-=offset%4;
            int index=((runSerial-1+offset)%BalancedFirstAnswers.Length+BalancedFirstAnswers.Length)%BalancedFirstAnswers.Length;
            return BalancedFirstAnswers[index];
        }

        public static List<SilkProblem> RunDeck(Random rng,int firstAnswer)
        {
            var deck=new List<SilkProblem>(TargetRoutes);
            for(int i=0;i<TargetRoutes;i++)
            {
                int k=i==0?firstAnswer:1+rng.Next(MaxLength);
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
                    return p.target.Contains("l-m") ? (long)p.a*p.c==(long)p.b*value : (long)p.a*value==(long)p.b*p.c;
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
                return value<p.answer?"silk_too_short":"silk_too_long";
            }
            return value<p.answer?"silk_too_short":"silk_too_long";
        }

        public static List<MgfProblem> BuildBank()
        {
            // QA 은행과 실전은 반드시 같은 ForRun 팩토리를 지난다. routeIndex 0~8,
            // ErrorRepair, 세 평행선 direct/reverse, 배율 s=1~4를 결정적으로 모두 포함한다.
            var bank=new List<MgfProblem>(TargetRoutes*MaxLength*4);
            for(int route=0;route<TargetRoutes;route++)
            for(int k=1;k<=MaxLength;k++)
            for(int s=1;s<=4;s++)
            {
                // /7의 몫을 4로 나눈 나머지가 s-1이 되도록 구성한다.
                int variant=k*112+route*28+(s-1)*7+((k+route+s)&1);
                Add(bank,ForRun(route,k,variant));
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
