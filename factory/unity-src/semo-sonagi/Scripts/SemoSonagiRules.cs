// 세모 소나기 — 중2 피타고라스 정리의 정수 판정 정본.
//
// curriculum/2022-middle-math.json 의 이번 단원 style_guide / expression_traps:
// - 가장 긴 변을 c로 두고 a²+b²=c²인지 정수식으로 비교한다.
// - 제곱근의 근삿값이나 부동소수점 비교로 직각 여부를 판정하지 않는다.
// - 넓이 자료는 세 변 위 정사각형의 넓이 A,B,C이므로 A+B=C를 비교한다.
// - 발문은 교과서의 판별·선택 명령형을 살린
//   「직각삼각형인 우산 두 개를 모두 골라 한 획으로 쓸어라」로 통일한다.
// - 개략도는 실제 각의 크기와 다를 수 있으며 모든 후보에 같은 실루엣을 쓴다.
//
// 오개념 역산 id:
// - no_square: 변의 길이를 제곱하지 않고 a+b와 c가 가깝다는 이유로 고른다.
// - near_square: 제곱의 합이 비슷하면 같다고 어림한다.
// - hypotenuse_by_position: 화면 아래쪽/오른쪽 변을 자동으로 빗변이라 여긴다.
// - silhouette_guess: 개략도 모양만 보고 직각이라고 여긴다.
using System;
using System.Collections.Generic;
using Mgf;

namespace Mgf.SemoSonagi
{
    public enum SonagiMode { Area, Length }

    [Serializable]
    public struct TriangleDatum
    {
        public int a, b, c;
        public string misconceptionId;
        public int displayTurn;
        public TriangleDatum(int x, int y, int z, string misconception, int turn)
        {
            a=x;b=y;c=z;misconceptionId=misconception;displayTurn=turn;
            Sort(ref a,ref b,ref c);
        }
        static void Sort(ref int x,ref int y,ref int z)
        {
            if(x>y){int t=x;x=y;y=t;}if(y>z){int t=y;y=z;z=t;}if(x>y){int t=x;x=y;y=t;}
        }
    }

    [Serializable]
    public sealed class SonagiProblem
    {
        public string id;
        public SonagiMode mode;
        public TriangleDatum[] candidates;
        public int correctMask;
        public int band;
        public bool rotatedLabels;
        // 마지막 묶음은 공방의 잘못된 인증 '주장'을 검산하는 감사 문제다.
        // 후보 전체가 이미 인증됐다는 뜻으로 쓰면 안 된다.
        public bool certificationAudit;
        public string prompt;
        public string unitConcept;
    }

    public static class SemoSonagiRules
    {
        public const int StartLives=3;
        public const int TargetSolved=6;
        public const int RequiredFirstCorrect=5;
        public const float SessionSeconds=120f;
        public static readonly int[] PairMasks={3,5,9,6,10,12};
        static readonly int[,] Triples={
            {3,4,5},{5,12,13},{6,8,10},{9,12,15},{8,15,17},{12,16,20},{7,24,25}
        };

        public static bool IsRight(SonagiMode mode,TriangleDatum t)
        {
            if(mode==SonagiMode.Area)return t.a+t.b==t.c;
            return t.a*t.a+t.b*t.b==t.c*t.c;
        }

        public static bool IsActualTriangle(SonagiMode mode,TriangleDatum t)
        {
            if(mode==SonagiMode.Length)return t.a+t.b>t.c;
            // 넓이 A,B,C는 실제 변의 제곱. sqrt(A)+sqrt(B)>sqrt(C)를
            // 제곱근 없이 (C-A-B)^2 < 4AB 로 검사한다.
            int d=t.c-t.a-t.b;
            return d*d<4*t.a*t.b;
        }

        public static bool MaskIsCorrect(SonagiProblem p,int mask)
        { return p!=null&&mask==p.correctMask; }

        static TriangleDatum RightArea(int seed,int turn)
        {
            int a=4+(seed*7)%22,b=5+(seed*11)%21;
            return new TriangleDatum(a,b,a+b,"",turn);
        }

        static TriangleDatum FalseArea(int seed,bool near,int turn)
        {
            int a=4+(seed*5)%21,b=6+(seed*9)%20;
            int delta=near?(seed%2==0?1:-1):(seed%2==0?2:-2);
            return new TriangleDatum(a,b,a+b+delta,near?"near_square":"no_square",turn);
        }

        static TriangleDatum RightLength(int seed,int turn)
        {
            int row=Math.Abs(seed)%Triples.GetLength(0),scale=1+(Math.Abs(seed)/Triples.GetLength(0))%3;
            return new TriangleDatum(Triples[row,0]*scale,Triples[row,1]*scale,Triples[row,2]*scale,"",turn);
        }

        static TriangleDatum FalseLength(int seed,bool near,int turn)
        {
            if(!near)
            {
                int a=4+Math.Abs(seed)%8,b=5+(Math.Abs(seed)*3)%11;
                return new TriangleDatum(a,b,a+b-1,"no_square",turn);
            }
            int row=Math.Abs(seed)%Triples.GetLength(0),scale=1+(Math.Abs(seed)/Triples.GetLength(0))%2;
            int a0=Triples[row,0]*scale,b0=Triples[row,1]*scale,c0=Triples[row,2]*scale;
            int c=c0+(seed%2==0?1:-1);
            if(c<=b0)c=c0+1;
            return new TriangleDatum(a0,b0,c,"near_square",turn);
        }

        static SonagiProblem Make(string id,SonagiMode mode,int seed,int pairIndex,int band,bool rotated,bool certificationAudit)
        {
            int mask=PairMasks[((pairIndex%6)+6)%6];
            var c=new TriangleDatum[4];int truth=0,wrong=0;
            for(int i=0;i<4;i++)
            {
                bool right=(mask&(1<<i))!=0;
                int turn=rotated?(seed+i*2)%3:0;
                c[i]=mode==SonagiMode.Area
                    ?(right?RightArea(seed*7+truth++*13+i,turn):FalseArea(seed*11+wrong++*17+i,wrong%2==0,turn))
                    :(right?RightLength(seed*5+truth++*11+i,turn):FalseLength(seed*7+wrong++*13+i,wrong%2==0,turn));
            }
            return new SonagiProblem{
                id=id,mode=mode,candidates=c,correctMask=mask,band=band,rotatedLabels=rotated,certificationAudit=certificationAudit,
                prompt=mode==SonagiMode.Area
                    ?"세 정사각형의 넓이를 비교하라.\n직각삼각형인 우산 두 개를 모두 골라 한 획으로 쓸어라."
                    :"세 변의 길이를 비교하라.\n직각삼각형인 우산 두 개를 모두 골라 한 획으로 쓸어라.",
                unitConcept=mode==SonagiMode.Area?"피타고라스 정리와 세 정사각형의 넓이":"피타고라스 정리의 역과 직각삼각형 판별"
            };
        }

        public static SonagiProblem Practice()
        {
            return new SonagiProblem{
                id="ss-practice",mode=SonagiMode.Area,band=0,correctMask=1,
                candidates=new[]{
                    new TriangleDatum(9,16,25,"",0),new TriangleDatum(9,16,26,"near_square",0),
                    new TriangleDatum(7,18,25,"",0),new TriangleDatum(7,18,26,"near_square",0)
                },
                prompt="9+16=25인 우산만 골라\n천을 가로질러 쓸고 놓아라.",unitConcept="세 정사각형의 넓이 관계"
            };
        }

        // 난도 밴드를 순서대로 소진하고, 정답 위치 쌍은 여섯 경우를 정확히 한 번씩 쓴다.
        public static List<SonagiProblem> RunDeck(int serial)
        {
            int shift=Math.Abs(serial)%6;
            return new List<SonagiProblem>{
                Make("ss-r1",SonagiMode.Area,101+serial,(0+shift)%6,1,false,false),
                Make("ss-r2",SonagiMode.Area,127+serial,(1+shift)%6,1,false,false),
                Make("ss-r3",SonagiMode.Length,151+serial,(2+shift)%6,2,false,false),
                Make("ss-r4",SonagiMode.Length,181+serial,(3+shift)%6,2,false,false),
                Make("ss-r5",SonagiMode.Length,211+serial,(4+shift)%6,3,true,false),
                Make("ss-r6",SonagiMode.Length,241+serial,(5+shift)%6,3,true,true)
            };
        }

        public static string CandidateLabel(SonagiProblem p,int i)
        {
            var t=p.candidates[i];int x=t.a,y=t.b,z=t.c;
            if(t.displayTurn==1){x=t.c;y=t.a;z=t.b;}else if(t.displayTurn==2){x=t.b;y=t.c;z=t.a;}
            string unit=p.mode==SonagiMode.Area?" cm²":" cm";
            return x+" · "+y+" · "+z+unit;
        }

        public static string Formula(SonagiProblem p,int i)
        {
            var t=p.candidates[i];
            return p.mode==SonagiMode.Area
                ?t.a+" + "+t.b+(IsRight(p.mode,t)?" = ":" ≠ ")+t.c
                :t.a+"² + "+t.b+"² = "+(t.a*t.a+t.b*t.b)+(IsRight(p.mode,t)?" = ":" ≠ ")+t.c+"²("+(t.c*t.c)+")";
        }

        public static int PairIndex(int mask)
        {for(int i=0;i<PairMasks.Length;i++)if(PairMasks[i]==mask)return i;return -1;}

        static string PairLabel(int mask)
        {
            string[] n={"ㄱ","ㄴ","ㄷ","ㄹ"};var s=new List<string>();
            for(int i=0;i<4;i++)if((mask&(1<<i))!=0)s.Add(n[i]);
            return string.Join("·",s.ToArray());
        }

        static MgfProblem ToBank(SonagiProblem p)
        {
            var choices=new string[PairMasks.Length];for(int i=0;i<choices.Length;i++)choices[i]=PairLabel(PairMasks[i]);
            string all="";string[] ids={"ㄱ","ㄴ","ㄷ","ㄹ"};
            for(int i=0;i<4;i++)all+=(i==0?"":" / ")+ids[i]+" "+CandidateLabel(p,i);
            return new MgfProblem{
                id=p.id,prompt=p.prompt+" "+all,choices=choices,answer=PairLabel(p.correctMask),
                answerNumeric=PairIndex(p.correctMask),unitConcept=p.unitConcept
            };
        }

        // 360개를 사후 난수 필터 없이 제약 만족 풀에서 직접 만든다.
        // 각 거짓 자료는 near_square와 no_square를 번갈아 포함하고 모든 자료가 실제 삼각형이다.
        public static List<SonagiProblem> BuildProblemPool()
        {
            var pool=new List<SonagiProblem>(360);
            for(int i=0;i<120;i++)pool.Add(Make("ss-bank-area-"+i,SonagiMode.Area,1000+i,i%6,1,false,false));
            for(int i=0;i<120;i++)pool.Add(Make("ss-bank-length-"+i,SonagiMode.Length,2000+i,(i*5)%6,2,false,false));
            for(int i=0;i<120;i++)pool.Add(Make("ss-bank-audit-"+i,SonagiMode.Length,3000+i,(i*7)%6,3,true,i%2==0));
            return pool;
        }

        public static List<MgfProblem> BuildBank()
        {
            var source=BuildProblemPool();var bank=new List<MgfProblem>(source.Count);
            for(int i=0;i<source.Count;i++)bank.Add(ToBank(source[i]));return bank;
        }

        public static bool ValidatePool(out string error)
        {
            var pool=BuildProblemPool();
            for(int p=0;p<pool.Count;p++)
            {
                int count=0;
                for(int i=0;i<4;i++)
                {
                    var t=pool[p].candidates[i];
                    if(!IsActualTriangle(pool[p].mode,t)){error=pool[p].id+" actual-triangle";return false;}
                    bool right=IsRight(pool[p].mode,t);if(right)count++;
                    if(right!=((pool[p].correctMask&(1<<i))!=0)){error=pool[p].id+" mask";return false;}
                    if(!right&&(string.IsNullOrEmpty(t.misconceptionId))){error=pool[p].id+" misconception";return false;}
                }
                if(count!=2){error=pool[p].id+" truth-count";return false;}
            }
            error="";return true;
        }

        // 실제 화면에 나오는 여섯 묶음도 문제 은행과 별도로 전수 검사한다.
        // certificationAudit는 '인증 주장' 표시에만 쓰고, 완료/반증은 IsRight 결과로만 정한다.
        public static bool ValidateRunDecks(out string error)
        {
            for(int serial=0;serial<6;serial++)
            {
                var deck=RunDeck(serial);
                if(deck.Count!=TargetSolved){error="run-"+serial+" count";return false;}
                for(int p=0;p<deck.Count;p++)
                {
                    int truth=0;
                    for(int i=0;i<deck[p].candidates.Length;i++)
                    {
                        bool right=IsRight(deck[p].mode,deck[p].candidates[i]);
                        if(right)truth++;
                        if(right!=((deck[p].correctMask&(1<<i))!=0))
                        {error=deck[p].id+" screen-claim-mask";return false;}
                    }
                    if(truth!=2){error=deck[p].id+" screen-truth-count";return false;}
                    if(deck[p].certificationAudit!=(p==deck.Count-1))
                    {error=deck[p].id+" audit-placement";return false;}
                }
            }
            error="";return true;
        }
    }
}
