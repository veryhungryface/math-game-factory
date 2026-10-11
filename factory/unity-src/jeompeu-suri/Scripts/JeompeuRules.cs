// 점프 수리 — 중2 사각형의 대각선 성질, 정수 좌표 판정, 제약 만족 문항 은행.
//
// curriculum/textbook-structure.json expression_traps:
// - 두 대각선의 교점을 무조건 중점이라고 부르지 않는다. 각 대각선의 중점을 따로 비교한다.
// - 평행사변형: 두 대각선이 서로 다른 것을 이등분한다.
// - 직사각형: 평행사변형이면서 두 대각선의 길이가 같다.
// - 마름모: 평행사변형이면서 두 대각선이 수직이다.
// - 정사각형은 직사각형·마름모·평행사변형에 모두 포함된다.
// - 길이는 제곱길이 정수로, 이등분은 좌표합 정수로 판정한다. 제곱근·부동소수점 비교를 쓰지 않는다.
//
// 오개념 역산(misconceptionId):
// - equalDiagonalWithoutBisect: 대각선 길이만 같고 서로 이등분하지 않음.
// - perpendicularWithoutBisect: 대각선이 수직이기만 하고 서로 이등분하지 않음.
// - parallelogramNeedsEqualDiagonals: 평행사변형에 불필요한 같은 길이를 요구함.
// - exclusiveSquareCategory: 정사각형의 포함 관계를 배타적으로 생각함.
using System;
using System.Collections.Generic;
using Mgf;
using UnityEngine;

namespace Mgf.JeompeuSuri
{
    public enum QuadKind { Parallelogram, Rectangle, Rhombus, Square }

    [Serializable]
    public sealed class RepairProblem
    {
        public string id;
        public QuadKind kind;
        public int band;
        public Vector2Int[] start = new Vector2Int[4];
        public Vector2Int[] target = new Vector2Int[4];
        public int answerVertex;
        public int allowedMask;
        public string prompt;
        public string unitConcept;
        public string misconceptionId;

        public Vector2Int AnswerPoint => target[answerVertex];
        public int AnswerNumeric => answerVertex * 49 + AnswerPoint.y * 7 + AnswerPoint.x;
        public string AnswerText => VertexName(answerVertex) + "→" + AnswerPoint.x + "," + AnswerPoint.y;
        public static string VertexName(int i) => i == 0 ? "A" : i == 1 ? "B" : i == 2 ? "C" : "D";
    }

    public static class JeompeuRules
    {
        public const int GridSize = 7;
        public const int StartLives = 3;
        public const int CourseCount = 10;
        public const int RequiredFirstCorrect = 8;
        public const int RequiredBand3Correct = 3;
        public const float CourseSeconds = 90f;

        static readonly string[] KindNames = { "평행사변형", "직사각형", "마름모", "정사각형" };

        // 전수 검사로 유효 수정이 정확히 하나임을 확인한 씨앗들이다. 이후 생성은 정사각 격자의
        // 8개 대칭과 꼭짓점 순환만 사용하므로 유일성·정수 판정이 그대로 보존된다.
        // 배열: current Ax Ay Bx By Cx Cy Dx Dy | answerVertex answerX answerY
        static readonly int[][] PSeeds = {
            new[]{0,0, 0,3, 3,6, 2,4, 3,3,3},
            new[]{0,0, 0,4, 2,6, 1,3, 3,2,2},
            new[]{0,0, 2,1, 6,3, 5,1, 1,1,2},
            new[]{0,0, 2,2, 6,6, 5,3, 1,1,3}
        };
        static readonly int[][] RSeeds = {
            new[]{1,1, 0,2, 2,2, 2,0, 0,0,0},
            new[]{1,1, 0,2, 4,2, 4,0, 0,0,0},
            new[]{1,1, 0,3, 3,3, 3,0, 0,0,0},
            new[]{1,1, 0,3, 5,3, 5,0, 0,0,0}
        };
        static readonly int[][] HSeeds = {
            new[]{1,0, 0,2, 2,2, 2,0, 0,0,0},
            new[]{1,0, 0,3, 3,3, 3,0, 0,0,0},
            new[]{1,0, 1,3, 4,4, 3,1, 0,0,0},
            new[]{1,0, 1,4, 5,5, 4,1, 0,0,0}
        };
        static readonly int[][] SSeeds = {
            new[]{1,0, 0,2, 2,2, 2,0, 0,0,0},
            new[]{1,0, 0,3, 3,3, 3,0, 0,0,0},
            new[]{1,0, 0,4, 4,4, 4,0, 0,0,0},
            new[]{1,0, 3,0, 3,3, 0,3, 0,0,0}
        };
        static readonly int[] PSmallSquare = {0,0, 0,2, 1,1, 2,0, 2,2,2};
        static readonly int[] PSmallSlant = {0,0, 1,2, 2,1, 2,0, 2,3,2};
        static readonly int[] RSmall = {1,1, 0,2, 3,2, 3,0, 0,0,0};
        static readonly int[] RSquare = {1,1, 0,2, 2,2, 2,0, 0,0,0};
        static readonly int[] HSmall = {1,0, 1,2, 3,3, 2,1, 0,0,0};
        static readonly int[] HSquare = {1,0, 0,2, 2,2, 2,0, 0,0,0};

        public static string KindName(QuadKind kind) => KindNames[(int)kind];

        public static bool InGrid(Vector2Int p) => p.x >= 0 && p.x < GridSize && p.y >= 0 && p.y < GridSize;

        public static bool IsConvex(Vector2Int[] v)
        {
            if (v == null || v.Length != 4) return false;
            for (int i = 0; i < 4; i++)
                for (int j = i + 1; j < 4; j++) if (v[i] == v[j]) return false;
            int sign = 0;
            for (int i = 0; i < 4; i++)
            {
                Vector2Int a = v[i], b = v[(i + 1) & 3], c = v[(i + 2) & 3];
                int cross = (b.x - a.x) * (c.y - b.y) - (b.y - a.y) * (c.x - b.x);
                if (cross == 0) return false;
                int s = cross > 0 ? 1 : -1;
                if (sign != 0 && sign != s) return false;
                sign = s;
            }
            return true;
        }

        public static bool Bisects(Vector2Int[] v)
        {
            return v[0].x + v[2].x == v[1].x + v[3].x
                && v[0].y + v[2].y == v[1].y + v[3].y;
        }

        public static int DiagonalSquare(Vector2Int[] v, bool ac)
        {
            Vector2Int d = ac ? v[0] - v[2] : v[1] - v[3];
            return d.x * d.x + d.y * d.y;
        }

        public static int DiagonalDot(Vector2Int[] v)
        {
            Vector2Int ac = v[0] - v[2], bd = v[1] - v[3];
            return ac.x * bd.x + ac.y * bd.y;
        }

        public static bool Matches(Vector2Int[] v, QuadKind kind)
        {
            if (!IsConvex(v) || !Bisects(v)) return false;
            bool equal = DiagonalSquare(v, true) == DiagonalSquare(v, false);
            bool perp = DiagonalDot(v) == 0;
            if (kind == QuadKind.Parallelogram) return true;
            if (kind == QuadKind.Rectangle) return equal;
            if (kind == QuadKind.Rhombus) return perp;
            return equal && perp;
        }

        public static string MissingReason(Vector2Int[] v, QuadKind kind)
        {
            if (!IsConvex(v)) return "네 꼭짓점이 둘레 순서로 볼록하게 이어져야 한다";
            if (!Bisects(v)) return "두 대각선의 중점이 아직 서로 다르다";
            bool equal = DiagonalSquare(v, true) == DiagonalSquare(v, false);
            bool perp = DiagonalDot(v) == 0;
            if ((kind == QuadKind.Rectangle || kind == QuadKind.Square) && !equal) return "두 대각선의 길이가 아직 다르다";
            if ((kind == QuadKind.Rhombus || kind == QuadKind.Square) && !perp) return "두 대각선이 아직 수직이 아니다";
            return "요청한 사각형의 성질을 다시 확인하시오";
        }

        public static string Misconception(Vector2Int[] v, QuadKind kind)
        {
            if (!IsConvex(v)) return "nonConvexOrDuplicate";
            bool p = Bisects(v);
            bool eq = DiagonalSquare(v, true) == DiagonalSquare(v, false);
            bool perp = DiagonalDot(v) == 0;
            if (!p && eq && (kind == QuadKind.Rectangle || kind == QuadKind.Square)) return "equalDiagonalWithoutBisect";
            if (!p && perp && (kind == QuadKind.Rhombus || kind == QuadKind.Square)) return "perpendicularWithoutBisect";
            if (kind == QuadKind.Parallelogram && p && !eq) return "parallelogramNeedsEqualDiagonals";
            if ((kind == QuadKind.Rectangle || kind == QuadKind.Rhombus) && p && eq && perp) return "exclusiveSquareCategory";
            return p ? "missingAdditionalProperty" : "midpointsDoNotCoincide";
        }

        static Vector2Int Sym(Vector2Int p, int sym)
        {
            int x = p.x, y = p.y;
            switch (sym & 7)
            {
                case 0: return new Vector2Int(x, y);
                case 1: return new Vector2Int(6 - y, x);
                case 2: return new Vector2Int(6 - x, 6 - y);
                case 3: return new Vector2Int(y, 6 - x);
                case 4: return new Vector2Int(6 - x, y);
                case 5: return new Vector2Int(x, 6 - y);
                case 6: return new Vector2Int(y, x);
                default:return new Vector2Int(6 - y, 6 - x);
            }
        }

        static RepairProblem FromSeed(int[] s, QuadKind kind, int sym, int cycle, int shiftVariant, int serial)
        {
            var current = new Vector2Int[4];
            var target = new Vector2Int[4];
            for (int i = 0; i < 4; i++) current[i] = new Vector2Int(s[i * 2], s[i * 2 + 1]);
            for (int i = 0; i < 4; i++) target[i] = current[i];
            int answerVertex = s[8];
            target[answerVertex] = new Vector2Int(s[9], s[10]);

            var c2 = new Vector2Int[4]; var t2 = new Vector2Int[4];
            for (int i = 0; i < 4; i++)
            {
                int src = (i + cycle) & 3;
                c2[i] = Sym(current[src], sym);
                t2[i] = Sym(target[src], sym);
            }
            int minX=6,minY=6,maxX=0,maxY=0;
            for(int i=0;i<4;i++)
            {
                minX=Math.Min(minX,Math.Min(c2[i].x,t2[i].x)); minY=Math.Min(minY,Math.Min(c2[i].y,t2[i].y));
                maxX=Math.Max(maxX,Math.Max(c2[i].x,t2[i].x)); maxY=Math.Max(maxY,Math.Max(c2[i].y,t2[i].y));
            }
            int nx=7-(maxX-minX),ny=7-(maxY-minY);
            int sx=-minX+(shiftVariant%Math.Max(1,nx));
            int sy=-minY+((shiftVariant/Math.Max(1,nx))%Math.Max(1,ny));
            for(int i=0;i<4;i++){c2[i]+=new Vector2Int(sx,sy);t2[i]+=new Vector2Int(sx,sy);}
            answerVertex = (answerVertex - cycle + 4) & 3;
            string n = (serial + 1).ToString("000");
            return new RepairProblem {
                id = "js-bank-" + n,
                kind = kind,
                band = kind == QuadKind.Parallelogram ? 1 : kind == QuadKind.Square ? 3 : 2,
                start = c2, target = t2, answerVertex = answerVertex, allowedMask = 15,
                prompt = n + "번 점프대가 " + KindName(kind) + "이 되도록 꼭짓점 하나를 옮기시오.",
                unitConcept = KindName(kind) + "의 대각선 성질"
            };
        }

        static List<RepairProblem> bankCache;

        public static List<RepairProblem> BuildBank()
        {
            if(bankCache!=null)return bankCache;
            var bank = new List<RepairProblem>(384);
            QuadKind[] kinds = { QuadKind.Parallelogram, QuadKind.Rectangle, QuadKind.Rhombus, QuadKind.Square };
            for (int k = 0; k < kinds.Length; k++)
            {
                int[][] seeds = k == 0 ? PSeeds : k == 1 ? RSeeds : k == 2 ? HSeeds : SSeeds;
                for(int variant=0;variant<96;variant++)
                {
                    int cycle=variant&3, sym=(variant/4)&7, seed=(variant/32)%seeds.Length;
                    int shift=variant*17+sym*5+seed*11;
                    var p = FromSeed(seeds[seed], kinds[k], sym, cycle, shift, bank.Count);
                    if (!Matches(p.target, p.kind)) throw new Exception("문항 목표 판정 실패: " + p.id);
                    bank.Add(p);
                }
            }
            bankCache=bank;return bankCache;
        }

        static RepairProblem P(string id, QuadKind kind, int band, int allowedMask, int answerVertex,
            int[] start, int answerX, int answerY)
        {
            var v = new Vector2Int[4];
            for (int i = 0; i < 4; i++) v[i] = new Vector2Int(start[i * 2], start[i * 2 + 1]);
            var target = (Vector2Int[])v.Clone();
            target[answerVertex] = new Vector2Int(answerX, answerY);
            return new RepairProblem {
                id=id, kind=kind, band=band, start=v, target=target, answerVertex=answerVertex, allowedMask=allowedMask,
                prompt=KindName(kind) + "이 되도록 꼭짓점 하나를 옮기시오.",
                unitConcept=KindName(kind) + "의 대각선 성질"
            };
        }

        public static RepairProblem Practice()
        {
            return P("js-practice", QuadKind.Parallelogram, 0, 1 << 3, 3,
                new[]{1,1, 5,1, 4,4, 1,4}, 0,4);
        }

        public static List<RepairProblem> RunDeck(System.Random rng)
        {
            var deck = new List<RepairProblem>(10) {
                RuntimeFromSeed(PSmallSquare,QuadKind.Parallelogram,1,3,RandomPoint(rng),1,rng,true),
                RuntimeFromSeed(PSmallSlant,QuadKind.Parallelogram,1,3,RandomPoint(rng),2,rng,true),
                RuntimeFromSeed(PSmallSlant,QuadKind.Parallelogram,1,3,RandomPoint(rng),3,rng,true),
                RuntimeFromSeed(RSmall,QuadKind.Rectangle,2,rng.Next(4),RandomPoint(rng),4,rng,false),
                RuntimeFromSeed(HSmall,QuadKind.Rhombus,2,rng.Next(4),RandomPoint(rng),5,rng,false),
                RuntimeFromSeed(RSquare,QuadKind.Rectangle,2,rng.Next(4),RandomPoint(rng),6,rng,false),
                RuntimeFromSeed(HSquare,QuadKind.Square,3,rng.Next(4),RandomPoint(rng),7,rng,false),
                RuntimeFromSeed(HSquare,QuadKind.Square,3,rng.Next(4),RandomPoint(rng),8,rng,false),
                RuntimeFromSeed(RSquare,QuadKind.Rectangle,3,rng.Next(4),RandomPoint(rng),9,rng,false),
                RuntimeFromSeed(HSquare,QuadKind.Rhombus,3,rng.Next(4),RandomPoint(rng),10,rng,false)
            };
            // 밴드 안에서만 순서를 섞어 3→3→4 정보량 곡선을 보존한다.
            ShuffleRange(deck, 0, 3, rng); ShuffleRange(deck, 3, 3, rng); ShuffleRange(deck, 6, 4, rng);
            for (int i = 0; i < deck.Count; i++)
                if (!Matches(deck[i].target, deck[i].kind)) throw new Exception("코스 목표 판정 실패: " + deck[i].id);
            return deck;
        }

        static Vector2Int RandomPoint(System.Random rng)
        {
            int n=rng.Next(49);return new Vector2Int(n%7,n/7);
        }

        static RepairProblem RuntimeFromSeed(int[] seed,QuadKind kind,int band,int wantedVertex,Vector2Int wantedPoint,int order,System.Random rng,bool onlyAnswer)
        {
            var rawStart=new Vector2Int[4];for(int i=0;i<4;i++)rawStart[i]=new Vector2Int(seed[i*2],seed[i*2+1]);
            var rawTarget=(Vector2Int[])rawStart.Clone();int rawAnswer=seed[8];rawTarget[rawAnswer]=new Vector2Int(seed[9],seed[10]);
            int firstSym=rng.Next(8);
            for(int pass=0;pass<8;pass++)
            {
                int sym=(firstSym+pass)&7;Vector2Int answerAt=Sym(rawTarget[rawAnswer],sym);Vector2Int shift=wantedPoint-answerAt;
                var s1=new Vector2Int[4];var t1=new Vector2Int[4];bool fits=true;
                for(int i=0;i<4;i++)
                {
                    s1[i]=Sym(rawStart[i],sym)+shift;t1[i]=Sym(rawTarget[i],sym)+shift;
                    if(!InGrid(s1[i])||!InGrid(t1[i]))fits=false;
                }
                if(!fits)continue;
                int cycle=(rawAnswer-wantedVertex+4)&3;var start=new Vector2Int[4];var target=new Vector2Int[4];
                for(int i=0;i<4;i++){start[i]=s1[(i+cycle)&3];target[i]=t1[(i+cycle)&3];}
                var p=new RepairProblem{
                    id="js-run-"+order,kind=kind,band=band,start=start,target=target,answerVertex=wantedVertex,
                    allowedMask=onlyAnswer?1<<wantedVertex:15,prompt=KindName(kind)+"이 되도록 꼭짓점 하나를 옮기시오.",
                    unitConcept=KindName(kind)+"의 대각선 성질"
                };
                if(!Matches(p.target,p.kind))throw new Exception("런타임 목표 판정 실패: "+order);
                return p;
            }
            throw new Exception("요청한 정답 위치에 문항을 배치할 수 없음: "+wantedPoint);
        }

        static void ShuffleRange(List<RepairProblem> a, int start, int count, System.Random rng)
        {
            for (int i = count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1); var t = a[start + i]; a[start + i] = a[start + j]; a[start + j] = t;
            }
        }

        public static List<MgfProblem> ToBank(List<RepairProblem> src)
        {
            var r = new List<MgfProblem>(src.Count);
            for (int i = 0; i < src.Count; i++)
            {
                var p = src[i];
                r.Add(new MgfProblem {
                    id=p.id, prompt=p.prompt, choices=null, answer=p.AnswerText,
                    answerNumeric=p.AnswerNumeric, unitConcept=p.unitConcept
                });
            }
            return r;
        }
    }
}
